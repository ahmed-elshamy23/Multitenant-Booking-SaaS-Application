namespace Booking_SaaS.Services.Abstraction.DTOs.Tenant;

public class TenantUpdateDto : TenantAddDto
{
    public bool IsActive { get; set; }
}