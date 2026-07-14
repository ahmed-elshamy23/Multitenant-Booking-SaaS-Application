using Booking_SaaS.Domain.Contracts;
using Booking_SaaS.Domain.Entities;
using Booking_SaaS.Domain.Results;
using Booking_SaaS.Services.Abstraction;
using Booking_SaaS.Services.Abstraction.Contracts;
using Booking_SaaS.Services.Abstraction.DTOs;
using Booking_SaaS.Services.Abstraction.DTOs.Bookings;
using Booking_SaaS.Services.Abstraction.Orchestrators;
using FluentValidation;

namespace Booking_SaaS.Services.Orchestrators;

public class BookingOrchestrator : IBookingOrchestrator
{
    private readonly IBookingService _bookingService;
    private readonly IValidator<BookingAddDto> _bookingValidator;
    private readonly ICachingService _cachingService;
    private readonly int _currentTenantId;
    private readonly IScheduleService _scheduleService;
    private readonly ITenantService _tenantService;
    private readonly IUnitOfWork _unitOfWork;

    public BookingOrchestrator(ITenantService tenantService, ITenantResolver tenantResolver,
        IScheduleService scheduleService, IBookingService bookingService,
        IUnitOfWork unitOfWork, IValidator<BookingAddDto> bookingValidator, ICachingService cachingService)
    {
        _tenantService = tenantService;
        _currentTenantId = tenantResolver.CurrentTenantId;
        _scheduleService = scheduleService;
        _bookingService = bookingService;
        _unitOfWork = unitOfWork;
        _bookingValidator = bookingValidator;
        _cachingService = cachingService;
    }

    public async Task<Result<PaginatedDto<BookingDto>>> GetAllAsync(BookingFilterDto filterDto, int userId,
        CancellationToken cancellationToken = default)
    {
        var isActiveTenantResult = await _tenantService.IsActiveAsync(_currentTenantId, cancellationToken);
        if (!isActiveTenantResult.IsSuccess)
            return isActiveTenantResult.Error!;
        if (!isActiveTenantResult.Value)
            return Error.Tenant.Inactive;

        return await _bookingService.GetAllAsync(filterDto, userId, cancellationToken);
    }

    public async Task<Result<int>> AddAsync(BookingAddDto bookingDto, int scheduleId, int userId)
    {
        var isActiveTenantResult = await _tenantService.IsActiveAsync(_currentTenantId);
        if (!isActiveTenantResult.IsSuccess)
            return isActiveTenantResult.Error!;
        if (!isActiveTenantResult.Value)
            return Error.Tenant.Inactive;

        var validationResult = _bookingValidator.Validate(bookingDto);
        if (!validationResult.IsValid)
            return Error.Validation.InvalidParameters(validationResult.Errors
                .Select(x => x.ErrorMessage));

        var scheduleResult = await _scheduleService.GetByIdAsync(scheduleId);
        if (!scheduleResult.IsSuccess)
            return scheduleResult.Error!;

        var schedule = scheduleResult.Value;
        if (schedule == null)
            return Error.Validation.NotFound(nameof(Schedule));

        if (!schedule.AllowsMultiple && bookingDto.Quantity > 1)
            return Error.Booking.ExceededMaximum(1);

        if (!schedule.Date.HasValue)
        {
            if (bookingDto.Date == null)
                return Error.Booking.MissingDate;

            if (bookingDto.Date.Value.DayOfWeek != schedule.DayOfWeek)
                return Error.Booking.DayOfWeekMismatch;
        }

        var targetDate = schedule.Date ?? bookingDto.Date!.Value;
        var exactStartDateTime = targetDate.ToDateTime(schedule.StartTime, DateTimeKind.Utc);
        if (exactStartDateTime <= DateTime.UtcNow)
            return Error.Booking.PastDateNotAllowed;

        var cacheKey = $"{scheduleId}:{targetDate:yyyyMMdd}";
        var cachedValue = await _cachingService.GetAsync(cacheKey);
        var toBeIncremented = false;
        if (cachedValue != null)
        {
            if (int.Parse(cachedValue) == 0)
                return Error.Booking.FullyBooked;

            var remainingSlots = await _cachingService.DecrementByAsync(cacheKey, bookingDto.Quantity);
            if (remainingSlots < 0)
            {
                await _cachingService.IncrementByAsync(cacheKey, bookingDto.Quantity);
                return Error.Booking.ExceededMaximum(int.Parse(cachedValue));
            }

            toBeIncremented = true;
        }

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            var lockAcquired = await _scheduleService.GetAndLockByIdAsync(scheduleId);
            if (!lockAcquired.IsSuccess)
            {
                await _unitOfWork.RollbackAsync();
                return lockAcquired.Error!;
            }

            if (lockAcquired.Value == null)
            {
                await _unitOfWork.RollbackAsync();
                return Error.Validation.NotFound(nameof(Schedule));
            }

            schedule = lockAcquired.Value;
            if (await _bookingService.HasUserAlreadyBookedAsync(userId, scheduleId, targetDate))
                return Error.Booking.DuplicateUserBooking;

            var currentBookingsCount = 0;
            if (cachedValue == null)
            {
                currentBookingsCount = await _bookingService.GetPendingBookingsCountAsync(scheduleId, targetDate);
                if (currentBookingsCount == schedule.Capacity)
                {
                    await _unitOfWork.RollbackAsync();
                    return Error.Booking.FullyBooked;
                }

                if (currentBookingsCount + bookingDto.Quantity > schedule.Capacity)
                {
                    await _unitOfWork.RollbackAsync();
                    return Error.Booking.ExceededMaximum(schedule.Capacity - currentBookingsCount);
                }
            }

            bookingDto.Date = targetDate;
            var result = await _bookingService.AddAsync(bookingDto, scheduleId, userId);
            if (!result.IsSuccess)
            {
                await _unitOfWork.RollbackAsync();
                return result;
            }

            await _unitOfWork.CommitAsync();
            if (cachedValue == null)
            {
                var newAvailableCapacity = schedule.Capacity - currentBookingsCount - bookingDto.Quantity;
                await _cachingService.SetAsync(cacheKey, newAvailableCapacity.ToString(), false);
            }

            toBeIncremented = false;
            return result;
        }
        catch (Exception)
        {
            await _unitOfWork.RollbackAsync();
            throw;
        }
        finally
        {
            if (toBeIncremented)
                await _cachingService.IncrementByAsync(cacheKey, bookingDto.Quantity);
        }
    }

    public async Task<Result> CancelAsync(int bookingId, int userId)
    {
        var isActiveTenantResult = await _tenantService.IsActiveAsync(_currentTenantId);
        if (!isActiveTenantResult.IsSuccess)
            return isActiveTenantResult.Error!;
        if (!isActiveTenantResult.Value)
            return Error.Tenant.Inactive;

        var bookingResult = await _bookingService.GetByIdAsync(bookingId, userId);
        if (!bookingResult.IsSuccess)
            return bookingResult.Error!;

        var booking = bookingResult.Value;
        if (booking == null)
            return Error.Validation.NotFound(nameof(Booking));

        var cacheKey = $"{booking.ScheduleId!.Value}:{booking.Date!.Value:yyyyMMdd}";

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            var scheduleLockAcquired = await _scheduleService.AcquireLockAsync(booking.ScheduleId!.Value);
            if (!scheduleLockAcquired)
            {
                await _unitOfWork.RollbackAsync();
                return Error.Validation.NotFound(nameof(Schedule));
            }

            var result = await _bookingService.CancelAsync(bookingId, userId);
            if (!result.IsSuccess)
            {
                await _unitOfWork.RollbackAsync();
                return result;
            }

            await _unitOfWork.CommitAsync();
            var cachedValue = await _cachingService.GetAsync(cacheKey);
            if (cachedValue != null)
                await _cachingService.IncrementByAsync(cacheKey, booking.Quantity);

            return result;
        }
        catch (Exception)
        {
            await _unitOfWork.RollbackAsync();
            throw;
        }
    }
}