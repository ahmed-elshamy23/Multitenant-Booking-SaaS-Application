namespace Booking_SaaS.Services.Abstraction.DTOs;

public class BaseDateTimeFilterDto : BaseDto
{
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }
    public int PageIndex { get; set; }
    public int PageSize { get; set; }
}