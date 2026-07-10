using Booking_SaaS.Domain.Results;
using Booking_SaaS.Services.Abstraction.DTOs;
using Booking_SaaS.Services.Abstraction.DTOs.Bookings;

namespace Booking_SaaS.Services.Abstraction;

public interface IBookingService
{
    Task<Result<PaginatedDto<BookingDto>>> GetAllAsync(BookingFilterDto filterDto, int userId,
        CancellationToken cancellationToken = default);

    Task<Result<BookingDto>> GetByIdAsync(int bookingId, int userId);

    Task<Result<int>> AddAsync(BookingAddDto bookingDto, int scheduleId, int userId);
    Task<Result> CancelAsync(int scheduleId, int userId);

    Task<bool> HasPendingBookingAsync(int scheduleId);
    Task<int> GetPendingBookingsCountAsync(int scheduleId, DateOnly date);
    Task<int> GetPendingBookingsCountAsync(int scheduleId);
    Task<bool> HasUserAlreadyBookedAsync(int userId, int scheduleId, DateOnly date);
}