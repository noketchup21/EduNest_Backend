using System.Collections.Concurrent;
using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace EduNest_Backend.Middleware.RateLimit
{
    public class RateLimitMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RateLimitMiddleware> _logger;
        private readonly RateLimitOption _options;

        private static readonly ConcurrentDictionary<string, ClientRequestInfo> _clients = new();

        public RateLimitMiddleware(
            RequestDelegate next,
            ILogger<RateLimitMiddleware> logger,
            IOptions<RateLimitOption> options)
        {
            _next = next;
            _logger = logger;
            _options = options.Value;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var clientIp = GetClientIp(context);
            var now = DateTime.UtcNow;

            var clientInfo = _clients.AddOrUpdate(
                clientIp,
                _ => new ClientRequestInfo { WindowStart = now, RequestCount = 1 },
                (_, existing) =>
                {
                    if ((now - existing.WindowStart).TotalSeconds >= _options.WindowSeconds)
                    {
                        existing.WindowStart = now;
                        existing.RequestCount = 1;
                    }
                    else
                    {
                        existing.RequestCount++;
                    }
                    return existing;
                });

            context.Response.Headers["X-RateLimit-Limit"] = _options.MaxRequests.ToString();
            context.Response.Headers["X-RateLimit-Remaining"] =
                Math.Max(0, _options.MaxRequests - clientInfo.RequestCount).ToString();
            context.Response.Headers["X-RateLimit-Reset"] =
                ((long)(clientInfo.WindowStart.AddSeconds(_options.WindowSeconds) - DateTime.UnixEpoch).TotalSeconds).ToString();

            if (clientInfo.RequestCount > _options.MaxRequests)
            {
                _logger.LogWarning("Rate limit exceeded for IP: {ClientIp}", clientIp);
                context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
                context.Response.ContentType = "application/json";

                var response = new
                {
                    statusCode = 429,
                    message = "Too many requests. Please slow down.",
                    retryAfter = _options.WindowSeconds
                };

                await context.Response.WriteAsync(JsonSerializer.Serialize(response));
                return;
            }

            await _next(context);
        }

        private static string GetClientIp(HttpContext context)
        {
            var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedFor))
                return forwardedFor.Split(',')[0].Trim();

            var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
            if (!string.IsNullOrEmpty(realIp))
                return realIp;

            return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }
    }

    public class ClientRequestInfo
    {
        public DateTime WindowStart { get; set; }
        public int RequestCount { get; set; }
    }
}
