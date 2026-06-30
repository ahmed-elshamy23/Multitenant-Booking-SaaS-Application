using AutoMapper;
using Booking_SaaS.Domain.Entities;
using Booking_SaaS.Services.Abstraction.DTOs.Tenant;

namespace Booking_SaaS.Services.Mappers;

public class TenantMapper : Profile
{
    public TenantMapper()
    {
        CreateMap<TenantAddDto, Tenant>();
        CreateMap<Tenant, TenantResultDto>();
    }
}