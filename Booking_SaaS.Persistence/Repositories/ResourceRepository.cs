using Booking_SaaS.Domain.Contracts.Repositories;
using Booking_SaaS.Domain.Entities;
using Booking_SaaS.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Booking_SaaS.Persistence.Repositories;

internal class ResourceRepository : GenericRepository<Resource, int>, IResourceRepository
{
    public ResourceRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<bool> AcquireLockAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Resources
            .FromSqlRaw("SELECT * FROM Resources WITH (UPDLOCK, ROWLOCK) WHERE Id = {0}", id)
            .Select(r => r.Id)
            .AnyAsync(cancellationToken);
    }

    public async Task<Resource?> GetAndLockByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Resources
            .FromSqlRaw("SELECT * FROM Resources WITH (UPDLOCK, ROWLOCK) WHERE Id = {0}", id)
            .FirstOrDefaultAsync(cancellationToken);
    }
}