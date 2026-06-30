using Booking_SaaS.Domain.Contracts.Repositories;
using Booking_SaaS.Domain.Entities;
using Booking_SaaS.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Booking_SaaS.Persistence.Repositories;

internal class RefreshTokenRepository : GenericRepository<RefreshToken, int>, IRefreshTokenRepository
{
    public RefreshTokenRepository(AppDbContext context) : base(context)
    {
    }

    public async Task RevokeTokenFamilyAsync(Guid familyId)
    {
        await _context.RefreshTokens
            .Where(r => r.FamilyId == familyId && !r.IsRevoked && r.ExpiresOn > DateTime.UtcNow)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(r => r.IsRevoked, true)
                .SetProperty(r => r.LastModifiedOn, DateTime.UtcNow)
                .SetProperty(r => r.Version, Guid.NewGuid()));
    }

    public async Task RevokeUserTokensAsync(int userId)
    {
        await _context.RefreshTokens
            .Where(r => r.UserId == userId && !r.IsRevoked && r.ExpiresOn > DateTime.UtcNow)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(r => r.IsRevoked, true)
                .SetProperty(r => r.LastModifiedOn, DateTime.UtcNow)
                .SetProperty(r => r.Version, Guid.NewGuid()));
    }
}