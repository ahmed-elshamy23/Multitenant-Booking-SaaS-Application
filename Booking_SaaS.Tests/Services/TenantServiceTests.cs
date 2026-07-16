using AutoMapper;
using Booking_SaaS.Domain.Contracts;
using Booking_SaaS.Domain.Contracts.Repositories;
using Booking_SaaS.Domain.Entities;
using Booking_SaaS.Domain.Enums;
using Booking_SaaS.Domain.Results;
using Booking_SaaS.Services;
using Booking_SaaS.Services.Abstraction.DTOs;
using Booking_SaaS.Services.Abstraction.DTOs.Tenant;
using Booking_SaaS.Services.Specifications;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Moq;

namespace Booking_SaaS.Tests.Services;

public class TenantServiceTests
{
    private readonly Mock<IMapper> _mapper = new();
    private readonly Mock<IValidator<PaginatedDto<TenantResultDto>>> _paginationValidator = new();
    private readonly Mock<IGenericRepository<Tenant, int>> _repo = new();
    private readonly Mock<IValidator<TenantAddDto>> _tenantValidator = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly TenantService _tenantService;

    public TenantServiceTests()
    {
        _unitOfWork.Setup(x => x.GetRepository<Tenant, int>()).Returns(_repo.Object);

        _tenantService = new TenantService(_unitOfWork.Object,
                                           _tenantValidator.Object,
                                           _paginationValidator.Object,
                                           _mapper.Object);
    }

    [Fact]
    public async Task AddAsync_ShouldFailWhenValidationErrorsExist()
    {
        var fakeFailures = new List<ValidationFailure> { new("Property", "Error") };
        _tenantValidator.Setup(x => x.Validate(It.IsAny<TenantAddDto>()))
            .Returns(new ValidationResult(fakeFailures));

        var dto = new TenantAddDto();
        var result = await _tenantService.AddAsync(dto);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull().And.BeOfType<Error>();
        result.Error.ErrorType.Should().Be(ErrorType.Validation);

        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddAsync_ShouldFailWhenTenantAlreadyExists()
    {
        _tenantValidator.Setup(x => x.Validate(It.IsAny<TenantAddDto>()))
            .Returns(new ValidationResult());

        _repo.Setup(x => x.AnyAsync(It.IsAny<TenantSpecifications.CheckDuplicateSpecification>(),
                                    It.IsAny<CancellationToken>()))
                                    .ReturnsAsync(true);

        var dto = new TenantAddDto();
        var result = await _tenantService.AddAsync(dto);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(Error.Tenant.DuplicateName);

        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddAsync_ShouldPassWhenSuccessfullyAdded()
    {
        _tenantValidator.Setup(x => x.Validate(It.IsAny<TenantAddDto>()))
            .Returns(new ValidationResult());

        _repo.Setup(x => x.AnyAsync(It.IsAny<TenantSpecifications.CheckDuplicateSpecification>(),
                                    It.IsAny<CancellationToken>()))
                                    .ReturnsAsync(false);

        _mapper.Setup(x => x.Map<Tenant>(It.IsAny<TenantAddDto>()))
            .Returns(new Tenant());

        var dto = new TenantAddDto();
        var result = await _tenantService.AddAsync(dto);

        result.IsSuccess.Should().BeTrue();
        result.Error.Should().BeNull();

        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetTenantNameByIdAsync_ShouldFailWhenNoTenantExists()
    {
        _repo.Setup(x => x.GetByIdAsync(It.IsAny<int>(),
                                        It.IsAny<CancellationToken>()))
                                        .ReturnsAsync((Tenant?)null);

        var result = await _tenantService.GetTenantNameByIdAsync(1);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull().And.BeOfType<Error>();
        result.Error.ErrorType.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task GetTenantNameByIdAsync_ShouldPassWhenTenantExists()
    {
        var name = "Test";

        _repo.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
             .ReturnsAsync(new Tenant { Name = name });

        var result = await _tenantService.GetTenantNameByIdAsync(1);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(name);
        result.Error.Should().BeNull();
    }

    [Fact]
    public async Task IsActiveAsync_ShouldFailWhenNoTenantExists()
    {
        _repo.Setup(x => x.GetByIdAsync(It.IsAny<int>(),
                                        It.IsAny<CancellationToken>()))
                                        .ReturnsAsync((Tenant?)null);

        var result = await _tenantService.IsActiveAsync(1);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull().And.BeOfType<Error>();
        result.Error.ErrorType.Should().Be(ErrorType.NotFound);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task IsActiveAsync_ShouldPassAndReturnGivenValueWhenTenantExists(bool isActive)
    {
        _repo.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
             .ReturnsAsync(new Tenant { IsActive = isActive });

        var result = await _tenantService.IsActiveAsync(1);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(isActive);
        result.Error.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_ShouldFailWhenValidationErrorsExist()
    {
        var fakeFailures = new List<ValidationFailure> { new("Property", "Error") };
        _tenantValidator.Setup(x => x.Validate(It.IsAny<TenantUpdateDto>()))
            .Returns(new ValidationResult(fakeFailures));

        var dto = new TenantUpdateDto();
        var result = await _tenantService.UpdateAsync(dto, 1);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull().And.BeOfType<Error>();
        result.Error.ErrorType.Should().Be(ErrorType.Validation);

        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_ShouldFailWhenInsertingDuplicateName()
    {
        _tenantValidator.Setup(x => x.Validate(It.IsAny<TenantUpdateDto>()))
            .Returns(new ValidationResult());

        _repo.Setup(x => x.AnyAsync(It.IsAny<TenantSpecifications.CheckDuplicateSpecification>(),
                                    It.IsAny<CancellationToken>()))
                                    .ReturnsAsync(true);

        var dto = new TenantUpdateDto();
        var result = await _tenantService.UpdateAsync(dto, 1);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(Error.Tenant.DuplicateName);

        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_ShouldFailWhenTenantIsNotFound()
    {
        _tenantValidator.Setup(x => x.Validate(It.IsAny<TenantUpdateDto>()))
            .Returns(new ValidationResult());

        _repo.Setup(x => x.AnyAsync(It.IsAny<TenantSpecifications.CheckDuplicateSpecification>(),
                                    It.IsAny<CancellationToken>()))
                                    .ReturnsAsync(false);

        _repo.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
             .ReturnsAsync((Tenant?)null);

        var dto = new TenantUpdateDto();
        var result = await _tenantService.UpdateAsync(dto, 1);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull().And.BeOfType<Error>();
        result.Error.ErrorType.Should().Be(ErrorType.NotFound);

        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_ShouldPassWhenSuccessfullyUpdated()
    {
        _tenantValidator.Setup(x => x.Validate(It.IsAny<TenantUpdateDto>()))
            .Returns(new ValidationResult());

        _repo.Setup(x => x.AnyAsync(It.IsAny<TenantSpecifications.CheckDuplicateSpecification>(),
                                    It.IsAny<CancellationToken>()))
                                    .ReturnsAsync(false);

        _mapper.Setup(x => x.Map(It.IsAny<TenantUpdateDto>(), It.IsAny<Tenant>()))
            .Returns(new Tenant());

        _repo.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
             .ReturnsAsync(new Tenant());

        var dto = new TenantUpdateDto();
        var result = await _tenantService.UpdateAsync(dto, 1);

        result.IsSuccess.Should().BeTrue();
        result.Error.Should().BeNull();

        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_ShouldFailWhenValidationErrorsExist()
    {
        var fakeFailures = new List<ValidationFailure> { new("Property", "Error") };

        _paginationValidator.Setup(x => x.Validate(It.IsAny<PaginatedDto<TenantResultDto>>()))
            .Returns(new ValidationResult(fakeFailures));

        var result = await _tenantService.GetAllAsync("Test", -1, 0);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error.ErrorType.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public async Task GetAllAsync_ShouldPassAndReturnPaginatedData()
    {
        var pageIndex = 1;
        var pageSize = 10;
        var totalCount = 50;

        _paginationValidator.Setup(x => x.Validate(It.IsAny<PaginatedDto<TenantResultDto>>()))
            .Returns(new ValidationResult());

        _repo.Setup(x => x.GetCountAsync(It.IsAny<TenantSpecifications.FilterSpecification>(),
                                         It.IsAny<CancellationToken>()))
                                        .ReturnsAsync(totalCount);

        var mockTenants = new List<Tenant> { new(), new() };
        _repo.Setup(x => x.GetAllAsync(It.IsAny<TenantSpecifications.PaginatedFilterSpecification>(),
                                       It.IsAny<CancellationToken>()))
                                      .ReturnsAsync(mockTenants);

        var mockDtos = new List<TenantResultDto> { new(), new() };
        _mapper.Setup(x => x.Map<List<Tenant>, List<TenantResultDto>>(It.IsAny<List<Tenant>>()))
               .Returns(mockDtos);

        var result = await _tenantService.GetAllAsync("Test", pageIndex, pageSize);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.TotalCount.Should().Be(totalCount);
        result.Value.Items.Should().BeEquivalentTo(mockDtos);
        result.Value.PageIndex.Should().Be(pageIndex);
        result.Value.PageSize.Should().Be(pageSize);
        result.Error.Should().BeNull();
    }
}