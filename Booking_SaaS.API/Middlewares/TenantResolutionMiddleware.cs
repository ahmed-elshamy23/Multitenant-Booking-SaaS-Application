using System.Security.Claims;
using Booking_SaaS.Domain.Contracts;
using Microsoft.AspNetCore.Authorization;

namespace Booking_SaaS.API.Middlewares;

public class TenantResolutionMiddleware
{
    private const string TenantIdClaim = "tenant_id";
    private const string TenantIdHeader = "X-Tenant-Id";

    private readonly RequestDelegate _next;

    public TenantResolutionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ITenantResolver tenantResolver)
    {
        var endpoint = context.GetEndpoint();
        if (endpoint is null)
        {
            await _next(context);
            return;
        }

        tenantResolver.CurrentTenantId = ResolveTenantId(context, endpoint);
        await _next(context);
    }

    private static int ResolveTenantId(HttpContext context, Endpoint endpoint)
    {
        var user = context.User;
        var requiresAuthentication = endpoint.Metadata.GetMetadata<IAuthorizeData>() != null;

        if (requiresAuthentication && user.Identity?.IsAuthenticated == true &&
            int.TryParse(
                user.FindFirstValue(TenantIdClaim),
                out var claimTenantId))
            return claimTenantId;

        if (!requiresAuthentication &&
            int.TryParse(
                context.Request.Headers[TenantIdHeader],
                out var headerTenantId))
            return headerTenantId;

        return 0;
    }
}