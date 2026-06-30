using System.Linq.Expressions;
using Booking_SaaS.Domain.Contracts;
using Booking_SaaS.Domain.Contracts.Repositories;
using Booking_SaaS.Domain.Entities;
using Booking_SaaS.Persistence.Context;
using Booking_SaaS.Persistence.Specifications;
using Microsoft.EntityFrameworkCore;

namespace Booking_SaaS.Persistence.Repositories;

internal class GenericRepository<TEntity, TKey> : IGenericRepository<TEntity, TKey> where TEntity : BaseEntity<TKey>
{
    protected readonly AppDbContext _context;

    public GenericRepository(AppDbContext context)
    {
        _context = context;
    }

    public virtual void Add(TEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        _context.Set<TEntity>().Add(entity);
    }

    public virtual void Update(TEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        _context.Entry(entity).State = EntityState.Modified;
    }

    public virtual void Delete(TEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        _context.Set<TEntity>().Remove(entity);
    }

    public virtual async Task<List<TEntity>> GetAllAsync(SpecificationsBase<TEntity, TKey> specifications,
        CancellationToken cancellationToken = default)
    {
        var query = SpecificationFactory.BuildQuery(_context.Set<TEntity>(), specifications);
        return await query.AsNoTracking().ToListAsync(cancellationToken);
    }

    public virtual async Task<TEntity?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default)
    {
        return await _context.Set<TEntity>().FindAsync(id, cancellationToken);
    }

    public virtual async Task<int> GetCountAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Set<TEntity>().CountAsync(cancellationToken);
    }

    public virtual async Task<int> GetCountAsync(SpecificationsBase<TEntity, TKey> specifications,
        CancellationToken cancellationToken = default)
    {
        return await SpecificationFactory.BuildQuery(_context.Set<TEntity>(), specifications)
            .CountAsync(cancellationToken);
    }

    public async Task<int> GetSumAsync(SpecificationsBase<TEntity, TKey> specifications,
        Expression<Func<TEntity, int>> selector,
        CancellationToken cancellationToken = default)
    {
        return await SpecificationFactory.BuildQuery(_context.Set<TEntity>(), specifications)
            .SumAsync(selector, cancellationToken);
    }

    public virtual async Task<bool> AnyAsync(SpecificationsBase<TEntity, TKey> specifications,
        CancellationToken cancellationToken = default)
    {
        return await SpecificationFactory.BuildQuery(_context.Set<TEntity>(), specifications)
            .AnyAsync(cancellationToken);
    }
}