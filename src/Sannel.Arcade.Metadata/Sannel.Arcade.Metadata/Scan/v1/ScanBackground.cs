
namespace Sannel.Arcade.Metadata.Scan.v1;

public class ScanBackground : BackgroundService
{
	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		while (stoppingToken.IsCancellationRequested == false)
		{
		}
	}
}
