using AutoMapper;
using Booking_SaaS.Domain.Contracts;
using Booking_SaaS.Domain.Contracts.Repositories;
using Booking_SaaS.Domain.Entities;
using Booking_SaaS.Domain.Results;
using Booking_SaaS.Services.Abstraction;
using Booking_SaaS.Services.Abstraction.DTOs;
using Booking_SaaS.Services.Abstraction.DTOs.Schedule;
using Booking_SaaS.Services.Specifications;
using FluentValidation;
using Hangfire;

namespace Booking_SaaS.Services;

internal class ScheduleService : IScheduleService
{
    private readonly IValidator<ScheduleFilterDto> _filterValidator;
    private readonly IMapper _mapper;
    private readonly IValidator<PaginatedDto<ScheduleResultDto>> _paginationValidator;
    private readonly IScheduleRepository _repo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<ScheduleAddDto> _validator;

    public ScheduleService(IUnitOfWork unitOfWork, IValidator<ScheduleFilterDto> filterValidator, IMapper mapper,
        IValidator<PaginatedDto<ScheduleResultDto>> paginationValidator, IValidator<ScheduleAddDto> validator)
    {
        _unitOfWork = unitOfWork;
        _filterValidator = filterValidator;
        _mapper = mapper;
        _paginationValidator = paginationValidator;
        _validator = validator;
        _repo = _unitOfWork.ScheduleRepository;
    }

    public async Task<Result<PaginatedDto<ScheduleResultDto>>> GetAllAsync(ScheduleFilterDto filterDto, int resourceId,
        CancellationToken cancellationToken = default)
    {
        var paginatedDto = new PaginatedDto<ScheduleResultDto>
        {
            PageIndex = filterDto.PageIndex,
            PageSize = filterDto.PageSize
        };
        var validationResult = _paginationValidator.Validate(paginatedDto);
        if (!validationResult.IsValid)
            return Error.Validation.InvalidParameters(validationResult.Errors
                .Select(x => x.ErrorMessage));

        validationResult = _filterValidator.Validate(filterDto);
        if (!validationResult.IsValid)
            return Error.Validation.InvalidParameters(validationResult.Errors
                .Select(x => x.ErrorMessage));

        var filterSpecification = new ScheduleSpecifications.FilterSpecification(filterDto, resourceId);
        paginatedDto.TotalCount = await _repo.GetCountAsync(filterSpecification, cancellationToken);

        var paginationSpecification = new ScheduleSpecifications.PaginatedFilterSpecification(filterDto, resourceId);
        paginatedDto.Items =
            _mapper.Map<List<Schedule>, List<ScheduleResultDto>>(await _repo.GetAllAsync(paginationSpecification,
                cancellationToken));

        return paginatedDto;
    }

    public async Task<Result<ScheduleResultDto>> GetByIdAsync(int scheduleId,
        CancellationToken cancellationToken = default)
    {
        var schedule = await _repo.GetByIdAsync(scheduleId, cancellationToken);
        if (schedule == null)
            return Error.Validation.NotFound(nameof(Schedule));

        return _mapper.Map<ScheduleResultDto>(schedule);
    }

    public async Task<Result<ScheduleResultDto>> GetAndLockByIdAsync(int scheduleId,
        CancellationToken cancellationToken = default)
    {
        var schedule = await _repo.GetAndLockByIdAsync(scheduleId, cancellationToken);
        if (schedule == null)
            return Error.Validation.NotFound(nameof(Schedule));

        return _mapper.Map<ScheduleResultDto>(schedule);
    }

    public async Task<bool> AcquireLockAsync(int scheduleId, CancellationToken cancellationToken = default)
    {
        return await _repo.AcquireLockAsync(scheduleId, cancellationToken);
    }

    public async Task<Result<int>> AddAsync(ScheduleAddDto scheduleDto, int resourceId, int currentTenantId)
    {
        var validationResult = _validator.Validate(scheduleDto);
        if (!validationResult.IsValid)
            return Error.Validation.InvalidParameters(validationResult.Errors
                .Select(x => x.ErrorMessage));

        if (await CheckDuplicateScheduleAsync(scheduleDto, resourceId, 0))
            return Error.Schedule.Duplicate;

        scheduleDto.DayOfWeek ??= scheduleDto.Date!.Value.DayOfWeek;

        var schedule = _mapper.Map<Schedule>(scheduleDto);
        schedule.ResourceId = resourceId;
        _repo.Add(schedule);
        await _unitOfWork.SaveChangesAsync();

        RegisterAndAttachJobs(schedule, scheduleDto, currentTenantId);
        await _unitOfWork.SaveChangesAsync();

        return schedule.Id;
    }

    public async Task<Result> UpdateAsync(ScheduleUpdateDto scheduleDto, int resourceId, int scheduleId,
        bool hasPendingBookings)
    {
        var validationResult = _validator.Validate(scheduleDto);
        if (!validationResult.IsValid)
            return Error.Validation.InvalidParameters(validationResult.Errors
                .Select(x => x.ErrorMessage));

        var existingSchedule = await _repo.GetByIdAsync(scheduleId);
        if (existingSchedule == null || existingSchedule.ResourceId != resourceId)
            return Error.Validation.NotFound(nameof(Schedule));

        if (existingSchedule.Date.HasValue &&
            existingSchedule.Date.Value.ToDateTime(existingSchedule.StartTime, DateTimeKind.Utc) <= DateTime.UtcNow)
            return Error.Schedule.OldSchedule;

        if (existingSchedule.Date.HasValue != scheduleDto.Date.HasValue)
            return Error.Schedule.ScheduleConversion;

        if (existingSchedule.AllowsMultiple != scheduleDto.AllowsMultiple)
            return Error.Schedule.MultipleSupportChange;

        if (scheduleDto.DayOfWeek == null)
            scheduleDto.DayOfWeek = scheduleDto.Date!.Value.DayOfWeek;

        var isDateOrTimeChanged = IsDateOrTimeChanged(existingSchedule, scheduleDto);
        if (isDateOrTimeChanged)
        {
            if (hasPendingBookings)
                return Error.Schedule.UpdateConflict;

            if (await CheckDuplicateScheduleAsync(scheduleDto, resourceId, scheduleId))
                return Error.Schedule.Duplicate;
        }

        _mapper.Map(scheduleDto, existingSchedule);
        _repo.Update(existingSchedule);
        await _unitOfWork.SaveChangesAsync();

        if (!isDateOrTimeChanged)
            return Result.Success();

        if (existingSchedule.Date.HasValue)
        {
            BackgroundJob.Delete(existingSchedule.InProgressJobId);
            BackgroundJob.Delete(existingSchedule.CompletedJobId);
        }
        else
        {
            RecurringJob.RemoveIfExists(existingSchedule.InProgressJobId);
            RecurringJob.RemoveIfExists(existingSchedule.CompletedJobId);
        }

        RegisterAndAttachJobs(existingSchedule, scheduleDto, existingSchedule.TenantId);
        await _unitOfWork.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result> DeleteAsync(int resourceId, int scheduleId)
    {
        var existingSchedule = await _repo.GetByIdAsync(scheduleId);
        if (existingSchedule == null || existingSchedule.ResourceId != resourceId)
            return Error.Validation.NotFound(nameof(Schedule));

        if (existingSchedule.Date.HasValue &&
            existingSchedule.Date.Value.ToDateTime(existingSchedule.StartTime, DateTimeKind.Utc) <= DateTime.UtcNow)
            return Error.Schedule.OldSchedule;

        var inProgressJobId = existingSchedule.InProgressJobId;
        var completedJobId = existingSchedule.CompletedJobId;

        _repo.Delete(existingSchedule);
        await _unitOfWork.SaveChangesAsync();

        if (existingSchedule.Date.HasValue)
        {
            BackgroundJob.Delete(inProgressJobId);
            BackgroundJob.Delete(completedJobId);
        }
        else
        {
            RecurringJob.RemoveIfExists(inProgressJobId);
            RecurringJob.RemoveIfExists(completedJobId);
        }

        return Result.Success();
    }

    [AutomaticRetry(Attempts = 5)]
    public async Task MarkInProgressAndSaveSnapshotAsync(int scheduleId, int tenantId, bool isRecurringSchedule)
    {
        var schedule = await _repo.GetByIdAsync(scheduleId);
        if (schedule == null)
            return;

        var targetDate = isRecurringSchedule
            ? DateOnly.FromDateTime(DateTime.UtcNow)
            : schedule.Date!.Value;
        await _repo.MarkInProgressAsync(schedule, targetDate, tenantId);
    }

    [AutomaticRetry(Attempts = 5)]
    public async Task MarkCompletedAsync(int scheduleId, int tenantId, bool isRecurringSchedule)
    {
        var schedule = await _repo.GetByIdAsync(scheduleId);
        if (schedule == null)
            return;

        var targetDate = isRecurringSchedule
            ? DateOnly.FromDateTime(DateTime.UtcNow)
            : schedule.Date!.Value;
        await _repo.MarkCompletedAsync(scheduleId, targetDate, tenantId);
    }

    private async Task<bool> CheckDuplicateScheduleAsync(ScheduleAddDto scheduleDto, int resourceId, int scheduleId)
    {
        ScheduleSpecifications.CheckDuplicateSpecification specification;
        if (scheduleDto.DayOfWeek != null)
            specification =
                new ScheduleSpecifications.CheckDuplicateSpecification(scheduleId, resourceId,
                    scheduleDto.DayOfWeek.Value, scheduleDto.StartTime, scheduleDto.EndTime);
        else
            specification =
                new ScheduleSpecifications.CheckDuplicateSpecification(scheduleId, resourceId,
                    scheduleDto.Date!.Value, scheduleDto.StartTime, scheduleDto.EndTime);

        return await _repo.AnyAsync(specification);
    }

    private static bool IsDateOrTimeChanged(Schedule existingSchedule, ScheduleUpdateDto scheduleDto)
    {
        return existingSchedule.DayOfWeek != scheduleDto.DayOfWeek
               || existingSchedule.Date != scheduleDto.Date
               || existingSchedule.StartTime != scheduleDto.StartTime
               || existingSchedule.EndTime != scheduleDto.EndTime;
    }

    private void RegisterAndAttachJobs(Schedule schedule, ScheduleAddDto scheduleDto, int currentTenantId)
    {
        string inProgressJobId;
        string completedJobId;
        if (scheduleDto.Date.HasValue)
        {
            var inProgressDelay = scheduleDto.Date.Value.ToDateTime(scheduleDto.StartTime, DateTimeKind.Utc) -
                                  DateTime.UtcNow;
            var completedDelay = scheduleDto.Date.Value.ToDateTime(scheduleDto.EndTime, DateTimeKind.Utc) -
                                 DateTime.UtcNow;
            inProgressJobId = BackgroundJob.Schedule(
                () => MarkInProgressAndSaveSnapshotAsync(schedule.Id, currentTenantId, false),
                inProgressDelay < TimeSpan.Zero ? TimeSpan.Zero : inProgressDelay);

            completedJobId = BackgroundJob.Schedule(
                () => MarkCompletedAsync(schedule.Id, currentTenantId, false),
                completedDelay < TimeSpan.Zero ? TimeSpan.Zero : completedDelay);
        }
        else
        {
            var inProgressCron =
                $"{scheduleDto.StartTime.Minute} {scheduleDto.StartTime.Hour} * * {(int)scheduleDto.DayOfWeek!}";
            var completedCron =
                $"{scheduleDto.EndTime.Minute} {scheduleDto.EndTime.Hour} * * {(int)scheduleDto.DayOfWeek}";

            inProgressJobId = $"inprogress-{schedule.Id}";
            completedJobId = $"completed-{schedule.Id}";

            RecurringJob.AddOrUpdate(
                inProgressJobId,
                () => MarkInProgressAndSaveSnapshotAsync(schedule.Id, currentTenantId, true),
                inProgressCron,
                new RecurringJobOptions
                {
                    TimeZone = TimeZoneInfo.Utc
                });

            RecurringJob.AddOrUpdate(
                completedJobId,
                () => MarkCompletedAsync(schedule.Id, currentTenantId, true),
                completedCron,
                new RecurringJobOptions
                {
                    TimeZone = TimeZoneInfo.Utc
                });
        }

        schedule.InProgressJobId = inProgressJobId;
        schedule.CompletedJobId = completedJobId;
    }
}