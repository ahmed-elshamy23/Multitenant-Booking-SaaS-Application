using Booking_SaaS.Domain.Results;
using Booking_SaaS.Services.Abstraction.DTOs;
using Booking_SaaS.Services.Abstraction.DTOs.Bookings;

namespace Booking_SaaS.Services.Abstraction.Orchestrators;

public interface IBookingOrchestrator
{
    Task<Result<PaginatedDto<BookingDto>>> GetAllAsync(BookingFilterDto filterDto, int userId,
        CancellationToken cancellationToken = default);

    Task<Result<int>> AddAsync(BookingAddDto bookingDto, int scheduleId, int userId);
    Task<Result> CancelAsync(int bookingId, int userId);
}