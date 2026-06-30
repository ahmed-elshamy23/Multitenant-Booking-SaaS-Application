using System.Text.Json.Serialization;

namespace Booking_SaaS.Services.Abstraction.DTOs.Schedule;

public class ScheduleAddDto : BaseDto
{
    public int Capacity { get; set; }
    public bool AllowsMultiple { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public DayOfWeek? DayOfWeek { get; set; }

    public DateOnly? Date { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
}