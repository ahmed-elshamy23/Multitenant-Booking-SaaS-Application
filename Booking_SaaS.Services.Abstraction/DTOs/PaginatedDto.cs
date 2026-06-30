namespace Booking_SaaS.Services.Abstraction.DTOs;

public class PaginatedDto<T> where T : BaseDto
{
    public List<T> Items { get; set; }
    public int PageIndex { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
}