using Booking_SaaS.Domain.Results;
using Booking_SaaS.Services.Abstraction.DTOs;
using Booking_SaaS.Services.Abstraction.DTOs.Tenant;

namespace Booking_SaaS.Services.Abstraction;

public interface ITenantService
{
    Task<Result<PaginatedDto<TenantResultDto>>> GetAllAsync(string? name, int pageIndex, int pageSize,
        CancellationToken cancellationToken = default);

    Task<Result<string>> GetTenantNameByIdAsync(int tenantId);
    Task<Result> AddAsync(TenantAddDto tenantDto);
    Task<Result> UpdateAsync(TenantUpdateDto tenantDto, int tenantId);
    Task<Result<bool>> IsActiveAsync(int tenantId, CancellationToken cancellationToken = default);
}