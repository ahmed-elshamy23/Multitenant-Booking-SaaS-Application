using System.Linq.Expressions;
using Booking_SaaS.Domain.Entities;

namespace Booking_SaaS.Domain.Contracts.Repositories;

public interface IGenericRepository<TEntity, TKey> where TEntity : BaseEntity<TKey>
{
    Task<List<TEntity>> GetAllAsync(SpecificationsBase<TEntity, TKey> specifications,
        CancellationToken cancellationToken = default);

    Task<TEntity?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default);
    Task<int> GetCountAsync(CancellationToken cancellationToken = default);

    Task<int> GetCountAsync(SpecificationsBase<TEntity, TKey> specifications,
        CancellationToken cancellationToken = default);

    Task<int> GetSumAsync(SpecificationsBase<TEntity, TKey> specifications, Expression<Func<TEntity, int>> selector,
        CancellationToken cancellationToken = default);

    Task<bool> AnyAsync(SpecificationsBase<TEntity, TKey> specifications,
        CancellationToken cancellationToken = default);

    void Add(TEntity entity);
    void Update(TEntity entity);
    void Delete(TEntity entity);
}