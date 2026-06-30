using Booking_SaaS.Domain.Entities;

namespace Booking_SaaS.Domain.Contracts.Repositories;

public interface IResourceRepository : IGenericRepository<Resource, int>, ILockable<Resource>
{
}