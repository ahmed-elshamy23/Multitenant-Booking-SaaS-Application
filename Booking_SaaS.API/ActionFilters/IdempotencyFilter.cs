using Booking_SaaS.Domain.Results;
using Booking_SaaS.Services.Abstraction;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Text.Json;

namespace Booking_SaaS.API.ActionFilters;

public class IdempotencyFilter : IAsyncActionFilter
{
    private const string IdempotencyHeader = "X-Idempotency-Key";
    private readonly ICachingService _cache;

    public IdempotencyFilter(ICachingService cache)
    {
        _cache = cache;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!context.HttpContext.Request.Headers.TryGetValue(IdempotencyHeader, out var extractedKey) || string.IsNullOrEmpty(extractedKey))
        {
            context.Result = new ContentResult
            {
                StatusCode = StatusCodes.Status400BadRequest,
                ContentType = "application/json",
                Content = JsonSerializer.Serialize(Result.Failure(Error.Idempotency.MissingHeader))
            };
            return;
        }

        var cacheKey = $"idempotency:{extractedKey}";
        var cachedData = await _cache.GetAsync(cacheKey);
        if (!string.IsNullOrEmpty(cachedData))
        {
            var cachedResponse = JsonSerializer.Deserialize<CachedResponse>(cachedData);
            if (cachedResponse != null)
            {
                context.Result = new ContentResult
                {
                    Content = cachedResponse.JsonPayload,
                    ContentType = "application/json",
                    StatusCode = cachedResponse.StatusCode
                };
                return;
            }
        }

        var executedContext = await next();
        if (executedContext.Exception == null && executedContext.Result is ObjectResult objectResult)
        {
            var statusCode = objectResult.StatusCode ?? 200;
            if (statusCode >= 200 && statusCode < 300)
            {
                var jsonPayload = JsonSerializer.Serialize(objectResult.Value);
                var responseToCache = new CachedResponse
                {
                    StatusCode = statusCode,
                    JsonPayload = jsonPayload
                };

                await _cache.SetAsync(cacheKey, JsonSerializer.Serialize(responseToCache));
            }
        }
    }

    private class CachedResponse
    {
        public int StatusCode { get; set; }
        public string JsonPayload { get; set; } = string.Empty;
    }
}