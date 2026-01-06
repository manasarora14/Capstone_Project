using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Hosting;

namespace ServiceManagementApi.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;
    private readonly IWebHostEnvironment _env;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger, IWebHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            
            _logger.LogError(ex, "Unhandled exception while processing request {Method} {Path}", context.Request.Method, context.Request.Path);

            
            var statusCode = HttpStatusCode.InternalServerError;
            var message = "Internal Server Error";

            
            if (ex is UnauthorizedAccessException)
            {
                statusCode = HttpStatusCode.Unauthorized;
                message = "Unauthorized";
            }
            else if (ex is KeyNotFoundException)
            {
                statusCode = HttpStatusCode.NotFound;
                message = ex.Message;
            }
            else if (ex is ArgumentException || ex is ArgumentNullException || ex is FormatException)
            {
                statusCode = HttpStatusCode.BadRequest;
                message = ex.Message;
            }
            else if (ex is InvalidOperationException)
            {
                statusCode = HttpStatusCode.Conflict;
                message = ex.Message;
            }

            
            var payload = new
            {
                status = (int)statusCode,
                message,
                detail = _env.IsDevelopment() ? ex.ToString() : ex.Message,
                traceId = context.TraceIdentifier
            };

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)statusCode;

            var json = JsonSerializer.Serialize(payload);
            await context.Response.WriteAsync(json);
        }
    }
}