namespace Booking_SaaS.Services.Abstraction.DTOs.Resource;

public class ResourceAddDto : BaseDto
{
    public string Name { get; set; }
    public string Description { get; set; }
    public string Type { get; set; }
}