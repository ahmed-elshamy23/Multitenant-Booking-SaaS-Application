using Booking_SaaS.Domain.Contracts.Repositories;
using Booking_SaaS.Domain.Entities;

namespace Booking_SaaS.Domain.Contracts;

public interface IUnitOfWork
{
    IResourceRepository ResourceRepository { get; }
    IScheduleRepository ScheduleRepository { get; }
    IBookingRepository BookingRepository { get; }
    IRefreshTokenRepository RefreshTokenRepository { get; }

    IGenericRepository<TEntity, TKey> GetRepository<TEntity, TKey>() where TEntity : BaseEntity<TKey>;
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitAsync(CancellationToken cancellationToken = default);
    Task RollbackAsync(CancellationToken cancellationToken = default);
}