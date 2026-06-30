using AutoMapper;
using Booking_SaaS.Domain.Entities;
using Booking_SaaS.Domain.Enums;
using Booking_SaaS.Services.Abstraction.DTOs.Resource;

namespace Booking_SaaS.Services.Mappers;

public class ResourceMapper : Profile
{
    public ResourceMapper()
    {
        CreateMap<ResourceAddDto, Resource>()
            .ForMember(r => r.Type, opt => opt.MapFrom(r => Enum.Parse<ResourceType>(r.Type, true)));

        CreateMap<Resource, ResourceResultDto>();
    }
}