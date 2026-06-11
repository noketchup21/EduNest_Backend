using System.Runtime.ExceptionServices;
using System.Text.Json;

namespace EduNest_Backend.Middleware
{
    public sealed class ApiExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ApiExceptionHandlingMiddleware> _logger;
        private readonly IHostEnvironment _environment;

        public ApiExceptionHandlingMiddleware(
            RequestDelegate next,
            ILogger<ApiExceptionHandlingMiddleware> logger,
            IHostEnvironment environment)
        {
            _next = next;
            _logger = logger;
            _environment = environment;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await WriteErrorAsync(context, ex);
            }
        }

        private async Task WriteErrorAsync(HttpContext context, Exception ex)
        {
            if (context.Response.HasStarted)
            {
                _logger.LogError(ex, "Unhandled exception after response started.");
                ExceptionDispatchInfo.Capture(ex).Throw();
            }

            var statusCode = ex switch
            {
                KeyNotFoundException => StatusCodes.Status404NotFound,
                UnauthorizedAccessException => StatusCodes.Status403Forbidden,
                InvalidOperationException => StatusCodes.Status400BadRequest,
                ArgumentException => StatusCodes.Status400BadRequest,
                _ => StatusCodes.Status500InternalServerError
            };

            if (statusCode >= StatusCodes.Status500InternalServerError)
            {
                _logger.LogError(ex, "Unhandled request exception.");
            }
            else
            {
                _logger.LogWarning(ex, "Handled request exception.");
            }

            var response = new
            {
                statusCode,
                message = statusCode >= StatusCodes.Status500InternalServerError
                    ? "Server error."
                    : ex.Message,
                traceId = context.TraceIdentifier,
                detail = _environment.IsDevelopment()
                    ? ex.ToString()
                    : null
            };

            context.Response.Clear();
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";

            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
    }
}
