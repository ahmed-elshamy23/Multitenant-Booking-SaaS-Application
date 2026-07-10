using Booking_SaaS.API.Attributes;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Booking_SaaS.API.OperationFilters;

public class IdempotencyKeyOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var requiresHeader = context.MethodInfo
            .GetCustomAttributes(true)
            .OfType<RequireIdempotencyKeyAttribute>()
            .Any();

        if (!requiresHeader)
            return;

        operation.Parameters ??= new List<OpenApiParameter>();
        operation.Parameters.Add(new OpenApiParameter
        {
            Name = "X-Idempotency-Key",
            In = ParameterLocation.Header,
            Required = true,
            Description = "Idempotency Key",
            Schema = new OpenApiSchema
            {
                Type = "string"
            }
        });
    }
}