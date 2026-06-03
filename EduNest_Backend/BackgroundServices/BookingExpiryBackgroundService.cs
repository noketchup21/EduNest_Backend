using BusinessLayer.IServices;

namespace EduNest_Backend.BackgroundServices
{
    public sealed class BookingExpiryBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public BookingExpiryBackgroundService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
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
                    Console.WriteLine($"Booking expiry job error: {ex}");
                }

                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }
}
