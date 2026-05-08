namespace EduNest_Backend.Middleware.RateLimit
{
    public class RateLimitOption
    {
        public const string SectionName = "RateLimit";

        /// <summary>Maximum requests allowed per window.</summary>
        public int MaxRequests { get; set; } = 100;

        /// <summary>Time window in seconds.</summary>
        public int WindowSeconds { get; set; } = 60;
    }
}
