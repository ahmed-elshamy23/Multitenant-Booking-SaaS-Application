using AutoMapper;
using Booking_SaaS.Domain.Contracts;
using Booking_SaaS.Domain.Contracts.Repositories;
using Booking_SaaS.Domain.Entities;
using Booking_SaaS.Domain.Enums;
using Booking_SaaS.Domain.Results;
using Booking_SaaS.Services;
using Booking_SaaS.Services.Abstraction.DTOs;
using Booking_SaaS.Services.Abstraction.DTOs.Resource;
using Booking_SaaS.Services.Specifications;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Moq;

namespace Booking_SaaS.Tests.Services;

public class ResourceServiceTests
{
    private readonly Mock<IValidator<ResourceFilterDto>> _filterValidator = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly Mock<IValidator<PaginatedDto<ResourceResultDto>>> _paginationValidator = new();
    private readonly Mock<IResourceRepository> _repo = new();
    private readonly Mock<IValidator<ResourceAddDto>> _resourceValidator = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly ResourceService _resourceService;

    public ResourceServiceTests()
    {
        _unitOfWork.Setup(x => x.ResourceRepository).Returns(_repo.Object);

        _resourceService = new ResourceService(_resourceValidator.Object,
                                               _unitOfWork.Object,
                                               _mapper.Object,
                                               _paginationValidator.Object,
                                               _filterValidator.Object);
    }

    [Fact]
    public async Task GetAllAsync_ShouldFailWhenPaginationValidationErrorsExist()
    {
        var fakeFailures = new List<ValidationFailure> { new("Property", "Error") };
        _paginationValidator.Setup(x => x.Validate(It.IsAny<PaginatedDto<ResourceResultDto>>()))
                            .Returns(new ValidationResult(fakeFailures));

        var filterDto = new ResourceFilterDto();
        var result = await _resourceService.GetAllAsync(filterDto);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull().And.BeOfType<Error>();
        result.Error.ErrorType.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public async Task GetAllAsync_ShouldFailWhenFilterValidationErrorsExist()
    {
        _paginationValidator.Setup(x => x.Validate(It.IsAny<PaginatedDto<ResourceResultDto>>()))
                            .Returns(new ValidationResult());

        var fakeFailures = new List<ValidationFailure> { new("Property", "Error") };
        _filterValidator.Setup(x => x.Validate(It.IsAny<ResourceFilterDto>()))
                        .Returns(new ValidationResult(fakeFailures));

        var filterDto = new ResourceFilterDto();
        var result = await _resourceService.GetAllAsync(filterDto);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull().And.BeOfType<Error>();
        result.Error.ErrorType.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public async Task GetAllAsync_ShouldPassAndReturnPaginatedData()
    {
        _paginationValidator.Setup(x => x.Validate(It.IsAny<PaginatedDto<ResourceResultDto>>()))
                            .Returns(new ValidationResult());

        _filterValidator.Setup(x => x.Validate(It.IsAny<ResourceFilterDto>()))
                        .Returns(new ValidationResult());

        var filterDto = new ResourceFilterDto() { Type = "0" };
        var totalCount = 5;

        _repo.Setup(x => x.GetCountAsync(It.IsAny<ResourceSpecifications.FilterSpecification>(),
                                         It.IsAny<CancellationToken>()))
                                        .ReturnsAsync(totalCount);

        var mockResources = new List<Resource> { new(), new() };
        _repo.Setup(x => x.GetAllAsync(It.IsAny<ResourceSpecifications.PaginatedFilterSpecification>(),
                                       It.IsAny<CancellationToken>()))
                                      .ReturnsAsync(mockResources);

        var mockDtos = new List<ResourceResultDto> { new(), new() };
        _mapper.Setup(x => x.Map<List<Resource>, List<ResourceResultDto>>(It.IsAny<List<Resource>>()))
               .Returns(mockDtos);

        var result = await _resourceService.GetAllAsync(filterDto);

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
             .ReturnsAsync((Resource?)null);

        var result = await _resourceService.GetByIdAsync(1);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull().And.BeOfType<Error>();
        result.Error.ErrorType.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldPassWhenResourceExists()
    {
        var dto = new ResourceResultDto();
        _repo.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
             .ReturnsAsync(new Resource());

        _mapper.Setup(x => x.Map<ResourceResultDto>(It.IsAny<Resource>()))
               .Returns(dto);

        var result = await _resourceService.GetByIdAsync(1);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(dto);
        result.Error.Should().BeNull();
    }

    [Fact]
    public async Task AddAsync_ShouldFailWhenValidationErrorsExist()
    {
        var fakeFailures = new List<ValidationFailure> { new("Property", "Error") };
        _resourceValidator.Setup(x => x.Validate(It.IsAny<ResourceAddDto>()))
                          .Returns(new ValidationResult(fakeFailures));

        var dto = new ResourceAddDto { Type = "0" };
        var result = await _resourceService.AddAsync(dto);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull().And.BeOfType<Error>();
        result.Error.ErrorType.Should().Be(ErrorType.Validation);

        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddAsync_ShouldFailWhenResourceAlreadyExists()
    {
        _resourceValidator.Setup(x => x.Validate(It.IsAny<ResourceAddDto>()))
                          .Returns(new ValidationResult());

        _repo.Setup(x => x.AnyAsync(It.IsAny<ResourceSpecifications.CheckDuplicateSpecification>(),
                                    It.IsAny<CancellationToken>()))
                                   .ReturnsAsync(true);

        var dto = new ResourceAddDto { Type = "0" };
        var result = await _resourceService.AddAsync(dto);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(Error.Resource.Duplicate);

        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddAsync_ShouldPassWhenSuccessfullyAdded()
    {
        _resourceValidator.Setup(x => x.Validate(It.IsAny<ResourceAddDto>()))
                          .Returns(new ValidationResult());

        _repo.Setup(x => x.AnyAsync(It.IsAny<ResourceSpecifications.CheckDuplicateSpecification>(),
                                    It.IsAny<CancellationToken>()))
                                   .ReturnsAsync(false);

        var id = 1;
        _mapper.Setup(x => x.Map<Resource>(It.IsAny<ResourceAddDto>()))
               .Returns(new Resource { Id = id });

        var dto = new ResourceAddDto { Type = "0" };
        var result = await _resourceService.AddAsync(dto);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(id);
        result.Error.Should().BeNull();

        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ShouldFailWhenValidationErrorsExist()
    {
        var fakeFailures = new List<ValidationFailure> { new("Property", "Error") };
        _resourceValidator.Setup(x => x.Validate(It.IsAny<ResourceUpdateDto>()))
                                .Returns(new ValidationResult(fakeFailures));

        var dto = new ResourceUpdateDto { Type = "0" };
        var result = await _resourceService.UpdateAsync(dto, 1);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull().And.BeOfType<Error>();
        result.Error.ErrorType.Should().Be(ErrorType.Validation);

        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_ShouldFailWhenDuplicateExists()
    {
        _resourceValidator.Setup(x => x.Validate(It.IsAny<ResourceUpdateDto>()))
                                .Returns(new ValidationResult());

        _repo.Setup(x => x.AnyAsync(It.IsAny<ResourceSpecifications.CheckDuplicateSpecification>(),
                                    It.IsAny<CancellationToken>()))
                                   .ReturnsAsync(true);

        var dto = new ResourceUpdateDto { Type = "0" };
        var result = await _resourceService.UpdateAsync(dto, 1);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(Error.Resource.Duplicate);

        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_ShouldFailWhenResourceNotFound()
    {
        _resourceValidator.Setup(x => x.Validate(It.IsAny<ResourceUpdateDto>()))
                                .Returns(new ValidationResult());

        _repo.Setup(x => x.AnyAsync(It.IsAny<ResourceSpecifications.CheckDuplicateSpecification>(),
                                    It.IsAny<CancellationToken>()))
                                   .ReturnsAsync(false);

        _repo.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
             .ReturnsAsync((Resource?)null);

        var dto = new ResourceUpdateDto { Type = "0" };
        var result = await _resourceService.UpdateAsync(dto, 1);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull().And.BeOfType<Error>();
        result.Error.ErrorType.Should().Be(ErrorType.NotFound);

        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_ShouldPassWhenSuccessfullyUpdated()
    {
        _resourceValidator.Setup(x => x.Validate(It.IsAny<ResourceUpdateDto>()))
                                .Returns(new ValidationResult());

        _repo.Setup(x => x.AnyAsync(It.IsAny<ResourceSpecifications.CheckDuplicateSpecification>(),
                                    It.IsAny<CancellationToken>()))
                                   .ReturnsAsync(false);

        _repo.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
             .ReturnsAsync(new Resource());

        var dto = new ResourceUpdateDto { Type = "0" };
        var result = await _resourceService.UpdateAsync(dto, 1);

        result.IsSuccess.Should().BeTrue();
        result.Error.Should().BeNull();

        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ShouldFailWhenResourceIsNotFound()
    {
        _repo.Setup(x => x.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync((Resource?)null);

        var result = await _resourceService.DeleteAsync(1);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull().And.BeOfType<Error>();
        result.Error.ErrorType.Should().Be(ErrorType.NotFound);

        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_ShouldPassWhenSuccessfullyDeleted()
    {
        _repo.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
             .ReturnsAsync(new Resource());

        var result = await _resourceService.DeleteAsync(1);

        result.IsSuccess.Should().BeTrue();
        result.Error.Should().BeNull();

        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}