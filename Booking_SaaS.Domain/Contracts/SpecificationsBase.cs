using System.Linq.Expressions;
using Booking_SaaS.Domain.Entities;

namespace Booking_SaaS.Domain.Contracts;

public abstract class SpecificationsBase<TEntity, TKey> where TEntity : BaseEntity<TKey>
{
    protected SpecificationsBase(Expression<Func<TEntity, bool>>? criteria = null)
    {
        Criteria = criteria;
    }

    public Expression<Func<TEntity, bool>>? Criteria { get; init; }
    public List<Expression<Func<TEntity, object>>> IncludeExpressions { get; } = [];
    public List<Expression<Func<TEntity, object>>> OrderByExpressions { get; } = [];
    public List<Expression<Func<TEntity, object>>> OrderByDescendingExpressions { get; } = [];

    public int Take { get; private set; }
    public int Skip { get; private set; }
    public bool IsPaginated { get; private set; }

    public void AddInclude(Expression<Func<TEntity, object>> expression)
    {
        IncludeExpressions.Add(expression);
    }

    public void AddOrderBy(Expression<Func<TEntity, object>> expression)
    {
        OrderByExpressions.Add(expression);
    }

    public void AddOrderByDescending(Expression<Func<TEntity, object>> expression)
    {
        OrderByDescendingExpressions.Add(expression);
    }

    protected void ApplyPagination(int pageIndex, int pageSize)
    {
        IsPaginated = true;
        Take = pageSize;
        Skip = (pageIndex - 1) * pageSize;
    }
}