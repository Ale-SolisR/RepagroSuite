using System.Net;
using System.Text.Json;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace RepagroSuite.API.Middleware;

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
        var (statusCode, code, message, errors) = exception switch
        {
            ValidationException vex => (
                HttpStatusCode.BadRequest, "VALIDATION_ERROR",
                "Existen errores de validación.",
                vex.Errors.GroupBy(e => e.PropertyName.ToLowerInvariant())
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToList())
            ),
            UnauthorizedAccessException uex => (HttpStatusCode.Unauthorized, "UNAUTHORIZED", uex.Message, (Dictionary<string, List<string>>?)null),
            KeyNotFoundException knfex => (HttpStatusCode.NotFound, "NOT_FOUND", knfex.Message, (Dictionary<string, List<string>>?)null),
            DbUpdateConcurrencyException => (HttpStatusCode.Conflict, "CONCURRENCY_CONFLICT",
                "Otro usuario modificó este registro mientras lo editabas. Recarga e intenta de nuevo.",
                (Dictionary<string, List<string>>?)null),
            InvalidOperationException ioex => (HttpStatusCode.UnprocessableEntity, "BUSINESS_RULE_VIOLATION", ioex.Message, (Dictionary<string, List<string>>?)null),
            _ => (HttpStatusCode.InternalServerError, "INTERNAL_ERROR", "Ocurrió un error inesperado. Intente nuevamente.", (Dictionary<string, List<string>>?)null)
        };

        if ((int)statusCode >= 500)
            _logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);

        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";

        var response = new
        {
            success = false,
            code,
            message,
            errors
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        }));
    }
}
