namespace Booking_SaaS.Services.Abstraction.DTOs.Bookings;

public class BookingFilterDto : BaseDateTimeFilterDto
{
    public int? Id { get; set; }
}