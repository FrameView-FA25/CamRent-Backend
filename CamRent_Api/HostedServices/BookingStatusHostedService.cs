using CamRent_Application.IServices;

namespace CamRent_Api.HostedServices
{
	public class BookingStatusHostedService : BackgroundService
	{
		private readonly ILogger<BookingStatusHostedService> _logger;
		private readonly IServiceProvider _provider;
		private readonly TimeSpan _interval = TimeSpan.FromMinutes(1);

		public BookingStatusHostedService(ILogger<BookingStatusHostedService> logger, IServiceProvider provider)
		{
			_logger = logger;
			_provider = provider;
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			while (!stoppingToken.IsCancellationRequested)
			{
				try
				{
					using var scope = _provider.CreateScope();
					var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
					var count = await bookingService.ProcessStatusesAsync(DateTime.UtcNow);
					if (count > 0)
					{
						_logger.LogInformation("BookingStatusHostedService updated {Count} bookings", count);
					}
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Error processing booking statuses");
				}
				await Task.Delay(_interval, stoppingToken);
			}
		}
	}
}
