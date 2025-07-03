using Sannel.Arcade.Metadata.Scan.v1.Clients;
using Sannel.Arcade.Metadata.Scan.v1.Services;

namespace Sannel.Arcade.Metadata.Scan.v1;

public static class ScanSetup
{
	public static void Setup(IHostEnvironment env, IConfiguration configuration, IServiceCollection services)
	{
		// Register ScanBackground as singleton first
		services.AddSingleton<ScanBackground>();
		
		// Register as hosted service using the singleton instance
		services.AddHostedService(provider => provider.GetRequiredService<ScanBackground>());
		
		// Register other scan services
		services.AddScoped<IScanService, FileScanService>();
		
		// Register IGDB client - it needs IMediator, not HttpClient
		services.AddScoped<IMetadataClient, IgdbClient>();
	}
}
