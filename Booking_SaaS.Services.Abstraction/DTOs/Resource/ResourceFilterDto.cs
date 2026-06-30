namespace Booking_SaaS.Services.Abstraction.DTOs.Resource;

public class ResourceFilterDto : BaseDto
{
    public string? Name { get; set; }
    public string? Type { get; set; }
    public int PageIndex { get; set; }
    public int PageSize { get; set; }
}