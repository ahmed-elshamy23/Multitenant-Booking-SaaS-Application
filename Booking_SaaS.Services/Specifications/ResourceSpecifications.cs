using System.Linq.Expressions;
using Booking_SaaS.Domain.Contracts;
using Booking_SaaS.Domain.Entities;
using Booking_SaaS.Domain.Enums;
using Booking_SaaS.Services.Extensions;

namespace Booking_SaaS.Services.Specifications;

internal static class ResourceSpecifications
{
    internal class CheckDuplicateSpecification : SpecificationsBase<Resource, int>
    {
        internal CheckDuplicateSpecification(string name, ResourceType type, int id) : base(r =>
            r.Name == name && r.Type == type && r.Id != id)
        {
        }
    }

    internal class IsExistingSpecification : SpecificationsBase<Resource, int>
    {
        internal IsExistingSpecification(int id) : base(r => r.Id == id)
        {
        }
    }

    internal class FilterSpecification : SpecificationsBase<Resource, int>
    {
        internal FilterSpecification(string? name, ResourceType? type)
        {
            Expression<Func<Resource, bool>>? criteria = null;

            if (!string.IsNullOrEmpty(name))
                criteria = r => r.Name.StartsWith(name);
            if (type != null)
                criteria = criteria == null
                    ? r => r.Type == type
                    : criteria.And(r => r.Type == type);

            Criteria = criteria;
        }
    }

    internal class PaginatedFilterSpecification : FilterSpecification
    {
        internal PaginatedFilterSpecification(string? name, ResourceType? type, int pageIndex, int pageSize)
            : base(name, type)
        {
            ApplyPagination(pageIndex, pageSize);
            AddOrderBy(r => r.Name);
        }
    }

    internal class HasSchedulesSpecification : SpecificationsBase<Resource, int>
    {
        internal HasSchedulesSpecification(int id) : base(r => r.Id == id && r.Schedules.Any())
        {
        }
    }
}