using Microsoft.AspNetCore.RateLimiting;

namespace EduNest_Backend.Middleware.RateLimit
{
    public static class RateLimitMiddlewareExtensions
    {
        public static IApplicationBuilder UseRateLimiting(this IApplicationBuilder app)
            => app.UseMiddleware<RateLimitMiddleware>();

        public static IServiceCollection AddRateLimiting(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.Configure<RateLimitOption>(
                configuration.GetSection(RateLimitOption.SectionName));
            return services;
        }
    }
}
