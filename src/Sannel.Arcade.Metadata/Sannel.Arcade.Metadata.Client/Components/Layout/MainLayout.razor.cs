using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using MudBlazor;

namespace Sannel.Arcade.Metadata.Client.Components.Layout;

public partial class MainLayout : LayoutComponentBase
{
	private bool _drawerOpen = true;
	private bool _isDarkMode = true;
	private MudThemeProvider _mudThemeProvider = default!;

	void DrawerToggle()
	{
		_drawerOpen = !_drawerOpen;
	}

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (firstRender)
		{
			_isDarkMode = await _mudThemeProvider.GetSystemDarkModeAsync();
			await _mudThemeProvider.WatchSystemDarkModeAsync(OnSystemDarkModeChanged);
			StateHasChanged();
		}
	}

	private Task OnSystemDarkModeChanged(bool newValue)
	{
		_isDarkMode = newValue;
		StateHasChanged();
		return Task.CompletedTask;
	}
}
