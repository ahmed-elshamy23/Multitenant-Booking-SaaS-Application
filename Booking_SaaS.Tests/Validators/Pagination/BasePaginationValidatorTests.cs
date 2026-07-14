using Booking_SaaS.Services.Abstraction.DTOs;
using Booking_SaaS.Services.Validators.Pagination;
using FluentValidation.TestHelper;

namespace Booking_SaaS.Tests.Validators.Pagination;

public class DummyPaginationValidator : BasePaginationValidator<BaseDto> { }

public class BasePaginationValidatorTests
{
    private readonly DummyPaginationValidator _paginationValidator = new();

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void ShouldPassWhenPageIndexIsValid(int pageIndex)
    {
        var dto = new PaginatedDto<BaseDto> { PageIndex = pageIndex };
        var result = _paginationValidator.TestValidate(dto);
        result.ShouldNotHaveValidationErrorFor(d => d.PageIndex);
    }

    [Fact]
    public void ShouldFailWhenPageIndexIsInvalid()
    {
        var dto = new PaginatedDto<BaseDto> { PageIndex = 0 };
        var result = _paginationValidator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(d => d.PageIndex);
    }

    [Theory]
    [InlineData(5)]
    [InlineData(6)]
    public void ShouldPassWhenPageSizeIsValid(int pageSize)
    {
        var dto = new PaginatedDto<BaseDto> { PageSize = pageSize };
        var result = _paginationValidator.TestValidate(dto);
        result.ShouldNotHaveValidationErrorFor(d => d.PageSize);
    }

    [Theory]
    [InlineData(4)]
    [InlineData(50)]
    [InlineData(51)]
    public void ShouldFailWhenPageSizeIsInvalid(int pageSize)
    {
        var dto = new PaginatedDto<BaseDto> { PageSize = pageSize };
        var result = _paginationValidator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(d => d.PageSize);
    }
}