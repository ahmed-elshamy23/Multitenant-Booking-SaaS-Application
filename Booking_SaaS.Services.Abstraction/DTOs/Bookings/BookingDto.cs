using System.Text.Json.Serialization;
using Booking_SaaS.Domain.Enums;

namespace Booking_SaaS.Services.Abstraction.DTOs.Bookings;

public class BookingDto : BookingAddDto
{
    public int Id { get; set; }
    public int? ScheduleId { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public DateTime CreatedOn { get; set; }
    public DateTime? LastModifiedOn { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public BookingStatus Status { get; set; }
}