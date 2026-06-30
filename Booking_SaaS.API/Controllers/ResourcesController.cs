using Booking_SaaS.Domain.Results;
using Booking_SaaS.Services.Abstraction;
using Booking_SaaS.Services.Abstraction.DTOs;
using Booking_SaaS.Services.Abstraction.DTOs.Resource;
using Booking_SaaS.Services.Abstraction.Orchestrators;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Booking_SaaS.API.Controllers;

public class ResourcesController : ApiController
{
    private readonly IResourceOrchestrator _resourceOrchestrator;

    public ResourcesController(IServiceManager serviceManager)
    {
        _resourceOrchestrator = serviceManager.ResourceOrchestrator;
    }

    [HttpGet]
    [Authorize(Roles = "admin,user")]
    [SwaggerOperation(OperationId = "Resource_GetAll")]
    public async Task<ActionResult<Result<PaginatedDto<ResourceResultDto>>>> GetAllAsync(
        [FromQuery] ResourceFilterDto filterDto,
        CancellationToken cancellationToken = default)
    {
        var result = await _resourceOrchestrator.GetAllAsync(filterDto, cancellationToken);
        return ToApiResponse(result);
    }

    [HttpGet("{resourceId:int}")]
    [Authorize(Roles = "admin,user")]
    [SwaggerOperation(OperationId = "Resource_GetById")]
    public async Task<ActionResult<Result<ResourceResultDto>>> GetByIdAsync(int resourceId,
        CancellationToken cancellationToken = default)
    {
        var result = await _resourceOrchestrator.GetByIdAsync(resourceId, cancellationToken);
        return ToApiResponse(result);
    }

    [HttpPost]
    [Authorize(Roles = "admin")]
    [SwaggerOperation(OperationId = "Resource_Add")]
    public async Task<ActionResult<Result<int>>> AddAsync(ResourceAddDto resourceDto)
    {
        var result = await _resourceOrchestrator.AddResourceAsync(resourceDto);
        return ToApiResponse(result);
    }

    [HttpPut("{resourceId:int}")]
    [Authorize(Roles = "admin")]
    [SwaggerOperation(OperationId = "Resource_Update")]
    public async Task<ActionResult<Result>> UpdateAsync(ResourceUpdateDto resourceDto, int resourceId)
    {
        var result = await _resourceOrchestrator.UpdateResourceAsync(resourceDto, resourceId);
        return ToApiResponse(result);
    }

    [HttpDelete("{resourceId:int}")]
    [Authorize(Roles = "admin")]
    [SwaggerOperation(OperationId = "Resource_Delete")]
    public async Task<ActionResult<Result>> DeleteAsync(int resourceId)
    {
        var result = await _resourceOrchestrator.DeleteResourceAsync(resourceId);
        return ToApiResponse(result);
    }
}