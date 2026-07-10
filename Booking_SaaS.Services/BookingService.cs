using AutoMapper;
using Booking_SaaS.Domain.Contracts;
using Booking_SaaS.Domain.Contracts.Repositories;
using Booking_SaaS.Domain.Entities;
using Booking_SaaS.Domain.Enums;
using Booking_SaaS.Domain.Results;
using Booking_SaaS.Services.Abstraction;
using Booking_SaaS.Services.Abstraction.DTOs;
using Booking_SaaS.Services.Abstraction.DTOs.Bookings;
using Booking_SaaS.Services.Specifications;
using FluentValidation;

namespace Booking_SaaS.Services;

internal class BookingService : IBookingService
{
    private readonly IValidator<BookingFilterDto> _filterValidator;
    private readonly IMapper _mapper;
    private readonly IValidator<PaginatedDto<BookingDto>> _paginationValidator;
    private readonly IBookingRepository _repo;
    private readonly IUnitOfWork _unitOfWork;

    public BookingService(IUnitOfWork unitOfWork, IValidator<BookingFilterDto> filterValidator, IMapper mapper,
        IValidator<PaginatedDto<BookingDto>> paginationValidator)
    {
        _unitOfWork = unitOfWork;
        _filterValidator = filterValidator;
        _mapper = mapper;
        _paginationValidator = paginationValidator;
        _repo = _unitOfWork.BookingRepository;
    }

    public async Task<Result<PaginatedDto<BookingDto>>> GetAllAsync(BookingFilterDto filterDto, int userId,
        CancellationToken cancellationToken = default)
    {
        var paginatedDto = new PaginatedDto<BookingDto>
        {
            PageIndex = filterDto.PageIndex,
            PageSize = filterDto.PageSize
        };
        var validationResult = _paginationValidator.Validate(paginatedDto);
        if (!validationResult.IsValid)
            return Error.Validation.InvalidParameters(validationResult.Errors
                .Select(x => x.ErrorMessage));

        validationResult = _filterValidator.Validate(filterDto);
        if (!validationResult.IsValid)
            return Error.Validation.InvalidParameters(validationResult.Errors
                .Select(x => x.ErrorMessage));

        var filterSpecification = new BookingSpecifications.UserFilterSpecification(filterDto, userId);
        paginatedDto.TotalCount = await _repo.GetCountAsync(filterSpecification, cancellationToken);

        var paginationSpecification =
            new BookingSpecifications.UserPaginatedFilterSpecification(filterDto, userId, filterDto.PageIndex,
                filterDto.PageSize);
        paginatedDto.Items =
            _mapper.Map<List<Booking>, List<BookingDto>>(await _repo.GetAllAsync(paginationSpecification,
                cancellationToken));

        return paginatedDto;
    }

    public async Task<Result<BookingDto>> GetByIdAsync(int bookingId, int userId)
    {
        var booking = await _repo.GetByIdAsync(bookingId);
        if (booking == null || booking.UserId != userId)
            return Error.Validation.NotFound(nameof(Booking));
        return _mapper.Map<Booking, BookingDto>(booking);
    }

    public async Task<Result<int>> AddAsync(BookingAddDto bookingDto, int scheduleId, int userId)
    {
        var booking = _mapper.Map<Booking>(bookingDto);
        booking.ScheduleId = scheduleId;
        booking.UserId = userId;
        booking.Status = BookingStatus.Pending;

        _repo.Add(booking);
        await _unitOfWork.SaveChangesAsync();

        return booking.Id;
    }

    public async Task<Result> CancelAsync(int bookingId, int userId)
    {
        var booking = await _repo.GetByIdAsync(bookingId);
        if (booking == null || booking.UserId != userId)
            return Error.Validation.NotFound(nameof(Booking));

        if (booking.Status != BookingStatus.Pending)
            return Error.Booking.NonPending;

        if (booking.Date <= DateOnly.FromDateTime(DateTime.UtcNow))
            return Error.Booking.SameDayCancellation;

        booking.Status = BookingStatus.Cancelled;
        _repo.Update(booking);
        await _unitOfWork.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<bool> HasPendingBookingAsync(int scheduleId)
    {
        var specification =
            new BookingSpecifications.HasPendingBookingsSpecification(scheduleId);
        return await _repo.AnyAsync(specification);
    }

    public async Task<int> GetPendingBookingsCountAsync(int scheduleId, DateOnly date)
    {
        var specification = new BookingSpecifications.PendingStatusAndDateSpecification(scheduleId, date);
        return await _repo.GetSumAsync(specification, b => b.Quantity);
    }

    public async Task<int> GetPendingBookingsCountAsync(int scheduleId)
    {
        return await _repo.GetRecurrentScheduleMaximumCapacityAsync(scheduleId);
    }

    public async Task<bool> HasUserAlreadyBookedAsync(int userId, int scheduleId, DateOnly date)
    {
        var specification =
            new BookingSpecifications.CheckDuplicateSpecification(userId, scheduleId, date);
        return await _repo.AnyAsync(specification);
    }
}