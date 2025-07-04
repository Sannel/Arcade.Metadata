using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Sannel.Arcade.Metadata.Client.Services;
using Sannel.Arcade.Metadata.Client.Models;

namespace Sannel.Arcade.Metadata.Client.Pages;

[Authorize]
public partial class Collection : ComponentBase
{
	[Inject] private IMetadataService MetadataService { get; set; } = null!;
	[Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = null!;
	[Inject] private NavigationManager Navigation { get; set; } = null!;

	private GetPlatformsResponse _platformsResponse = new();
	private string _errorMessage = string.Empty;
	private bool _isLoading = false;

	protected override async Task OnInitializedAsync()
	{
		var state = await AuthenticationStateProvider.GetAuthenticationStateAsync();
		await LoadPlatforms();
	}

	private async Task LoadPlatforms()
	{
		_isLoading = true;
		_errorMessage = string.Empty;
		StateHasChanged();

		try
		{
			_platformsResponse = await MetadataService.GetPlatformsAsync();
			if (!_platformsResponse.Success)
			{
				_errorMessage = _platformsResponse.Message;
			}
		}
		catch (Exception ex)
		{
			_errorMessage = $"Error loading platforms: {ex.Message}";
		}
		finally
		{
			_isLoading = false;
			StateHasChanged();
		}
	}

	private void NavigateToGames(string platformName)
	{
		Navigation.NavigateTo($"/games/{Uri.EscapeDataString(platformName)}");
	}

	private void NavigateToScan()
	{
		Navigation.NavigateTo("/scan");
	}

	private void RefreshPlatforms()
	{
		_ = Task.Run(LoadPlatforms);
	}
}