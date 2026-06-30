using Booking_SaaS.Domain.Results;
using Booking_SaaS.Services.Abstraction.DTOs;
using Booking_SaaS.Services.Abstraction.DTOs.Resource;

namespace Booking_SaaS.Services.Abstraction;

public interface IResourceService
{
    Task<Result<PaginatedDto<ResourceResultDto>>> GetAllAsync(ResourceFilterDto filterDto,
        CancellationToken cancellationToken = default);

    Task<Result<ResourceResultDto>> GetByIdAsync(int resourceId, CancellationToken cancellationToken = default);

    Task<Result<int>> AddAsync(ResourceAddDto resourceDto);
    Task<Result> UpdateAsync(ResourceUpdateDto resourceDto, int resourceId);
    Task<Result> DeleteAsync(int resourceId);
    Task<bool> HasSchedulesAsync(int resourceId);
    Task<bool> IsExistingResourceAsync(int resourceId);
    Task<bool> AcquireLockAsync(int resourceId, CancellationToken cancellationToken = default);
}