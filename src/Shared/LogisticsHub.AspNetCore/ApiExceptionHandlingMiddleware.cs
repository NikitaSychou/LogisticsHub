using System.Runtime.ExceptionServices;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace LogisticsHub.AspNetCore;

public sealed class ApiExceptionHandlingMiddleware
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web);
    private const string ProblemType = "https://httpstatuses.com/500";
    private const string ProblemTitle = "An unexpected error occurred.";
    private const string ProblemDetailPrefix = "Something went wrong. See logs for details.";

    private readonly RequestDelegate _next;
    private readonly ILogger<ApiExceptionHandlingMiddleware> _logger;

    public ApiExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ApiExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(context, exception);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var correlationId = GetCorrelationId(context);

        _logger.LogError(
            exception,
            "Unhandled API exception. CorrelationId {CorrelationId}.",
            correlationId);

        if (context.Response.HasStarted)
        {
            ExceptionDispatchInfo.Capture(exception).Throw();
        }

        context.Response.Clear();
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/problem+json";
        context.Response.Headers[CorrelationIdMiddleware.CorrelationIdHeaderName] = correlationId;

        var problemDetails = new ProblemDetails
        {
            Type = ProblemType,
            Title = ProblemTitle,
            Status = StatusCodes.Status500InternalServerError,
            Detail = $"{ProblemDetailPrefix} CorrelationId={correlationId}"
        };
        problemDetails.Extensions["correlationId"] = correlationId;

        await JsonSerializer.SerializeAsync(
            context.Response.Body,
            problemDetails,
            JsonSerializerOptions,
            context.RequestAborted);
    }

    private static string GetCorrelationId(HttpContext context)
    {
        if (context.Items.TryGetValue(CorrelationIdMiddleware.CorrelationIdName, out var value)
            && value is string correlationId
            && !string.IsNullOrWhiteSpace(correlationId))
        {
            return correlationId;
        }

        var headerValue = context.Request.Headers[CorrelationIdMiddleware.CorrelationIdHeaderName].ToString();

        return string.IsNullOrWhiteSpace(headerValue)
            ? context.TraceIdentifier
            : headerValue;
    }
}
