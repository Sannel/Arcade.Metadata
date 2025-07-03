using Sannel.Arcade.Metadata.Scan.v1.Services;

namespace Sannel.Arcade.Metadata.Scan.v1;

public class ScanBackground : BackgroundService
{
	private readonly IServiceProvider _services;
	public bool ShouldScan { get; set; } = false;

	public ScanBackground(IServiceProvider services)
	{
		_services = services ?? throw new ArgumentNullException(nameof(services));
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		while (stoppingToken.IsCancellationRequested == false)
		{
			await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
			if (ShouldScan)
			{
				await BuildMetaDataAsync(stoppingToken);
				ShouldScan = false; // Stop scanning after one run
			}
		}
	}

	private async Task BuildMetaDataAsync(CancellationToken cancellationToken)
	{
		try
		{
			using var scope = _services.CreateScope();
			var scanService = scope.ServiceProvider.GetRequiredService<IScanService>();
			await scanService.ScanAsync(cancellationToken);
		}
		catch (Exception ex)
		{
			// Log the exception or handle it as needed
			Console.WriteLine($"Error during scan: {ex.Message}");
		}
	}
}
