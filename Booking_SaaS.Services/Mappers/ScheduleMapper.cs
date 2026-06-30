using AutoMapper;
using Booking_SaaS.Domain.Entities;
using Booking_SaaS.Services.Abstraction.DTOs.Schedule;

namespace Booking_SaaS.Services.Mappers;

public class ScheduleMapper : Profile
{
    public ScheduleMapper()
    {
        CreateMap<ScheduleAddDto, Schedule>();
        CreateMap<Schedule, ScheduleResultDto>();
    }
}