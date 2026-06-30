namespace Booking_SaaS.Services.Abstraction.DTOs.Bookings;

public class BookingAddDto : BaseDto
{
    public DateOnly? Date { get; set; }
    public int Quantity { get; set; }
}