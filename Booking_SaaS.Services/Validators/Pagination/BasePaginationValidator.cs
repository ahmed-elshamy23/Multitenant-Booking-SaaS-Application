using Booking_SaaS.Services.Abstraction.DTOs;
using FluentValidation;

namespace Booking_SaaS.Services.Validators.Pagination;

public abstract class BasePaginationValidator<T> : AbstractValidator<PaginatedDto<T>> where T : BaseDto
{
    protected BasePaginationValidator()
    {
        RuleFor(x => x.PageIndex)
            .GreaterThanOrEqualTo(1);

        RuleFor(x => x.PageSize)
            .GreaterThanOrEqualTo(5)
            .LessThan(50);
    }
}