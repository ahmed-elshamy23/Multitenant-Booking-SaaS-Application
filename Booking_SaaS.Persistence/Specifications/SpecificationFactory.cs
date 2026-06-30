using Booking_SaaS.Domain.Contracts;
using Booking_SaaS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Booking_SaaS.Persistence.Specifications;

internal static class SpecificationFactory
{
    internal static IQueryable<TEntity> BuildQuery<TEntity, TKey>(IQueryable<TEntity> query,
        SpecificationsBase<TEntity, TKey> specification)
        where TEntity : BaseEntity<TKey>
    {
        query = query.AsNoTracking();

        if (specification.Criteria != null)
            query = query.Where(specification.Criteria);

        foreach (var expression in specification.IncludeExpressions)
            query = query.Include(expression);

        if (specification.OrderByExpressions.Any())
        {
            var ordered =
                query.OrderBy(specification.OrderByExpressions[0]);

            foreach (var expression in specification.OrderByExpressions.Skip(1)) ordered = ordered.ThenBy(expression);

            query = ordered;
        }

        else if (specification.OrderByDescendingExpressions.Any())
        {
            var ordered =
                query.OrderByDescending(specification.OrderByDescendingExpressions[0]);

            foreach (var expression in specification.OrderByDescendingExpressions.Skip(1))
                ordered = ordered.ThenByDescending(expression);

            query = ordered;
        }

        if (specification.IsPaginated)
            query = query.Skip(specification.Skip).Take(specification.Take);

        return query;
    }
}