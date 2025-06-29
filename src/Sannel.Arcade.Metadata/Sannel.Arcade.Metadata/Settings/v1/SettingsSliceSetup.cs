using Microsoft.Extensions.DependencyInjection;

using Sannel.Arcade.Metadata.Settings.v1.Services;

namespace Sannel.Arcade.Metadata.Settings.v1;

public static class SettingsSliceSetup
{
	public static void Setup(IHostEnvironment env, IConfiguration configuration, IServiceCollection services)
	{
		if (OperatingSystem.IsLinux())
		{
			services.AddSingleton<IRuntimeSettingsService>(sp =>
			{
				var logger = sp.GetRequiredService<ILogger<KWalletRuntimeSettingsService>>();
				var settingService = new KWalletRuntimeSettingsService(logger);
				settingService.InitializeAsync().GetAwaiter().GetResult();
				return settingService;
			});
		}
		else if (OperatingSystem.IsWindows())
		{
			var settingService = new WindowsCredentialManagerRuntimeSettingsService();
			_ = settingService.InitializeAsync();
			services.AddSingleton<IRuntimeSettingsService>(settingService);
		}
	}
}
