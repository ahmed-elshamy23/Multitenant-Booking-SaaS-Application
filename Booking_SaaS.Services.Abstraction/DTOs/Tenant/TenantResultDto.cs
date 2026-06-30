namespace Booking_SaaS.Services.Abstraction.DTOs.Tenant;

public class TenantResultDto : TenantUpdateDto
{
    public int Id { get; set; }
    public DateTime CreatedOn { get; set; }
    public DateTime? LastModifiedOn { get; set; }
}