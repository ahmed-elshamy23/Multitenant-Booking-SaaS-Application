using Booking_SaaS.API.ActionFilters;
using Booking_SaaS.API.Attributes;
using Booking_SaaS.Domain.Results;
using Booking_SaaS.Services.Abstraction;
using Booking_SaaS.Services.Abstraction.DTOs;
using Booking_SaaS.Services.Abstraction.DTOs.Schedule;
using Booking_SaaS.Services.Abstraction.Orchestrators;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Booking_SaaS.API.Controllers;

[Route("api/resources/{resourceId:int}/schedules")]
public class SchedulesController : ApiController
{
    private readonly IResourceOrchestrator _resourceOrchestrator;

    public SchedulesController(IServiceManager serviceManager)
    {
        _resourceOrchestrator = serviceManager.ResourceOrchestrator;
    }

    [HttpGet]
    [Authorize(Roles = "admin,user")]
    [SwaggerOperation(OperationId = "Resource_GetSchedules")]
    public async Task<ActionResult<Result<PaginatedDto<ScheduleResultDto>>>> GetAllAsync(
        [FromQuery] ScheduleFilterDto filterDto, int resourceId, CancellationToken cancellationToken = default)
    {
        var result = await _resourceOrchestrator.GetResourceSchedulesAsync(filterDto, resourceId, cancellationToken);
        return ToApiResponse(result);
    }

    [HttpPost]
    [Authorize(Roles = "admin")]
    [SwaggerOperation(OperationId = "Schedule_Add")]
    [ServiceFilter(typeof(IdempotencyFilter))]
    [RequireIdempotencyKey]
    public async Task<ActionResult<Result<int>>> AddAsync(ScheduleAddDto scheduleDto, int resourceId)
    {
        var result = await _resourceOrchestrator.AddScheduleAsync(scheduleDto, resourceId);
        return ToApiResponse(result);
    }

    [HttpPut("{scheduleId:int}")]
    [Authorize(Roles = "admin")]
    [SwaggerOperation(OperationId = "Schedule_Update")]
    [ServiceFilter(typeof(IdempotencyFilter))]
    [RequireIdempotencyKey]
    public async Task<ActionResult<Result>> UpdateAsync(ScheduleUpdateDto scheduleDto, int resourceId, int scheduleId)
    {
        var result = await _resourceOrchestrator.UpdateScheduleAsync(scheduleDto, resourceId, scheduleId);
        return ToApiResponse(result);
    }

    [HttpDelete("{scheduleId:int}")]
    [Authorize(Roles = "admin")]
    [SwaggerOperation(OperationId = "Schedule_Delete")]
    public async Task<ActionResult<Result>> DeleteAsync(int resourceId, int scheduleId)
    {
        var result = await _resourceOrchestrator.DeleteScheduleAsync(resourceId, scheduleId);
        return ToApiResponse(result);
    }
}