using Booking_SaaS.Domain.Entities;

namespace Booking_SaaS.Domain.Contracts.Repositories;

public interface IRefreshTokenRepository : IGenericRepository<RefreshToken, int>
{
    Task RevokeTokenFamilyAsync(Guid familyId);
    Task RevokeUserTokensAsync(int userId);
}