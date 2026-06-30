namespace Booking_SaaS.Domain.Entities;

public abstract class BaseEntity
{
    public DateTime CreatedOn { get; set; }
    public DateTime? LastModifiedOn { get; set; }
    public Guid Version { get; set; }
}

public abstract class BaseEntity<TKey> : BaseEntity
{
    public TKey Id { get; set; }
}