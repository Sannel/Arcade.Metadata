
using Sannel.Arcade.Metadata.Scan.v1.Services;

namespace Sannel.Arcade.Metadata.Scan.v1;

public class ScanBackground : BackgroundService
{
	private readonly IServiceProvider _services;
	public bool ShouldScan { get; set; } = false;

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		while (stoppingToken.IsCancellationRequested == false)
		{
			await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
			if (ShouldScan)
			{
				await BuildMetaDataAsync(stoppingToken);
			}
		}
	}

	private async Task BuildMetaDataAsync(CancellationToken cancellationToken)
	{
		using var scope = _services.CreateScope();
		var scanService = scope.ServiceProvider.GetRequiredService<IScanService>();
		await scanService.ScanAsync(cancellationToken);
	}

}
