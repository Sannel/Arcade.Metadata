using Microsoft.Extensions.DependencyInjection;

using Sannel.Arcade.Metadata.Settings.v1.Services;

namespace Sannel.Arcade.Metadata.Settings.v1;

public static class SettingsSliceSetup
{
	public static void Setup(IHostEnvironment env, IConfiguration configuration, IServiceCollection services)
	{
		if (OperatingSystem.IsLinux())
		{
			var settingService = new KWalletRuntimeSettingsService();
			_ = settingService.InitializeAsync();
			services.AddSingleton<IRuntimeSettingsService>(settingService);
		}
	}
}
