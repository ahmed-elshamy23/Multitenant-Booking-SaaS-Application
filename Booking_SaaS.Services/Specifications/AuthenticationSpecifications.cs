using Booking_SaaS.Domain.Contracts;
using Booking_SaaS.Domain.Entities;

namespace Booking_SaaS.Services.Specifications;

internal static class AuthenticationSpecifications
{
    internal class RefreshTokenSpecification : SpecificationsBase<RefreshToken, int>
    {
        internal RefreshTokenSpecification(string token) : base(r => r.Token == token)
        {
        }
    }
}