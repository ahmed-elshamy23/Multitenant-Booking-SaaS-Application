using Booking_SaaS.Domain.Contracts;
using Booking_SaaS.Domain.Entities;

namespace Booking_SaaS.Services.Specifications;

internal static class TenantSpecifications
{
    internal class CheckDuplicateSpecification : SpecificationsBase<Tenant, int>
    {
        internal CheckDuplicateSpecification(string name, int id) : base(t =>
            t.Name == name && t.Id != id)
        {
        }
    }

    internal class FilterSpecification : SpecificationsBase<Tenant, int>
    {
        internal FilterSpecification(string? name) : base(t =>
            t.Name.StartsWith(name ?? string.Empty))
        {
        }
    }

    internal class PaginatedFilterSpecification : FilterSpecification
    {
        internal PaginatedFilterSpecification(string? name, int pageIndex, int pageSize) : base(name)
        {
            ApplyPagination(pageIndex, pageSize);
        }
    }
}