using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Sannel.Arcade.Metadata.Client.Services;

namespace Sannel.Arcade.Metadata.Client.Pages;

[Authorize]
public partial class Scan : ComponentBase
{
	[Inject] private IScanService ScanService { get; set; } = null!;
	[Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = null!;

	private ScanStatusResponse _scanStatus = new();
	private string _errorMessage = string.Empty;
	private string _successMessage = string.Empty;
	private bool _isLoading = false;

	protected override async Task OnInitializedAsync()
	{
		var state = await AuthenticationStateProvider.GetAuthenticationStateAsync();
		await RefreshStatus();
	}

	private async Task StartScan()
	{
		_isLoading = true;
		_errorMessage = string.Empty;
		_successMessage = string.Empty;
		StateHasChanged();

		try
		{
			var success = await ScanService.StartScanAsync();
			if (success)
			{
				_successMessage = "Scan started successfully!";
				await RefreshStatus();
			}
			else
			{
				_errorMessage = "Failed to start scan. Please check your settings and try again.";
			}
		}
		catch (Exception ex)
		{
			_errorMessage = $"Error starting scan: {ex.Message}";
		}
		finally
		{
			_isLoading = false;
			StateHasChanged();
		}
	}

	private async Task StopScan()
	{
		_isLoading = true;
		_errorMessage = string.Empty;
		_successMessage = string.Empty;
		StateHasChanged();

		try
		{
			var success = await ScanService.StopScanAsync();
			if (success)
			{
				_successMessage = "Scan stopped successfully!";
				await RefreshStatus();
			}
			else
			{
				_errorMessage = "Failed to stop scan.";
			}
		}
		catch (Exception ex)
		{
			_errorMessage = $"Error stopping scan: {ex.Message}";
		}
		finally
		{
			_isLoading = false;
			StateHasChanged();
		}
	}

	private async Task RefreshStatus()
	{
		_isLoading = true;
		_errorMessage = string.Empty;
		StateHasChanged();

		try
		{
			_scanStatus = await ScanService.GetScanStatusAsync();
			if (!_scanStatus.Success)
			{
				_errorMessage = $"Failed to get scan status: {_scanStatus.Message}";
			}
		}
		catch (Exception ex)
		{
			_errorMessage = $"Error getting scan status: {ex.Message}";
		}
		finally
		{
			_isLoading = false;
			StateHasChanged();
		}
	}
}