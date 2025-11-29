namespace API.Middleware;

using System.Net;
using System.Text.Json;
using Domain.Exceptions;

/// <summary>
/// Global error handling middleware.
/// Transforms exceptions into consistent API error responses.
/// </summary>
public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
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

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var response = new ErrorResponse();

        switch (exception)
        {
            case NotFoundException ex:
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                response.Message = ex.Message;
                response.Code = ex.Code;
                break;

            case DomainException ex:
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Message = ex.Message;
                response.Code = ex.Code;
                break;

            case FluentValidation.ValidationException ex:
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Message = "Validation failed";
                response.Code = "VALIDATION_ERROR";
                response.Errors = ex.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(e => e.ErrorMessage).ToArray());
                break;

            default:
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                response.Message = "An unexpected error occurred";
                response.Code = "INTERNAL_ERROR";
                break;
        }

        return context.Response.WriteAsJsonAsync(response);
    }
}

/// <summary>
/// Standard API error response format.
/// </summary>
public class ErrorResponse
{
    public string Message { get; set; } = null!;
    public string Code { get; set; } = null!;
    public Dictionary<string, string[]>? Errors { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
