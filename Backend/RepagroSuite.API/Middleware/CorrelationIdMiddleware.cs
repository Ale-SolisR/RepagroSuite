using Serilog.Context;

namespace RepagroSuite.API.Middleware;

// Asigna un correlation ID por request (toma X-Correlation-Id del cliente o genera uno).
// Se propaga en logs vía Serilog LogContext y se devuelve en la respuesta para que
// el frontend pueda mostrarlo al usuario en mensajes de error ("Soporte ID: abc123").
public class CorrelationIdMiddleware
{
    private const string Header = "X-Correlation-Id";
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var cid = context.Request.Headers[Header].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(cid) || cid.Length > 64)
            cid = Guid.NewGuid().ToString("N")[..16];

        context.Response.Headers[Header] = cid;
        context.Items[Header] = cid;

        using (LogContext.PushProperty("CorrelationId", cid))
        {
            await _next(context);
        }
    }
}
