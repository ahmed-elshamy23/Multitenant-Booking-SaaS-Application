using AutoMapper;
using Booking_SaaS.Domain.Entities;
using Booking_SaaS.Services.Abstraction.DTOs.Bookings;

namespace Booking_SaaS.Services.Mappers;

public class BookingMapper : Profile
{
    public BookingMapper()
    {
        CreateMap<Booking, BookingDto>()
            .ForMember(dest => dest.StartTime, opt => opt.MapFrom(src => src.StartTime ?? src.Schedule.StartTime))
            .ForMember(dest => dest.EndTime, opt => opt.MapFrom(src => src.EndTime ?? src.Schedule.EndTime));

        CreateMap<BookingAddDto, Booking>();
    }
}