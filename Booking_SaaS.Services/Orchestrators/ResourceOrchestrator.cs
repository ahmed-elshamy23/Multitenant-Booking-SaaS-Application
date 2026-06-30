using Booking_SaaS.Domain.Contracts;
using Booking_SaaS.Domain.Entities;
using Booking_SaaS.Domain.Results;
using Booking_SaaS.Services.Abstraction;
using Booking_SaaS.Services.Abstraction.DTOs;
using Booking_SaaS.Services.Abstraction.DTOs.Resource;
using Booking_SaaS.Services.Abstraction.DTOs.Schedule;
using Booking_SaaS.Services.Abstraction.Orchestrators;

namespace Booking_SaaS.Services.Orchestrators;

public class ResourceOrchestrator : IResourceOrchestrator
{
    private readonly IBookingService _bookingService;
    private readonly int _currentTenantId;
    private readonly IResourceService _resourceService;
    private readonly IScheduleService _scheduleService;
    private readonly ITenantService _tenantService;
    private readonly IUnitOfWork _unitOfWork;

    public ResourceOrchestrator(ITenantService tenantService, ITenantResolver tenantResolver,
        IResourceService resourceService, IScheduleService scheduleService, IBookingService bookingService,
        IUnitOfWork unitOfWork)
    {
        _tenantService = tenantService;
        _currentTenantId = tenantResolver.CurrentTenantId;
        _resourceService = resourceService;
        _scheduleService = scheduleService;
        _bookingService = bookingService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<PaginatedDto<ResourceResultDto>>> GetAllAsync(ResourceFilterDto filterDto,
        CancellationToken cancellationToken = default)
    {
        var isActiveTenantResult = await _tenantService.IsActiveAsync(_currentTenantId, cancellationToken);
        if (!isActiveTenantResult.IsSuccess)
            return isActiveTenantResult.Error!;
        if (!isActiveTenantResult.Value)
            return Error.Tenant.Inactive;

        return await _resourceService.GetAllAsync(filterDto, cancellationToken);
    }

    public async Task<Result<ResourceResultDto>> GetByIdAsync(int resourceId,
        CancellationToken cancellationToken = default)
    {
        var isActiveTenantResult = await _tenantService.IsActiveAsync(_currentTenantId, cancellationToken);
        if (!isActiveTenantResult.IsSuccess)
            return isActiveTenantResult.Error!;
        if (!isActiveTenantResult.Value)
            return Error.Tenant.Inactive;

        return await _resourceService.GetByIdAsync(resourceId, cancellationToken);
    }

    public async Task<Result<int>> AddResourceAsync(ResourceAddDto resourceDto)
    {
        var isActiveTenantResult = await _tenantService.IsActiveAsync(_currentTenantId);
        if (!isActiveTenantResult.IsSuccess)
            return isActiveTenantResult.Error!;
        if (!isActiveTenantResult.Value)
            return Error.Tenant.Inactive;

        return await _resourceService.AddAsync(resourceDto);
    }

    public async Task<Result> UpdateResourceAsync(ResourceUpdateDto resourceDto, int resourceId)
    {
        var isActiveTenantResult = await _tenantService.IsActiveAsync(_currentTenantId);
        if (!isActiveTenantResult.IsSuccess)
            return isActiveTenantResult.Error!;
        if (!isActiveTenantResult.Value)
            return Error.Tenant.Inactive;

        return await _resourceService.UpdateAsync(resourceDto, resourceId);
    }

    public async Task<Result> DeleteResourceAsync(int resourceId)
    {
        var isActiveTenantResult = await _tenantService.IsActiveAsync(_currentTenantId);
        if (!isActiveTenantResult.IsSuccess)
            return isActiveTenantResult.Error!;
        if (!isActiveTenantResult.Value)
            return Error.Tenant.Inactive;

        if (await _resourceService.HasSchedulesAsync(resourceId))
            return Error.Resource.ExistingSchedules;

        return await _resourceService.DeleteAsync(resourceId);
    }

    public async Task<Result> UpdateScheduleAsync(ScheduleUpdateDto scheduleDto, int resourceId, int scheduleId)
    {
        var isActiveTenantResult = await _tenantService.IsActiveAsync(_currentTenantId);
        if (!isActiveTenantResult.IsSuccess)
            return isActiveTenantResult.Error!;
        if (!isActiveTenantResult.Value)
            return Error.Tenant.Inactive;

        Result result;
        await _unitOfWork.BeginTransactionAsync();
        try
        {
            var resourceLockAcquired = await _resourceService.AcquireLockAsync(resourceId);
            if (!resourceLockAcquired)
            {
                await _unitOfWork.RollbackAsync();
                return Error.Validation.NotFound(nameof(Resource));
            }

            var scheduleLockAcquired = await _scheduleService.AcquireLockAsync(scheduleId);
            if (!scheduleLockAcquired)
            {
                await _unitOfWork.RollbackAsync();
                return Error.Validation.NotFound(nameof(Schedule));
            }

            var pendingBookingsCount = scheduleDto.Date.HasValue
                ? await _bookingService.GetPendingBookingsCountAsync(scheduleId, scheduleDto.Date.Value)
                : await _bookingService.GetPendingBookingsCountAsync(scheduleId);
            if (scheduleDto.Capacity < pendingBookingsCount)
            {
                await _unitOfWork.RollbackAsync();
                return Error.Schedule.UnmatchedCapacity;
            }

            var hasPendingBookings = pendingBookingsCount > 0;
            result = await _scheduleService.UpdateAsync(scheduleDto, resourceId, scheduleId, hasPendingBookings);
            if (!result.IsSuccess)
            {
                await _unitOfWork.RollbackAsync();
                return result;
            }

            await _unitOfWork.CommitAsync();
        }
        catch (Exception)
        {
            await _unitOfWork.RollbackAsync();
            throw;
        }

        return result;
    }

    public async Task<Result> DeleteScheduleAsync(int resourceId, int scheduleId)
    {
        var isActiveTenantResult = await _tenantService.IsActiveAsync(_currentTenantId);
        if (!isActiveTenantResult.IsSuccess)
            return isActiveTenantResult.Error!;
        if (!isActiveTenantResult.Value)
            return Error.Tenant.Inactive;

        if (!await _resourceService.IsExistingResourceAsync(resourceId))
            return Error.Validation.NotFound(nameof(Resource));

        if (await _bookingService.HasPendingBookingAsync(scheduleId))
            return Error.Schedule.PendingBookings;

        return await _scheduleService.DeleteAsync(resourceId, scheduleId);
    }

    public async Task<Result<PaginatedDto<ScheduleResultDto>>> GetResourceSchedulesAsync(ScheduleFilterDto filterDto,
        int resourceId, CancellationToken cancellationToken = default)
    {
        var isActiveTenantResult = await _tenantService.IsActiveAsync(_currentTenantId, cancellationToken);
        if (!isActiveTenantResult.IsSuccess)
            return isActiveTenantResult.Error!;
        if (!isActiveTenantResult.Value)
            return Error.Tenant.Inactive;

        return await _scheduleService.GetAllAsync(filterDto, resourceId, cancellationToken);
    }

    public async Task<Result<int>> AddScheduleAsync(ScheduleAddDto scheduleDto, int resourceId)
    {
        var isActiveTenantResult = await _tenantService.IsActiveAsync(_currentTenantId);
        if (!isActiveTenantResult.IsSuccess)
            return isActiveTenantResult.Error!;
        if (!isActiveTenantResult.Value)
            return Error.Tenant.Inactive;

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            var lockAcquired = await _resourceService.AcquireLockAsync(resourceId);
            if (!lockAcquired)
            {
                await _unitOfWork.RollbackAsync();
                return Error.Validation.NotFound(nameof(Resource));
            }

            var result = await _scheduleService.AddAsync(scheduleDto, resourceId, _currentTenantId);
            if (!result.IsSuccess)
            {
                await _unitOfWork.RollbackAsync();
                return result;
            }

            await _unitOfWork.CommitAsync();
            return result;
        }
        catch (Exception)
        {
            await _unitOfWork.RollbackAsync();
            throw;
        }
    }
}