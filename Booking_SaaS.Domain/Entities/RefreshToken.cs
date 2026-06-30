namespace Booking_SaaS.Domain.Entities;

public class RefreshToken : BaseEntity<int>
{
    public int UserId { get; set; }
    public string Token { get; set; }
    public Guid FamilyId { get; set; }
    public DateTime ExpiresOn { get; set; }
    public bool IsRevoked { get; set; }
    public bool IsActive => !IsRevoked && ExpiresOn > DateTime.UtcNow;

    public AppUser User { get; set; }
}