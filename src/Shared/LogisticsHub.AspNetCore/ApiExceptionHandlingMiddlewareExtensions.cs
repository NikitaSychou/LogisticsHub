using Microsoft.AspNetCore.Builder;

namespace LogisticsHub.AspNetCore;

public static class ApiExceptionHandlingMiddlewareExtensions
{
    public static IApplicationBuilder UseApiExceptionHandling(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ApiExceptionHandlingMiddleware>();
    }
}
