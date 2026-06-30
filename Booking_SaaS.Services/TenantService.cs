using AutoMapper;
using Booking_SaaS.Domain.Contracts;
using Booking_SaaS.Domain.Contracts.Repositories;
using Booking_SaaS.Domain.Entities;
using Booking_SaaS.Domain.Results;
using Booking_SaaS.Services.Abstraction;
using Booking_SaaS.Services.Abstraction.DTOs;
using Booking_SaaS.Services.Abstraction.DTOs.Tenant;
using Booking_SaaS.Services.Specifications;
using FluentValidation;

namespace Booking_SaaS.Services;

public class TenantService : ITenantService
{
    private readonly IMapper _mapper;
    private readonly IValidator<PaginatedDto<TenantResultDto>> _paginationValidator;
    private readonly IGenericRepository<Tenant, int> _repo;
    private readonly IValidator<TenantAddDto> _tenantValidator;
    private readonly IUnitOfWork _unitOfWork;

    public TenantService(IUnitOfWork unitOfWork, IValidator<TenantAddDto> tenantValidator,
        IValidator<PaginatedDto<TenantResultDto>> paginationValidator, IMapper mapper)
    {
        _repo = unitOfWork.GetRepository<Tenant, int>();
        _unitOfWork = unitOfWork;
        _tenantValidator = tenantValidator;
        _paginationValidator = paginationValidator;
        _mapper = mapper;
    }

    public async Task<Result<PaginatedDto<TenantResultDto>>> GetAllAsync(string? name, int pageIndex, int pageSize,
        CancellationToken cancellationToken = default)
    {
        var paginatedDto = new PaginatedDto<TenantResultDto>
        {
            PageIndex = pageIndex,
            PageSize = pageSize
        };
        var validationResult = _paginationValidator.Validate(paginatedDto);
        if (!validationResult.IsValid)
            return Error.Validation.InvalidParameters(validationResult.Errors
                .Select(x => x.ErrorMessage));

        var filterSpecification = new TenantSpecifications.FilterSpecification(name);
        paginatedDto.TotalCount = await _repo.GetCountAsync(filterSpecification, cancellationToken);

        var paginationSpecification = new TenantSpecifications.PaginatedFilterSpecification(name, pageIndex, pageSize);
        paginatedDto.Items =
            _mapper.Map<List<Tenant>, List<TenantResultDto>>(await _repo.GetAllAsync(paginationSpecification,
                cancellationToken));

        return paginatedDto;
    }

    public async Task<Result<string>> GetTenantNameByIdAsync(int tenantId)
    {
        var tenant = await _repo.GetByIdAsync(tenantId);
        if (tenant == null)
            return Error.Validation.NotFound(nameof(Tenant));

        return tenant.Name;
    }

    public async Task<Result> AddAsync(TenantAddDto tenantDto)
    {
        var validationResult = _tenantValidator.Validate(tenantDto);
        if (!validationResult.IsValid)
            return Error.Validation.InvalidParameters(validationResult.Errors
                .Select(x => x.ErrorMessage));

        if (await CheckExistingTenantAsync(tenantDto, 0))
            return Error.Tenant.DuplicateName;

        var tenant = _mapper.Map<Tenant>(tenantDto);
        _repo.Add(tenant);
        await _unitOfWork.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result> UpdateAsync(TenantUpdateDto tenantDto, int tenantId)
    {
        var validationResult = _tenantValidator.Validate(tenantDto);
        if (!validationResult.IsValid)
            return Error.Validation.InvalidParameters(validationResult.Errors
                .Select(x => x.ErrorMessage));

        if (await CheckExistingTenantAsync(tenantDto, tenantId))
            return Error.Tenant.DuplicateName;

        var existingTenant = await _repo.GetByIdAsync(tenantId);
        if (existingTenant == null)
            return Error.Validation.NotFound(nameof(Tenant));

        _mapper.Map(tenantDto, existingTenant);
        _repo.Update(existingTenant);
        await _unitOfWork.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result<bool>> IsActiveAsync(int tenantId, CancellationToken cancellationToken = default)
    {
        var tenant = await _repo.GetByIdAsync(tenantId, cancellationToken);
        if (tenant == null)
            return Error.Validation.NotFound(nameof(Tenant));

        return tenant.IsActive;
    }

    private async Task<bool> CheckExistingTenantAsync(TenantAddDto tenantDto, int tenantId)
    {
        var specification = new TenantSpecifications.CheckDuplicateSpecification(tenantDto.Name, tenantId);
        return await _repo.AnyAsync(specification);
    }
}