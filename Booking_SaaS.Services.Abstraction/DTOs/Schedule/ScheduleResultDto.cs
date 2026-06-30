namespace Booking_SaaS.Services.Abstraction.DTOs.Schedule;

public class ScheduleResultDto : ScheduleUpdateDto
{
    public int Id { get; set; }
    public DateTime CreatedOn { get; set; }
    public DateTime? LastModifiedOn { get; set; }
}