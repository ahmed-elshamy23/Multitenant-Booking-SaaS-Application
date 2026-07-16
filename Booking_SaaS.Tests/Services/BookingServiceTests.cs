using AutoMapper;
using Booking_SaaS.Domain.Contracts;
using Booking_SaaS.Domain.Contracts.Repositories;
using Booking_SaaS.Domain.Entities;
using Booking_SaaS.Domain.Enums;
using Booking_SaaS.Domain.Results;
using Booking_SaaS.Services;
using Booking_SaaS.Services.Abstraction.DTOs;
using Booking_SaaS.Services.Abstraction.DTOs.Bookings;
using Booking_SaaS.Services.Specifications;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Moq;

namespace Booking_SaaS.Tests.Services;

public class BookingServiceTests
{
    private readonly Mock<IMapper> _mapper = new();
    private readonly Mock<IValidator<BookingFilterDto>> _filterValidator = new();
    private readonly Mock<IValidator<PaginatedDto<BookingDto>>> _paginationValidator = new();
    private readonly Mock<IBookingRepository> _repo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly BookingService _bookingService;

    public BookingServiceTests()
    {
        _unitOfWork.Setup(x => x.BookingRepository).Returns(_repo.Object);

        _bookingService = new BookingService(_unitOfWork.Object,
                                             _filterValidator.Object,
                                             _mapper.Object,
                                             _paginationValidator.Object);
    }

    [Fact]
    public async Task GetAllAsync_ShouldFailWhenPaginationValidationErrorsExist()
    {
        var fakeFailures = new List<ValidationFailure> { new("Property", "Error") };
        _paginationValidator.Setup(x => x.Validate(It.IsAny<PaginatedDto<BookingDto>>()))
                            .Returns(new ValidationResult(fakeFailures));

        var filterDto = new BookingFilterDto();
        var result = await _bookingService.GetAllAsync(filterDto, 1);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull().And.BeOfType<Error>();
        result.Error.ErrorType.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public async Task GetAllAsync_ShouldFailWhenFilterValidationErrorsExist()
    {
        _paginationValidator.Setup(x => x.Validate(It.IsAny<PaginatedDto<BookingDto>>()))
                            .Returns(new ValidationResult());

        var fakeFailures = new List<ValidationFailure> { new("Property", "Error") };
        _filterValidator.Setup(x => x.Validate(It.IsAny<BookingFilterDto>()))
                        .Returns(new ValidationResult(fakeFailures));

        var filterDto = new BookingFilterDto();
        var result = await _bookingService.GetAllAsync(filterDto, 1);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull().And.BeOfType<Error>();
        result.Error.ErrorType.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public async Task GetAllAsync_ShouldPassAndReturnPaginatedData()
    {
        _paginationValidator.Setup(x => x.Validate(It.IsAny<PaginatedDto<BookingDto>>()))
                            .Returns(new ValidationResult());

        _filterValidator.Setup(x => x.Validate(It.IsAny<BookingFilterDto>()))
                        .Returns(new ValidationResult());

        var filterDto = new BookingFilterDto();
        var totalCount = 5;

        _repo.Setup(x => x.GetCountAsync(It.IsAny<BookingSpecifications.UserFilterSpecification>(),
                                         It.IsAny<CancellationToken>()))
                                        .ReturnsAsync(totalCount);

        var mockBookings = new List<Booking> { new(), new() };
        _repo.Setup(x => x.GetAllAsync(It.IsAny<BookingSpecifications.UserPaginatedFilterSpecification>(),
                                       It.IsAny<CancellationToken>()))
                                      .ReturnsAsync(mockBookings);

        var mockDtos = new List<BookingDto> { new(), new() };
        _mapper.Setup(x => x.Map<List<Booking>, List<BookingDto>>(It.IsAny<List<Booking>>()))
               .Returns(mockDtos);

        var result = await _bookingService.GetAllAsync(filterDto, 1);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.TotalCount.Should().Be(totalCount);
        result.Value.Items.Should().BeEquivalentTo(mockDtos);
        result.Value.PageIndex.Should().Be(filterDto.PageIndex);
        result.Value.PageSize.Should().Be(filterDto.PageSize);
        result.Error.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_ShouldFailWhenBookingNotFound()
    {
        _repo.Setup(x => x.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync((Booking?)null);

        var result = await _bookingService.GetByIdAsync(1, 1);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull().And.BeOfType<Error>();
        result.Error.ErrorType.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldFailWhenUserIdDoesNotMatch()
    {
        var booking = new Booking() { Id = 1, UserId = 2 };
        _repo.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
             .ReturnsAsync(booking);

        var result = await _bookingService.GetByIdAsync(1, 1);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull().And.BeOfType<Error>();
        result.Error.ErrorType.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldPassWhenBookingExistsAndBelongsToUser()
    {
        var userId = 1;
        var booking = new Booking() { Id = 1, UserId = userId };
        var dto = new BookingDto();

        _repo.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
             .ReturnsAsync(booking);

        _mapper.Setup(x => x.Map<Booking, BookingDto>(It.IsAny<Booking>()))
               .Returns(dto);

        var result = await _bookingService.GetByIdAsync(1, userId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(dto);
        result.Error.Should().BeNull();
    }

    [Fact]
    public async Task AddAsync_ShouldPassAndSetPropertiesCorrectly()
    {
        var dto = new BookingAddDto();
        var scheduleId = 10;
        var userId = 1;
        var id = 1;
        var mappedBooking = new Booking() { Id = id };

        _mapper.Setup(x => x.Map<Booking>(It.IsAny<BookingAddDto>()))
               .Returns(mappedBooking);

        var result = await _bookingService.AddAsync(dto, scheduleId, userId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(id);
        result.Error.Should().BeNull();

        mappedBooking.ScheduleId.Should().Be(scheduleId);
        mappedBooking.UserId.Should().Be(userId);
        mappedBooking.Status.Should().Be(BookingStatus.Pending);

        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CancelAsync_ShouldFailWhenBookingNotFound()
    {
        _repo.Setup(x => x.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync((Booking?)null);

        var result = await _bookingService.CancelAsync(1, 1);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull().And.BeOfType<Error>();
        result.Error.ErrorType.Should().Be(ErrorType.NotFound);

        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CancelAsync_ShouldFailWhenUserIdDoesNotMatch()
    {
        var booking = new Booking() { UserId = 2 };

        _repo.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
             .ReturnsAsync(booking);

        var result = await _bookingService.CancelAsync(1, 1);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull().And.BeOfType<Error>();
        result.Error.ErrorType.Should().Be(ErrorType.NotFound);

        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CancelAsync_ShouldFailWhenStatusIsNotPending()
    {
        var booking = new Booking() { Id = 1, UserId = 1, Status = BookingStatus.Completed };

        _repo.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
             .ReturnsAsync(booking);

        var result = await _bookingService.CancelAsync(1, 1);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(Error.Booking.NonPending);

        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CancelAsync_ShouldFailWhenCancellationIsOnSameDayOrPast()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var booking = new Booking() { Id = 1, UserId = 1, Status = BookingStatus.Pending, Date = today };

        _repo.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
             .ReturnsAsync(booking);

        var result = await _bookingService.CancelAsync(1, 1);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(Error.Booking.SameDayCancellation);

        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CancelAsync_ShouldPassWhenSuccessfullyCancelled()
    {
        var tomorrow = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
        var booking = new Booking() { Id = 1, UserId = 1, Status = BookingStatus.Pending, Date = tomorrow };

        _repo.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
             .ReturnsAsync(booking);

        var result = await _bookingService.CancelAsync(1, 1);

        result.IsSuccess.Should().BeTrue();
        result.Error.Should().BeNull();
        booking.Status.Should().Be(BookingStatus.Cancelled);

        _repo.Verify(x => x.Update(It.IsAny<Booking>()), Times.Once);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}