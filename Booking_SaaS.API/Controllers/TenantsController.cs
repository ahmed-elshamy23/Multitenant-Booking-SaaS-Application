using Booking_SaaS.Domain.Results;
using Booking_SaaS.Services.Abstraction;
using Booking_SaaS.Services.Abstraction.DTOs;
using Booking_SaaS.Services.Abstraction.DTOs.Tenant;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Booking_SaaS.API.Controllers;

[Authorize(Roles = "owner")]
public class TenantsController : ApiController
{
    private readonly ITenantService _tenantService;

    public TenantsController(IServiceManager serviceManager)
    {
        _tenantService = serviceManager.TenantService;
    }

    [HttpGet]
    [SwaggerOperation(OperationId = "Tenant_GetAll")]
    public async Task<ActionResult<Result<PaginatedDto<TenantResultDto>>>> GetAllAsync(string? name, int pageIndex,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var result = await _tenantService.GetAllAsync(name, pageIndex, pageSize, cancellationToken);
        return ToApiResponse(result);
    }

    [HttpPost]
    [SwaggerOperation(OperationId = "Tenant_Add")]
    public async Task<ActionResult<Result>> AddAsync(TenantAddDto tenantDto)
    {
        var result = await _tenantService.AddAsync(tenantDto);
        return ToApiResponse(result);
    }

    [HttpPut("{tenantId:int}")]
    [SwaggerOperation(OperationId = "Tenant_Update")]
    public async Task<ActionResult<Result>> UpdateAsync(TenantUpdateDto tenantDto, int tenantId)
    {
        var result = await _tenantService.UpdateAsync(tenantDto, tenantId);
        return ToApiResponse(result);
    }
}