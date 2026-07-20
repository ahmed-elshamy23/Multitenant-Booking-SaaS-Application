using AutoMapper;
using Booking_SaaS.Domain.Contracts;
using Booking_SaaS.Domain.Contracts.Repositories;
using Booking_SaaS.Domain.Entities;
using Booking_SaaS.Domain.Enums;
using Booking_SaaS.Domain.Results;
using Booking_SaaS.Services;
using Booking_SaaS.Services.Abstraction.DTOs;
using Booking_SaaS.Services.Abstraction.DTOs.Schedule;
using Booking_SaaS.Services.Specifications;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using Moq;

namespace Booking_SaaS.Tests.Services;

public class ScheduleServiceTests
{
    private readonly Mock<IValidator<ScheduleFilterDto>> _filterValidator = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly Mock<IValidator<PaginatedDto<ScheduleResultDto>>> _paginationValidator = new();
    private readonly Mock<IScheduleRepository> _repo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IValidator<ScheduleAddDto>> _validator = new();
    private readonly Mock<IBackgroundJobClient> _backgroundJobClient = new();
    private readonly Mock<IRecurringJobManager> _recurringJobManager = new();
    private readonly ScheduleService _scheduleService;

    #region TestData

    public static TheoryData<DateOnly?, DateOnly?> ScheduleTypeMismatchData => new()
    {
        { DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2)), null },
        { null, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2)) }
    };

    public static TheoryData<Schedule, ScheduleUpdateDto> RecurringDateOrTimeChangedData => new()
    {
        {
            new Schedule
            {
                DayOfWeek = DayOfWeek.Monday,
                StartTime = new TimeOnly(9, 0),
                EndTime = new TimeOnly(17, 0)
            },
            new ScheduleUpdateDto
            {
                DayOfWeek = DayOfWeek.Tuesday,
                StartTime = new TimeOnly(9, 0),
                EndTime = new TimeOnly(17, 0)
            }
        },
        {
            new Schedule
            {
                DayOfWeek = DayOfWeek.Monday,
                StartTime = new TimeOnly(9, 0),
                EndTime = new TimeOnly(17, 0)
            },
            new ScheduleUpdateDto
            {
                DayOfWeek = DayOfWeek.Monday,
                StartTime = new TimeOnly(10, 0),
                EndTime = new TimeOnly(17, 0)
            }
        },
        {
            new Schedule
            {
                DayOfWeek = DayOfWeek.Monday,
                StartTime = new TimeOnly(9, 0),
                EndTime = new TimeOnly(17, 0)
            },
            new ScheduleUpdateDto
            {
                DayOfWeek = DayOfWeek.Monday,
                StartTime = new TimeOnly(9, 0),
                EndTime = new TimeOnly(18, 0)
            }
        }
    };

    public static TheoryData<Schedule, ScheduleUpdateDto> NonRecurringDateOrTimeChangedData => new()
    {
        {
            new Schedule
            {
                Date = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(2),
                StartTime = new TimeOnly(9, 0),
                EndTime = new TimeOnly(17, 0)
            },
            new ScheduleUpdateDto
            {
                Date = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(3),
                StartTime = new TimeOnly(9, 0),
                EndTime = new TimeOnly(17, 0)
            }
        },
        {
            new Schedule
            {
                Date = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(2),
                StartTime = new TimeOnly(9, 0),
                EndTime = new TimeOnly(17, 0)
            },
            new ScheduleUpdateDto
            {
                Date = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(2),
                StartTime = new TimeOnly(10, 0),
                EndTime = new TimeOnly(17, 0)
            }
        },
        {
            new Schedule
            {
                Date = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(2),
                StartTime = new TimeOnly(9, 0),
                EndTime = new TimeOnly(17, 0)
            },
            new ScheduleUpdateDto
            {
                Date = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(2),
                StartTime = new TimeOnly(9, 0),
                EndTime = new TimeOnly(18, 0)
            }
        }
    };

    #endregion

    public ScheduleServiceTests()
    {
        _unitOfWork.Setup(x => x.ScheduleRepository).Returns(_repo.Object);

        _scheduleService = new ScheduleService(_unitOfWork.Object,
                                               _filterValidator.Object,
                                               _mapper.Object,
                                               _paginationValidator.Object,
                                               _validator.Object,
                                               _backgroundJobClient.Object,
                                               _recurringJobManager.Object);
    }

    [Fact]
    public async Task GetAllAsync_ShouldFailWhenPaginationValidationErrorsExist()
    {
        var fakeFailures = new List<ValidationFailure> { new("Property", "Error") };
        _paginationValidator.Setup(x => x.Validate(It.IsAny<PaginatedDto<ScheduleResultDto>>()))
                            .Returns(new ValidationResult(fakeFailures));

        var filterDto = new ScheduleFilterDto();
        var result = await _scheduleService.GetAllAsync(filterDto, 1);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull().And.BeOfType<Error>();
        result.Error.ErrorType.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public async Task GetAllAsync_ShouldFailWhenFilterValidationErrorsExist()
    {
        _paginationValidator.Setup(x => x.Validate(It.IsAny<PaginatedDto<ScheduleResultDto>>()))
                            .Returns(new ValidationResult());

        var fakeFailures = new List<ValidationFailure> { new("Property", "Error") };
        _filterValidator.Setup(x => x.Validate(It.IsAny<ScheduleFilterDto>()))
                        .Returns(new ValidationResult(fakeFailures));

        var filterDto = new ScheduleFilterDto();
        var result = await _scheduleService.GetAllAsync(filterDto, 1);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull().And.BeOfType<Error>();
        result.Error.ErrorType.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public async Task GetAllAsync_ShouldPassAndReturnPaginatedData()
    {
        _paginationValidator.Setup(x => x.Validate(It.IsAny<PaginatedDto<ScheduleResultDto>>()))
                            .Returns(new ValidationResult());

        _filterValidator.Setup(x => x.Validate(It.IsAny<ScheduleFilterDto>()))
                        .Returns(new ValidationResult());

        var filterDto = new ScheduleFilterDto();
        var totalCount = 5;

        _repo.Setup(x => x.GetCountAsync(It.IsAny<ScheduleSpecifications.FilterSpecification>(),
                                         It.IsAny<CancellationToken>()))
                                        .ReturnsAsync(totalCount);

        var mockResources = new List<Schedule> { new(), new() };
        _repo.Setup(x => x.GetAllAsync(It.IsAny<ScheduleSpecifications.PaginatedFilterSpecification>(),
                                       It.IsAny<CancellationToken>()))
                                      .ReturnsAsync(mockResources);

        var mockDtos = new List<ScheduleResultDto> { new(), new() };
        _mapper.Setup(x => x.Map<List<Schedule>, List<ScheduleResultDto>>(It.IsAny<List<Schedule>>()))
               .Returns(mockDtos);

        var result = await _scheduleService.GetAllAsync(filterDto, 1);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.TotalCount.Should().Be(totalCount);
        result.Value.Items.Should().BeEquivalentTo(mockDtos);
        result.Value.PageIndex.Should().Be(filterDto.PageIndex);
        result.Value.PageSize.Should().Be(filterDto.PageSize);
        result.Error.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_ShouldFailWhenResourceNotFound()
    {
        _repo.Setup(x => x.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync((Schedule?)null);

        var result = await _scheduleService.GetByIdAsync(1);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull().And.BeOfType<Error>();
        result.Error.ErrorType.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task GetAndLockByIdAsync_ShouldPassWhenResourceExists()
    {
        var dto = new ScheduleResultDto();
        _repo.Setup(x => x.GetAndLockByIdAsync(1, It.IsAny<CancellationToken>()))
             .ReturnsAsync(new Schedule());

        _mapper.Setup(x => x.Map<ScheduleResultDto>(It.IsAny<Schedule>()))
               .Returns(dto);

        var result = await _scheduleService.GetAndLockByIdAsync(1);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(dto);
        result.Error.Should().BeNull();
    }

    [Fact]
    public async Task GetAndLockByIdAsync_ShouldFailWhenResourceNotFound()
    {
        _repo.Setup(x => x.GetAndLockByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync((Schedule?)null);

        var result = await _scheduleService.GetAndLockByIdAsync(1);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull().And.BeOfType<Error>();
        result.Error.ErrorType.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldPassWhenResourceExists()
    {
        var dto = new ScheduleResultDto();
        _repo.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
             .ReturnsAsync(new Schedule());

        _mapper.Setup(x => x.Map<ScheduleResultDto>(It.IsAny<Schedule>()))
               .Returns(dto);

        var result = await _scheduleService.GetByIdAsync(1);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(dto);
        result.Error.Should().BeNull();
    }

    [Fact]
    public async Task MarkInProgressAsync_ShouldReturnEarlyWhenScheduleDoesNotExist()
    {
        _repo.Setup(x => x.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync((Schedule?)null);

        await _scheduleService.MarkInProgressAndSaveSnapshotAsync(1, 1, false);

        _repo.Verify(x => x.MarkInProgressAsync(It.IsAny<Schedule>(),
                                                It.IsAny<DateOnly>(),
                                                It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task MarkInProgressAsync_ShouldUseScheduleDateWhenNotRecurring()
    {
        var scheduleDate = new DateOnly(2026, 1, 1);
        _repo.Setup(x => x.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(new Schedule { Date = scheduleDate });

        await _scheduleService.MarkInProgressAndSaveSnapshotAsync(1, 99, false);

        _repo.Verify(x => x.MarkInProgressAsync(It.IsAny<Schedule>(), scheduleDate, It.IsAny<int>()), Times.Once);
    }

    [Fact]
    public async Task MarkInProgressAsync_ShouldUseCurrentDateWhenRecurring()
    {
        DateOnly? date = null;
        _repo.Setup(x => x.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(new Schedule());

        _repo.Setup(x => x.MarkInProgressAsync(It.IsAny<Schedule>(),
                                               It.IsAny<DateOnly>(),
                                               It.IsAny<int>()))
                          .Callback<Schedule, DateOnly, int>((s, d, t) => date = d);

        await _scheduleService.MarkInProgressAndSaveSnapshotAsync(1, 99, true);

        date.Should().NotBeNull();
        date.Should().Be(DateOnly.FromDateTime(DateTime.UtcNow));

        _repo.Verify(x => x.MarkInProgressAsync(It.IsAny<Schedule>(),
                                                It.IsAny<DateOnly>(),
                                                It.IsAny<int>()), Times.Once);
    }

    [Fact]
    public async Task MarkCompletedAsync_ShouldReturnEarlyWhenScheduleDoesNotExist()
    {
        _repo.Setup(x => x.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync((Schedule?)null);

        await _scheduleService.MarkCompletedAsync(1, 1, false);

        _repo.Verify(x => x.MarkCompletedAsync(It.IsAny<int>(),
                                               It.IsAny<DateOnly>(),
                                               It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task MarkCompletedAsync_ShouldUseScheduleDateWhenNotRecurring()
    {
        var scheduleDate = new DateOnly(2026, 1, 1);
        var schedule = new Schedule { Date = scheduleDate };
        _repo.Setup(x => x.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(schedule);

        await _scheduleService.MarkCompletedAsync(1, 99, false);

        _repo.Verify(x => x.MarkCompletedAsync(It.IsAny<int>(), scheduleDate, It.IsAny<int>()), Times.Once);
    }

    [Fact]
    public async Task MarkCompletedAsync_ShouldUseCurrentDateWhenRecurring()
    {
        DateOnly? date = null;
        _repo.Setup(x => x.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(new Schedule());

        _repo.Setup(x => x.MarkCompletedAsync(It.IsAny<int>(),
                                              It.IsAny<DateOnly>(),
                                              It.IsAny<int>()))
                          .Callback<int, DateOnly, int>((s, d, t) => date = d);

        await _scheduleService.MarkCompletedAsync(1, 99, true);

        date.Should().NotBeNull();
        date.Should().Be(DateOnly.FromDateTime(DateTime.UtcNow));

        _repo.Verify(x => x.MarkCompletedAsync(It.IsAny<int>(),
                                               It.IsAny<DateOnly>(),
                                               It.IsAny<int>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ShouldFailWhenScheduleDoesNotExist()
    {
        _repo.Setup(x => x.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync((Schedule?)null);

        var result = await _scheduleService.DeleteAsync(1, 1);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error.ErrorType.Should().Be(ErrorType.NotFound);

        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_ShouldFailWhenResourceIdDoesNotMatch()
    {
        var retrievedResourceId = 1;
        var requestedResourceId = 2;
        _repo.Setup(x => x.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(new Schedule() { ResourceId = retrievedResourceId });

        var result = await _scheduleService.DeleteAsync(requestedResourceId, 1);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error.ErrorType.Should().Be(ErrorType.NotFound);

        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_ShouldFailWhenScheduleIsOld()
    {
        var resourceId = 1;
        var pastDate = DateTime.UtcNow.AddDays(-2);
        var schedule = new Schedule
        {
            ResourceId = resourceId,
            Date = DateOnly.FromDateTime(pastDate),
            StartTime = TimeOnly.FromDateTime(pastDate)
        };

        _repo.Setup(x => x.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(schedule);

        var result = await _scheduleService.DeleteAsync(resourceId, 1);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(Error.Schedule.OldSchedule);

        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_ShouldPassAndDeleteBackgroundJobsWhenScheduleIsFutureAndNonRecurring()
    {
        var resourceId = 1;
        var futureDate = DateTime.UtcNow.AddDays(1);
        var schedule = new Schedule
        {
            ResourceId = resourceId,
            Date = DateOnly.FromDateTime(futureDate),
            StartTime = TimeOnly.FromDateTime(futureDate)
        };

        _repo.Setup(x => x.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(schedule);

        var result = await _scheduleService.DeleteAsync(resourceId, 1);

        result.IsSuccess.Should().BeTrue();
        result.Error.Should().BeNull();

        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        _backgroundJobClient.Verify(x => x.ChangeState(It.IsAny<string>(),
                                It.Is<IState>(state => state.Name == DeletedState.StateName),
                                It.IsAny<string>()), Times.Exactly(2));
    }

    [Fact]
    public async Task DeleteAsync_ShouldPassAndDeleteRecurringJobsWhenScheduleIsRecurring()
    {
        var resourceId = 1;
        var schedule = new Schedule
        {
            ResourceId = resourceId,
            StartTime = TimeOnly.FromDateTime(DateTime.UtcNow.AddDays(1))
        };

        _repo.Setup(x => x.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(schedule);

        var result = await _scheduleService.DeleteAsync(resourceId, 1);

        result.IsSuccess.Should().BeTrue();
        result.Error.Should().BeNull();

        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        _recurringJobManager.Verify(x => x.RemoveIfExists(It.IsAny<string>()), Times.Exactly(2));
    }

    [Fact]
    public async Task AddAsync_ShouldFailWhenValidationErrorsExist()
    {
        var fakeFailures = new List<ValidationFailure> { new("Property", "Error") };
        _validator.Setup(x => x.Validate(It.IsAny<ScheduleAddDto>()))
                            .Returns(new ValidationResult(fakeFailures));

        var dto = new ScheduleAddDto();
        var result = await _scheduleService.AddAsync(dto, 1, 1);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull().And.BeOfType<Error>();
        result.Error.ErrorType.Should().Be(ErrorType.Validation);

        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddAsync_ShouldFailWhenRecurringScheduleAlreadyExists()
    {
        _validator.Setup(x => x.Validate(It.IsAny<ScheduleAddDto>()))
                  .Returns(new ValidationResult());

        _repo.Setup(x => x.AnyAsync(It.IsAny<ScheduleSpecifications.CheckDuplicateSpecification>(),
                                    It.IsAny<CancellationToken>()))
                                    .ReturnsAsync(true);

        var dto = new ScheduleAddDto() { DayOfWeek = 0 };
        var result = await _scheduleService.AddAsync(dto, 1, 1);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(Error.Schedule.Duplicate);

        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddAsync_ShouldFailWhenNonRecurringScheduleAlreadyExists()
    {
        _validator.Setup(x => x.Validate(It.IsAny<ScheduleAddDto>()))
                  .Returns(new ValidationResult());

        _repo.Setup(x => x.AnyAsync(It.IsAny<ScheduleSpecifications.CheckDuplicateSpecification>(),
                                    It.IsAny<CancellationToken>()))
                                    .ReturnsAsync(true);

        var dto = new ScheduleAddDto() { Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2)) };
        var result = await _scheduleService.AddAsync(dto, 1, 1);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(Error.Schedule.Duplicate);

        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddAsync_ShouldPassAndRegisterJobsWhenRecurringScheduleSuccessfullyAdded()
    {
        _validator.Setup(x => x.Validate(It.IsAny<ScheduleAddDto>()))
                  .Returns(new ValidationResult());

        _repo.Setup(x => x.AnyAsync(It.IsAny<ScheduleSpecifications.CheckDuplicateSpecification>(),
                                    It.IsAny<CancellationToken>()))
                                    .ReturnsAsync(false);

        var scheduleId = 1;
        var resourceId = 2;
        var schedule = new Schedule() { Id = scheduleId };
        _mapper.Setup(x => x.Map<Schedule>(It.IsAny<ScheduleAddDto>()))
               .Returns(schedule);

        var dto = new ScheduleAddDto() { DayOfWeek = DayOfWeek.Sunday };
        var result = await _scheduleService.AddAsync(dto, resourceId, 1);

        result.IsSuccess.Should().BeTrue();
        result.Error.Should().BeNull();
        result.Value.Should().Be(scheduleId);

        schedule.Should().NotBeNull();
        schedule.ResourceId.Should().Be(resourceId);
        schedule.InProgressJobId.Should().NotBeNullOrEmpty();
        schedule.CompletedJobId.Should().NotBeNullOrEmpty();

        _recurringJobManager.Verify(x => x.AddOrUpdate(It.IsAny<string>(),
                                                       It.IsAny<Job>(),
                                                       It.IsAny<string>(),
                                                       It.IsAny<RecurringJobOptions>())
                                                       , Times.Exactly(2));

        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task AddAsync_ShouldPassAndRegisterJobsWhenNonRecurringScheduleSuccessfullyAdded()
    {
        _validator.Setup(x => x.Validate(It.IsAny<ScheduleAddDto>()))
                  .Returns(new ValidationResult());

        _repo.Setup(x => x.AnyAsync(It.IsAny<ScheduleSpecifications.CheckDuplicateSpecification>(),
                                    It.IsAny<CancellationToken>()))
                                    .ReturnsAsync(false);

        var jobId = "1";
        _backgroundJobClient.Setup(x => x.Create(It.IsAny<Job>(),
                                                 It.IsAny<ScheduledState>()))
                                         .Returns(jobId);

        var scheduleId = 1;
        var resourceId = 2;
        var schedule = new Schedule() { Id = scheduleId };
        _mapper.Setup(x => x.Map<Schedule>(It.IsAny<ScheduleAddDto>()))
               .Returns(schedule);

        var dto = new ScheduleAddDto() { Date = DateOnly.FromDateTime(DateTime.UtcNow) };
        var result = await _scheduleService.AddAsync(dto, resourceId, 1);

        dto.DayOfWeek.Should().Be(DateOnly.FromDateTime(DateTime.UtcNow).DayOfWeek);

        result.IsSuccess.Should().BeTrue();
        result.Error.Should().BeNull();
        result.Value.Should().Be(scheduleId);

        schedule.Should().NotBeNull();
        schedule.ResourceId.Should().Be(resourceId);
        schedule.InProgressJobId.Should().Be(jobId);
        schedule.CompletedJobId.Should().Be(jobId);

        _backgroundJobClient.Verify(x => x.Create(It.IsAny<Job>(),
                                                  It.IsAny<ScheduledState>())
                                                  , Times.Exactly(2));

        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task UpdateAsync_ShouldFailWhenValidationErrorsExist()
    {
        var fakeFailures = new List<ValidationFailure> { new("Property", "Error") };
        _validator.Setup(x => x.Validate(It.IsAny<ScheduleAddDto>()))
                            .Returns(new ValidationResult(fakeFailures));

        var dto = new ScheduleUpdateDto();
        var result = await _scheduleService.UpdateAsync(dto, 1, 1, false);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull().And.BeOfType<Error>();
        result.Error.ErrorType.Should().Be(ErrorType.Validation);

        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_ShouldFailWhenScheduleDoesNotExist()
    {
        _validator.Setup(x => x.Validate(It.IsAny<ScheduleAddDto>()))
                            .Returns(new ValidationResult());

        _repo.Setup(x => x.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync((Schedule?)null);

        var dto = new ScheduleUpdateDto();
        var result = await _scheduleService.UpdateAsync(dto, 1, 1, false);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error.ErrorType.Should().Be(ErrorType.NotFound);

        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_ShouldFailWhenResourceIdDoesNotMatch()
    {
        _validator.Setup(x => x.Validate(It.IsAny<ScheduleAddDto>()))
                            .Returns(new ValidationResult());

        var retrievedResourceId = 1;
        var requestedResourceId = 2;
        _repo.Setup(x => x.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(new Schedule() { ResourceId = retrievedResourceId });

        var dto = new ScheduleUpdateDto();
        var result = await _scheduleService.UpdateAsync(dto, requestedResourceId, 1, false);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error.ErrorType.Should().Be(ErrorType.NotFound);

        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_ShouldFailWhenScheduleIsOld()
    {
        _validator.Setup(x => x.Validate(It.IsAny<ScheduleAddDto>()))
                            .Returns(new ValidationResult());

        var pastDate = DateTime.UtcNow.AddDays(-2);
        var schedule = new Schedule
        {
            Date = DateOnly.FromDateTime(pastDate),
            StartTime = TimeOnly.FromDateTime(pastDate)
        };
        _repo.Setup(x => x.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(schedule);

        var dto = new ScheduleUpdateDto();
        var result = await _scheduleService.UpdateAsync(dto, 0, 1, false);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(Error.Schedule.OldSchedule);

        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [MemberData(nameof(ScheduleTypeMismatchData))]
    public async Task UpdateAsync_ShouldFailWhenScheduleTypeDoesNotMatch(DateOnly? existingDate, DateOnly? requestedDate)
    {
        _validator.Setup(x => x.Validate(It.IsAny<ScheduleAddDto>()))
                            .Returns(new ValidationResult());

        var schedule = new Schedule
        {
            Date = existingDate
        };
        _repo.Setup(x => x.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(schedule);

        var dto = new ScheduleUpdateDto() { Date = requestedDate };
        var result = await _scheduleService.UpdateAsync(dto, 0, 1, false);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(Error.Schedule.ScheduleConversion);

        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task UpdateAsync_ShouldFailWhenAllowsMultipleDoesNotMatch(bool existingAllowsMultiple, bool requestedAllowsMultiple)
    {
        _validator.Setup(x => x.Validate(It.IsAny<ScheduleAddDto>()))
                            .Returns(new ValidationResult());

        var schedule = new Schedule
        {
            AllowsMultiple = existingAllowsMultiple
        };
        _repo.Setup(x => x.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(schedule);

        var dto = new ScheduleUpdateDto() { AllowsMultiple = requestedAllowsMultiple };
        var result = await _scheduleService.UpdateAsync(dto, 0, 1, false);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(Error.Schedule.MultipleSupportChange);

        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [MemberData(nameof(RecurringDateOrTimeChangedData))]
    [MemberData(nameof(NonRecurringDateOrTimeChangedData))]
    public async Task UpdateAsync_ShouldFailWhenDateOrTimeChangedAndHasPendingBookings(Schedule schedule, ScheduleUpdateDto dto)
    {
        _validator.Setup(x => x.Validate(It.IsAny<ScheduleAddDto>()))
                            .Returns(new ValidationResult());

        _repo.Setup(x => x.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(schedule);

        var result = await _scheduleService.UpdateAsync(dto, 0, 1, true);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(Error.Schedule.UpdateConflict);

        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [MemberData(nameof(RecurringDateOrTimeChangedData))]
    [MemberData(nameof(NonRecurringDateOrTimeChangedData))]
    public async Task UpdateAsync_ShouldFailWhenDateOrTimeChangedAndExistingSchedule(Schedule schedule, ScheduleUpdateDto dto)
    {
        _validator.Setup(x => x.Validate(It.IsAny<ScheduleAddDto>()))
                            .Returns(new ValidationResult());

        _repo.Setup(x => x.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(schedule);

        _repo.Setup(x => x.AnyAsync(It.IsAny<ScheduleSpecifications.CheckDuplicateSpecification>(),
                                    It.IsAny<CancellationToken>()))
                          .ReturnsAsync(true);

        var result = await _scheduleService.UpdateAsync(dto, 0, 1, false);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(Error.Schedule.Duplicate);

        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_ShouldPassWhenScheduleUpdatedSuccessfullyWithSameDateAndTime()
    {
        _validator.Setup(x => x.Validate(It.IsAny<ScheduleAddDto>()))
                            .Returns(new ValidationResult());

        var futureDate = DateTime.UtcNow.AddDays(2);
        var schedule = new Schedule
        {
            Date = DateOnly.FromDateTime(futureDate),
            DayOfWeek = DateOnly.FromDateTime(futureDate).DayOfWeek
        };
        _repo.Setup(x => x.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(schedule);

        var dto = new ScheduleUpdateDto() { Date = DateOnly.FromDateTime(futureDate) };
        var result = await _scheduleService.UpdateAsync(dto, 0, 1, false);

        result.IsSuccess.Should().BeTrue();
        result.Error.Should().BeNull();

        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [MemberData(nameof(RecurringDateOrTimeChangedData))]
    public async Task UpdateAsync_ShouldPassAndWhenRecurringScheduleUpdatedSuccessfullyWithChangedDateOrTime(Schedule schedule, ScheduleUpdateDto dto)
    {
        _validator.Setup(x => x.Validate(It.IsAny<ScheduleAddDto>()))
                            .Returns(new ValidationResult());

        var futureDate = DateTime.UtcNow.AddDays(2);
        _repo.Setup(x => x.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(schedule);

        _repo.Setup(x => x.AnyAsync(It.IsAny<ScheduleSpecifications.CheckDuplicateSpecification>(),
                                    It.IsAny<CancellationToken>()))
                          .ReturnsAsync(false);

        var result = await _scheduleService.UpdateAsync(dto, 0, 1, false);

        result.IsSuccess.Should().BeTrue();
        result.Error.Should().BeNull();

        _recurringJobManager.Verify(x => x.RemoveIfExists(It.IsAny<string>()), Times.Exactly(2));

        _recurringJobManager.Verify(x => x.AddOrUpdate(It.IsAny<string>(),
                                                       It.IsAny<Job>(),
                                                       It.IsAny<string>(),
                                                       It.IsAny<RecurringJobOptions>())
                                                       , Times.Exactly(2));

        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Theory]
    [MemberData(nameof(NonRecurringDateOrTimeChangedData))]
    public async Task UpdateAsync_ShouldPassAndWhenNonRecurringScheduleUpdatedSuccessfullyWithChangedDateOrTime(Schedule schedule, ScheduleUpdateDto dto)
    {
        _validator.Setup(x => x.Validate(It.IsAny<ScheduleAddDto>()))
                            .Returns(new ValidationResult());

        var futureDate = DateTime.UtcNow.AddDays(2);
        _repo.Setup(x => x.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(schedule);

        _repo.Setup(x => x.AnyAsync(It.IsAny<ScheduleSpecifications.CheckDuplicateSpecification>(),
                                    It.IsAny<CancellationToken>()))
                          .ReturnsAsync(false);

        var result = await _scheduleService.UpdateAsync(dto, 0, 1, false);

        dto.DayOfWeek.Should().Be(dto.Date!.Value.DayOfWeek);

        result.IsSuccess.Should().BeTrue();
        result.Error.Should().BeNull();

        _backgroundJobClient.Verify(x => x.ChangeState(It.IsAny<string>(),
                                    It.Is<IState>(state => state.Name == DeletedState.StateName),
                                    It.IsAny<string>()), Times.Exactly(2));

        _backgroundJobClient.Verify(x => x.Create(It.IsAny<Job>(),
                                                  It.IsAny<ScheduledState>())
                                                  , Times.Exactly(2));

        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }
}