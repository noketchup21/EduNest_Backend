using BusinessLayer.IServices;

namespace EduNest_Backend.BackgroundServices
{
    public sealed class BookingExpiryBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<BookingExpiryBackgroundService> _logger;

        public BookingExpiryBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger<BookingExpiryBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();

                    var bookingService = scope.ServiceProvider
                        .GetRequiredService<IBookingService>();

                    await bookingService.ExpirePendingBookingsAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Booking expiry job failed.");
                }

                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }
}
