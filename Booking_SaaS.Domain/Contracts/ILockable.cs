using Booking_SaaS.Domain.Entities;

namespace Booking_SaaS.Domain.Contracts;

public interface ILockable<TEntity> where TEntity : BaseEntity
{
    Task<bool> AcquireLockAsync(int id, CancellationToken cancellationToken = default);
    Task<TEntity?> GetAndLockByIdAsync(int id, CancellationToken cancellationToken = default);
}