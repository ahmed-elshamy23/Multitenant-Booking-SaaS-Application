using Booking_SaaS.Domain.Contracts;
using Booking_SaaS.Domain.Entities;
using Booking_SaaS.Domain.Results;
using Booking_SaaS.Services.Abstraction;
using Booking_SaaS.Services.Abstraction.DTOs;
using Booking_SaaS.Services.Abstraction.DTOs.Bookings;
using Booking_SaaS.Services.Abstraction.Orchestrators;
using FluentValidation;

namespace Booking_SaaS.Services.Orchestrators;

public class BookingOrchestrator : IBookingOrchestrator
{
    private readonly IBookingService _bookingService;
    private readonly IValidator<BookingAddDto> _bookingValidator;
    private readonly int _currentTenantId;
    private readonly IScheduleService _scheduleService;
    private readonly ITenantService _tenantService;
    private readonly IUnitOfWork _unitOfWork;

    public BookingOrchestrator(ITenantService tenantService, ITenantResolver tenantResolver,
        IScheduleService scheduleService, IBookingService bookingService,
        IUnitOfWork unitOfWork, IValidator<BookingAddDto> bookingValidator)
    {
        _tenantService = tenantService;
        _currentTenantId = tenantResolver.CurrentTenantId;
        _scheduleService = scheduleService;
        _bookingService = bookingService;
        _unitOfWork = unitOfWork;
        _bookingValidator = bookingValidator;
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

            var schedule = lockAcquired.Value;
            if (!schedule.AllowsMultiple && bookingDto.Quantity > 1)
            {
                await _unitOfWork.RollbackAsync();
                return Error.Booking.ExceededMaximum(1);
            }

            if (!schedule.Date.HasValue)
            {
                if (bookingDto.Date == null)
                {
                    await _unitOfWork.RollbackAsync();
                    return Error.Booking.MissingDate;
                }

                if (bookingDto.Date.Value.DayOfWeek != schedule.DayOfWeek)
                {
                    await _unitOfWork.RollbackAsync();
                    return Error.Booking.DayOfWeekMismatch;
                }
            }

            var targetDate = schedule.Date ?? bookingDto.Date!.Value;
            var exactStartDateTime = targetDate.ToDateTime(schedule.StartTime, DateTimeKind.Utc);
            if (exactStartDateTime <= DateTime.UtcNow)
            {
                await _unitOfWork.RollbackAsync();
                return Error.Booking.PastDateNotAllowed;
            }

            var currentBookingsCount = await _bookingService.GetPendingBookingsCountAsync(scheduleId, targetDate);
            if (currentBookingsCount == schedule.Capacity)
            {
                await _unitOfWork.RollbackAsync();
                return Error.Booking.FullyBooked;
            }

            if (await _bookingService.HasUserAlreadyBookedAsync(userId, scheduleId, targetDate))
                return Error.Booking.DuplicateUserBooking;

            if (currentBookingsCount + bookingDto.Quantity > schedule.Capacity)
            {
                await _unitOfWork.RollbackAsync();
                return Error.Booking.ExceededMaximum(schedule.Capacity - currentBookingsCount);
            }

            bookingDto.Date = targetDate;
            var result = await _bookingService.AddAsync(bookingDto, scheduleId, userId);
            if (!result.IsSuccess)
            {
                await _unitOfWork.RollbackAsync();
                return result;
            }

            await _unitOfWork.CommitAsync();
            return result;
        }
        catch (Exception)
        {
            await _unitOfWork.RollbackAsync();
            throw;
        }
    }

    public async Task<Result> CancelAsync(int bookingId, int userId)
    {
        var isActiveTenantResult = await _tenantService.IsActiveAsync(_currentTenantId);
        if (!isActiveTenantResult.IsSuccess)
            return isActiveTenantResult.Error!;
        if (!isActiveTenantResult.Value)
            return Error.Tenant.Inactive;

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            var scheduleIdResult = await _bookingService.GetScheduleIdAsync(bookingId, userId);
            if (!scheduleIdResult.IsSuccess)
            {
                await _unitOfWork.RollbackAsync();
                return scheduleIdResult.Error!;
            }

            var scheduleLockAcquired = await _scheduleService.AcquireLockAsync(scheduleIdResult.Value);
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
            return result;
        }
        catch (Exception)
        {
            await _unitOfWork.RollbackAsync();
            throw;
        }
    }
}