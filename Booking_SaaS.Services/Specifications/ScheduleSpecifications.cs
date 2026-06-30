using System.Linq.Expressions;
using Booking_SaaS.Domain.Contracts;
using Booking_SaaS.Domain.Entities;
using Booking_SaaS.Services.Abstraction.DTOs.Schedule;
using Booking_SaaS.Services.Extensions;

namespace Booking_SaaS.Services.Specifications;

internal static class ScheduleSpecifications
{
    internal class FilterSpecification : SpecificationsBase<Schedule, int>
    {
        internal FilterSpecification(ScheduleFilterDto filterDto, int resourceId)
        {
            Expression<Func<Schedule, bool>> criteria = s => s.ResourceId == resourceId;

            var startDate = filterDto.StartDate;
            var endDate = filterDto.EndDate;
            var startTime = filterDto.StartTime;
            var endTime = filterDto.EndTime;

            if (startDate != null)
                criteria = Add(criteria,
                    s => s.Date == null || s.Date >= startDate);

            if (endDate != null)
                criteria = Add(criteria,
                    s => s.Date == null || s.Date <= endDate);

            if (startTime != null)
                criteria = Add(criteria,
                    s => s.StartTime >= startTime);

            if (endTime != null)
                criteria = Add(criteria,
                    s => s.EndTime <= endTime);

            Criteria = criteria;
        }

        private static Expression<Func<Schedule, bool>> Add(
            Expression<Func<Schedule, bool>>? current,
            Expression<Func<Schedule, bool>> next)
        {
            return current == null ? next : current.And(next);
        }
    }

    internal class PaginatedFilterSpecification : FilterSpecification
    {
        internal PaginatedFilterSpecification(ScheduleFilterDto filterDto, int resourceId) : base(filterDto, resourceId)
        {
            ApplyPagination(filterDto.PageIndex, filterDto.PageSize);
            AddOrderBy(s => s.Date == null);
            AddOrderBy(s => s.DayOfWeek);
            AddOrderBy(s => s.StartTime);
            AddOrderBy(s => s.Id);
        }
    }

    internal class CheckDuplicateSpecification : SpecificationsBase<Schedule, int>
    {
        internal CheckDuplicateSpecification(int id, int resourceId, DayOfWeek dayOfWeek,
            TimeOnly startTime, TimeOnly endTime) : base(s =>
            s.ResourceId == resourceId &&
            s.Id != id &&
            s.StartTime < endTime && s.EndTime > startTime &&
            s.DayOfWeek == dayOfWeek)
        {
        }

        internal CheckDuplicateSpecification(int id, int resourceId, DateOnly date,
            TimeOnly startTime, TimeOnly endTime) : base(s =>
            s.ResourceId == resourceId &&
            s.Id != id &&
            s.StartTime < endTime && s.EndTime > startTime &&
            s.DayOfWeek == date.DayOfWeek &&
            (s.Date == null || s.Date == date))
        {
        }
    }
}