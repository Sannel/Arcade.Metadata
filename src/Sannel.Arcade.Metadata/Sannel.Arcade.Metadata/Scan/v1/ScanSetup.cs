using Sannel.Arcade.Metadata.Scan.v1.Clients;
using Sannel.Arcade.Metadata.Scan.v1.Services;

namespace Sannel.Arcade.Metadata.Scan.v1;

public static class ScanSetup
{
	public static void Setup(IHostEnvironment env, IConfiguration configuration, IServiceCollection services)
	{
		services.AddHostedService<ScanBackground>();
		services.AddScoped<IScanService, FileScanService>();
		
		// Register IGDB client
		services.AddHttpClient<IMetadataClient, IgdbClient>();
	}
}
