using Booking_SaaS.API.Attributes;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Booking_SaaS.API.OperationFilters;

public class TenantHeaderOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var requiresHeader = context.MethodInfo
            .GetCustomAttributes(true)
            .OfType<RequireTenantIdAttribute>()
            .Any();

        if (!requiresHeader)
            return;

        operation.Parameters ??= new List<OpenApiParameter>();
        operation.Parameters.Add(new OpenApiParameter
        {
            Name = "X-Tenant-Id",
            In = ParameterLocation.Header,
            Required = true,
            Description = "Tenant identifier",
            Schema = new OpenApiSchema
            {
                Type = "int"
            }
        });
    }
}