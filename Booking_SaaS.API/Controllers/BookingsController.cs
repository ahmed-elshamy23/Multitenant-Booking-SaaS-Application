using System.Security.Claims;
using Booking_SaaS.API.ActionFilters;
using Booking_SaaS.API.Attributes;
using Booking_SaaS.Domain.Results;
using Booking_SaaS.Services.Abstraction;
using Booking_SaaS.Services.Abstraction.DTOs;
using Booking_SaaS.Services.Abstraction.DTOs.Bookings;
using Booking_SaaS.Services.Abstraction.Orchestrators;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Booking_SaaS.API.Controllers;

[Route("api")]
[Authorize(Roles = "user")]
public class BookingsController : ApiController
{
    private readonly IBookingOrchestrator _bookingOrchestrator;

    public BookingsController(IServiceManager serviceManager)
    {
        _bookingOrchestrator = serviceManager.BookingOrchestrator;
    }

    [HttpGet("bookings/my")]
    [SwaggerOperation(OperationId = "Booking_GetAll")]
    public async Task<ActionResult<Result<PaginatedDto<BookingDto>>>> GetAllAsync(
        [FromQuery] BookingFilterDto filterDto, CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await _bookingOrchestrator.GetAllAsync(filterDto, int.Parse(userId), cancellationToken);
        return ToApiResponse(result);
    }

    [HttpPost("schedules/{scheduleId:int}/bookings")]
    [SwaggerOperation(OperationId = "Booking_Add")]
    [ServiceFilter(typeof(IdempotencyFilter))]
    [RequireIdempotencyKey]
    public async Task<ActionResult<Result<int>>> AddAsync(BookingAddDto bookingDto, int scheduleId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await _bookingOrchestrator.AddAsync(bookingDto, scheduleId, int.Parse(userId));
        return ToApiResponse(result);
    }

    [HttpPut("bookings/{bookingId:int}/cancel")]
    [SwaggerOperation(OperationId = "Booking_Cancel")]
    [ServiceFilter(typeof(IdempotencyFilter))]
    [RequireIdempotencyKey]
    public async Task<ActionResult<Result>> CancelAsync(int bookingId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await _bookingOrchestrator.CancelAsync(bookingId, int.Parse(userId));
        return ToApiResponse(result);
    }
}