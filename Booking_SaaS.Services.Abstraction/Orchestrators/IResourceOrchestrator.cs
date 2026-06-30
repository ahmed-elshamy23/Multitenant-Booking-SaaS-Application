using Booking_SaaS.Domain.Results;
using Booking_SaaS.Services.Abstraction.DTOs;
using Booking_SaaS.Services.Abstraction.DTOs.Resource;
using Booking_SaaS.Services.Abstraction.DTOs.Schedule;

namespace Booking_SaaS.Services.Abstraction.Orchestrators;

public interface IResourceOrchestrator
{
    Task<Result<PaginatedDto<ResourceResultDto>>> GetAllAsync(ResourceFilterDto filterDto,
        CancellationToken cancellationToken = default);

    Task<Result<ResourceResultDto>> GetByIdAsync(int resourceId, CancellationToken cancellationToken = default);

    Task<Result<int>> AddResourceAsync(ResourceAddDto resourceDto);
    Task<Result> UpdateResourceAsync(ResourceUpdateDto resourceDto, int resourceId);
    Task<Result> DeleteResourceAsync(int resourceId);

    Task<Result<PaginatedDto<ScheduleResultDto>>> GetResourceSchedulesAsync(ScheduleFilterDto filterDto,
        int resourceId, CancellationToken cancellationToken = default);

    Task<Result<int>> AddScheduleAsync(ScheduleAddDto scheduleDto, int resourceId);
    Task<Result> UpdateScheduleAsync(ScheduleUpdateDto scheduleDto, int resourceId, int scheduleId);
    Task<Result> DeleteScheduleAsync(int resourceId, int scheduleId);
}