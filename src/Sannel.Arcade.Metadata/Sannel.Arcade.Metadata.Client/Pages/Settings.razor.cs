using Sannel.Arcade.Metadata.Client.Models;
using Sannel.Arcade.Metadata.Client.Services;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Authorization;
using System.Reflection.Metadata;
using Microsoft.AspNetCore.Components.Authorization;

namespace Sannel.Arcade.Metadata.Client.Pages;

[Authorize]
public partial class Settings : ComponentBase
{
	[Inject] private ISettingsService SettingsService { get; set; } = null!;

	[Inject]NavigationManager Navigation { get; set; } = null!;

	[Inject]AuthenticationStateProvider AuthenticationStateProvider { get; set; } = null!;

	private IgdbSettings _igdbSettings = new();
	private RomsSettings _romsSettings = new();
	private string _errorMessage = string.Empty;
	private string _successMessage = string.Empty;
	private bool _isLoading = false;

	private const string IgdbClientIdKey = "igdb.clientId";
	private const string IgdbClientSecretKey = "igdb.clientSecret";
	private const string RomsRootKey = "roms.root";


	protected override async Task OnInitializedAsync()
	{
		var state = await AuthenticationStateProvider.GetAuthenticationStateAsync();
		await LoadSettings();
	}

	private async Task LoadSettings()
	{
		await LoadIgdbSettings();
		await LoadRomsSettings();
	}

	private async Task LoadIgdbSettings()
	{
		_isLoading = true;
		_errorMessage = string.Empty;
		_successMessage = string.Empty;

		try
		{
			// Load Client ID
			GetRuntimeSettingResponse clientIdResponse = await SettingsService.GetSettingAsync(IgdbClientIdKey);
			if (clientIdResponse.Success && clientIdResponse.Setting is not null)
			{
				_igdbSettings.ClientId = clientIdResponse.Setting.Value;
			}

			// Load Client Secret
			GetRuntimeSettingResponse clientSecretResponse = await SettingsService.GetSettingAsync(IgdbClientSecretKey);
			if (clientSecretResponse.Success && clientSecretResponse.Setting is not null)
			{
				_igdbSettings.ClientSecret = clientSecretResponse.Setting.Value;
			}
		}
		catch (Exception ex)
		{
			_errorMessage = $"Error loading IGDB settings: {ex.Message}";
		}
		finally
		{
			_isLoading = false;
			StateHasChanged();
		}
	}

	private async Task LoadRomsSettings()
	{
		_isLoading = true;
		_errorMessage = string.Empty;
		_successMessage = string.Empty;

		try
		{
			// Load ROMs root directory
			GetRuntimeSettingResponse romsRootResponse = await SettingsService.GetInsecureSettingAsync(RomsRootKey);
			if (romsRootResponse.Success && romsRootResponse.Setting is not null)
			{
				_romsSettings.RootDirectory = romsRootResponse.Setting.Value;
			}
		}
		catch (Exception ex)
		{
			_errorMessage = $"Error loading ROMs settings: {ex.Message}";
		}
		finally
		{
			_isLoading = false;
			StateHasChanged();
		}
	}

	private async Task SaveIgdbSettings()
	{
		_isLoading = true;
		_errorMessage = string.Empty;
		_successMessage = string.Empty;

		try
		{
			// Save Client ID
			SetRuntimeSettingResponse clientIdResponse = await SettingsService.SetSettingAsync(IgdbClientIdKey, _igdbSettings.ClientId);
			if (!clientIdResponse.Success)
			{
				_errorMessage = $"Failed to save Client ID: {clientIdResponse.ErrorMessage}";
				return;
			}

			// Save Client Secret
			SetRuntimeSettingResponse clientSecretResponse = await SettingsService.SetSettingAsync(IgdbClientSecretKey, _igdbSettings.ClientSecret);
			if (!clientSecretResponse.Success)
			{
				_errorMessage = $"Failed to save Client Secret: {clientSecretResponse.ErrorMessage}";
				return;
			}

			_successMessage = "IGDB settings saved successfully!";
		}
		catch (Exception ex)
		{
			_errorMessage = $"Error saving IGDB settings: {ex.Message}";
		}
		finally
		{
			_isLoading = false;
			StateHasChanged();
		}
	}

	private async Task SaveRomsSettings()
	{
		_isLoading = true;
		_errorMessage = string.Empty;
		_successMessage = string.Empty;

		try
		{
			// Save ROMs root directory
			SetRuntimeSettingResponse romsRootResponse = await SettingsService.SetInsecureSettingAsync(RomsRootKey, _romsSettings.RootDirectory);
			if (!romsRootResponse.Success)
			{
				_errorMessage = $"Failed to save ROMs root directory: {romsRootResponse.ErrorMessage}";
				return;
			}

			_successMessage = "ROMs settings saved successfully!";
		}
		catch (Exception ex)
		{
			_errorMessage = $"Error saving ROMs settings: {ex.Message}";
		}
		finally
		{
			_isLoading = false;
			StateHasChanged();
		}
	}
}