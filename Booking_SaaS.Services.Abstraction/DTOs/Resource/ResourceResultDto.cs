namespace Booking_SaaS.Services.Abstraction.DTOs.Resource;

public class ResourceResultDto : ResourceUpdateDto
{
    public DateTime CreatedOn { get; set; }
    public DateTime? LastModifiedOn { get; set; }
}