using Sannel.Arcade.Metadata.Metadata.v1.Services;

namespace Sannel.Arcade.Metadata.Metadata.v1;

public static class MetadataSliceSetup
{
	public static void Setup(IHostEnvironment env, IConfiguration configuration, IServiceCollection services)
	{
		// Register metadata services
		services.AddScoped<IMetadataService, MetadataService>();
	}
}