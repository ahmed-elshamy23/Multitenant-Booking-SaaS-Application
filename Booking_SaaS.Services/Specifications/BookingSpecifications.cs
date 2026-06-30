using System.Linq.Expressions;
using Booking_SaaS.Domain.Contracts;
using Booking_SaaS.Domain.Entities;
using Booking_SaaS.Domain.Enums;
using Booking_SaaS.Services.Abstraction.DTOs.Bookings;
using Booking_SaaS.Services.Extensions;

namespace Booking_SaaS.Services.Specifications;

internal static class BookingSpecifications
{
    internal class PendingStatusAndDateSpecification : SpecificationsBase<Booking, int>
    {
        internal PendingStatusAndDateSpecification(int scheduleId, DateOnly date) : base(b =>
            b.ScheduleId == scheduleId && b.Status == BookingStatus.Pending && b.Date == date)
        {
        }
    }

    internal class HasPendingBookingsSpecification : SpecificationsBase<Booking, int>
    {
        internal HasPendingBookingsSpecification(int scheduleId) : base(b =>
            b.ScheduleId == scheduleId && b.Status == BookingStatus.Pending)
        {
        }
    }

    internal class UserFilterSpecification : SpecificationsBase<Booking, int>
    {
        internal UserFilterSpecification(BookingFilterDto filterDto, int userId)
        {
            if (filterDto.Id.HasValue)
            {
                Criteria = b => b.Id == filterDto.Id.Value && b.UserId == userId;
                return;
            }

            Expression<Func<Booking, bool>> criteria = b => b.UserId == userId;

            if (filterDto.StartDate != null)
                criteria = Add(criteria,
                    b => b.Date >= filterDto.StartDate);

            if (filterDto.EndDate != null)
                criteria = Add(criteria,
                    b => b.Date <= filterDto.EndDate);

            if (filterDto.StartTime != null)
                criteria = Add(criteria,
                    b => b.StartTime.HasValue
                        ? b.StartTime.Value >= filterDto.StartTime
                        : b.Schedule.StartTime >= filterDto.StartTime);

            if (filterDto.EndTime != null)
                criteria = Add(criteria,
                    b => b.EndTime.HasValue
                        ? b.EndTime.Value <= filterDto.EndTime
                        : b.Schedule.EndTime <= filterDto.EndTime);

            Criteria = criteria;
        }

        private static Expression<Func<Booking, bool>> Add(
            Expression<Func<Booking, bool>>? current,
            Expression<Func<Booking, bool>> next)
        {
            return current == null ? next : current.And(next);
        }
    }

    internal class UserPaginatedFilterSpecification : UserFilterSpecification
    {
        internal UserPaginatedFilterSpecification(BookingFilterDto filterDto, int userId, int pageIndex, int pageSize)
            : base(filterDto, userId)
        {
            ApplyPagination(pageIndex, pageSize);
            AddInclude(b => b.Schedule);

            AddOrderBy(b => b.Status);
            AddOrderBy(s => s.Date);
            AddOrderBy(b => b.StartTime!);
            AddOrderBy(s => s.Id);
        }
    }

    internal class CheckDuplicateSpecification : SpecificationsBase<Booking, int>
    {
        internal CheckDuplicateSpecification(int userId, int scheduleId, DateOnly date)
            : base(b => b.UserId == userId && b.ScheduleId == scheduleId && b.Date == date)
        {
        }
    }
}