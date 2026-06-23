using System.Net;
using System.Text.Json;
using AppointmentBooking.Application.Common;
using AppointmentBooking.Application.Exceptions;

namespace AppointmentBooking.API.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
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
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, message, errors) = exception switch
        {
            NotFoundException notFound => (HttpStatusCode.NotFound, notFound.Message, (IEnumerable<string>?)null),
            ValidationException validation => (HttpStatusCode.BadRequest, validation.Message, validation.Errors),
            ConflictException conflict => (HttpStatusCode.Conflict, conflict.Message, (IEnumerable<string>?)null),
            UnauthorizedException unauthorized => (HttpStatusCode.Unauthorized, unauthorized.Message, (IEnumerable<string>?)null),
            _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred.", (IEnumerable<string>?)null)
        };

        if (statusCode == HttpStatusCode.InternalServerError)
            _logger.LogError(exception, "Unhandled exception occurred.");
        else
            _logger.LogWarning(exception, "Handled application exception: {Message}", message);

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var response = ApiResponse<object>.Fail(message, errors);
        await context.Response.WriteAsync(JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }));
    }
}
