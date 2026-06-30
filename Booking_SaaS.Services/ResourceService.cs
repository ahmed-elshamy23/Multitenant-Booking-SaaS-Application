using AutoMapper;
using Booking_SaaS.Domain.Contracts;
using Booking_SaaS.Domain.Contracts.Repositories;
using Booking_SaaS.Domain.Entities;
using Booking_SaaS.Domain.Enums;
using Booking_SaaS.Domain.Results;
using Booking_SaaS.Services.Abstraction;
using Booking_SaaS.Services.Abstraction.DTOs;
using Booking_SaaS.Services.Abstraction.DTOs.Resource;
using Booking_SaaS.Services.Specifications;
using FluentValidation;

namespace Booking_SaaS.Services;

internal class ResourceService : IResourceService
{
    private readonly IValidator<ResourceFilterDto> _filterValidator;
    private readonly IMapper _mapper;
    private readonly IValidator<PaginatedDto<ResourceResultDto>> _paginationValidator;
    private readonly IResourceRepository _repo;
    private readonly IValidator<ResourceAddDto> _resourceValidator;
    private readonly IUnitOfWork _unitOfWork;

    public ResourceService(IValidator<ResourceAddDto> resourceValidator, IUnitOfWork unitOfWork, IMapper mapper,
        IValidator<PaginatedDto<ResourceResultDto>> paginationValidator, IValidator<ResourceFilterDto> filterValidator)
    {
        _resourceValidator = resourceValidator;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _paginationValidator = paginationValidator;
        _filterValidator = filterValidator;
        _repo = unitOfWork.ResourceRepository;
    }

    public async Task<Result<PaginatedDto<ResourceResultDto>>> GetAllAsync(ResourceFilterDto filterDto,
        CancellationToken cancellationToken = default)
    {
        var paginatedDto = new PaginatedDto<ResourceResultDto>
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

        ResourceType? resourceType = null;
        if (Enum.TryParse(filterDto.Type, true, out ResourceType type))
            resourceType = type;

        var filterSpecification = new ResourceSpecifications.FilterSpecification(filterDto.Name, resourceType);
        paginatedDto.TotalCount = await _repo.GetCountAsync(filterSpecification, cancellationToken);

        var paginationSpecification =
            new ResourceSpecifications.PaginatedFilterSpecification(filterDto.Name, resourceType, filterDto.PageIndex,
                filterDto.PageSize);
        paginatedDto.Items =
            _mapper.Map<List<Resource>, List<ResourceResultDto>>(await _repo.GetAllAsync(paginationSpecification,
                cancellationToken));

        return paginatedDto;
    }

    public async Task<Result<ResourceResultDto>> GetByIdAsync(int resourceId,
        CancellationToken cancellationToken = default)
    {
        var resource = await _repo.GetByIdAsync(resourceId, cancellationToken);
        if (resource == null)
            return Error.Validation.NotFound(nameof(Resource));

        return _mapper.Map<ResourceResultDto>(resource);
    }

    public async Task<Result<int>> AddAsync(ResourceAddDto resourceDto)
    {
        var validationResult = _resourceValidator.Validate(resourceDto);
        if (!validationResult.IsValid)
            return Error.Validation.InvalidParameters(validationResult.Errors
                .Select(x => x.ErrorMessage));

        if (await CheckDuplicateResourceAsync(resourceDto, 0))
            return Error.Resource.Duplicate;

        var resource = _mapper.Map<Resource>(resourceDto);
        _repo.Add(resource);
        await _unitOfWork.SaveChangesAsync();

        return resource.Id;
    }

    public async Task<Result> UpdateAsync(ResourceUpdateDto resourceDto, int resourceId)
    {
        var validationResult = _resourceValidator.Validate(resourceDto);
        if (!validationResult.IsValid)
            return Error.Validation.InvalidParameters(validationResult.Errors
                .Select(x => x.ErrorMessage));

        if (await CheckDuplicateResourceAsync(resourceDto, resourceId))
            return Error.Resource.Duplicate;

        var existingResource = await _repo.GetByIdAsync(resourceId);
        if (existingResource == null)
            return Error.Validation.NotFound(nameof(Resource));

        _mapper.Map(resourceDto, existingResource);
        _repo.Update(existingResource);
        await _unitOfWork.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result> DeleteAsync(int resourceId)
    {
        var existingResource = await _repo.GetByIdAsync(resourceId);
        if (existingResource == null)
            return Error.Validation.NotFound(nameof(Resource));

        _repo.Delete(existingResource);
        await _unitOfWork.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<bool> HasSchedulesAsync(int resourceId)
    {
        var specification =
            new ResourceSpecifications.HasSchedulesSpecification(resourceId);
        return await _repo.AnyAsync(specification);
    }

    public async Task<bool> IsExistingResourceAsync(int resourceId)
    {
        var specification =
            new ResourceSpecifications.IsExistingSpecification(resourceId);
        return await _repo.AnyAsync(specification);
    }

    public async Task<bool> AcquireLockAsync(int resourceId, CancellationToken cancellationToken = default)
    {
        return await _repo.AcquireLockAsync(resourceId, cancellationToken);
    }

    private async Task<bool> CheckDuplicateResourceAsync(ResourceAddDto resourceDto, int resourceId)
    {
        var specification =
            new ResourceSpecifications.CheckDuplicateSpecification(resourceDto.Name,
                Enum.Parse<ResourceType>(resourceDto.Type, true), resourceId);
        return await _repo.AnyAsync(specification);
    }
}