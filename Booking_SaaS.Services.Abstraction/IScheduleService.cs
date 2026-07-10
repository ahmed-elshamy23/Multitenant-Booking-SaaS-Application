using Booking_SaaS.Domain.Results;
using Booking_SaaS.Services.Abstraction.DTOs;
using Booking_SaaS.Services.Abstraction.DTOs.Schedule;

namespace Booking_SaaS.Services.Abstraction;

public interface IScheduleService
{
    Task<Result<PaginatedDto<ScheduleResultDto>>> GetAllAsync(ScheduleFilterDto filterDto, int resourceId,
        CancellationToken cancellationToken = default);

    Task<Result<ScheduleResultDto>> GetByIdAsync(int scheduleId, CancellationToken cancellationToken = default);
    Task<Result<ScheduleResultDto>> GetAndLockByIdAsync(int scheduleId, CancellationToken cancellationToken = default);
    Task<bool> AcquireLockAsync(int scheduleId, CancellationToken cancellationToken = default);
    Task<Result<int>> AddAsync(ScheduleAddDto scheduleDto, int resourceId, int currentTenantId);
    Task<Result> UpdateAsync(ScheduleUpdateDto scheduleDto, int resourceId, int scheduleId, bool hasPendingBookings);
    Task<Result> DeleteAsync(int resourceId, int scheduleId);
}