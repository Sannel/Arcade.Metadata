using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Sannel.Arcade.Metadata.Client.Services;

namespace Sannel.Arcade.Metadata.Client.Pages;

[Authorize]
public partial class Scan : ComponentBase, IDisposable
{
	[Inject] private IScanService ScanService { get; set; } = null!;
	[Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = null!;

	private ScanStatusResponse _scanStatus = new();
	private string _errorMessage = string.Empty;
	private string _successMessage = string.Empty;
	private bool _isLoading = false;
	private Timer? _statusPollingTimer;
	private bool _wasScanningPreviously = false;

	protected override async Task OnInitializedAsync()
	{
		var state = await AuthenticationStateProvider.GetAuthenticationStateAsync();
		await RefreshStatus();
		StartStatusPolling();
	}

	private void StartStatusPolling()
	{
		// Poll status every 2 seconds when scanning is active
		_statusPollingTimer = new Timer(async _ => await PollScanStatus(), null, TimeSpan.Zero, TimeSpan.FromSeconds(2));
	}

	private async Task PollScanStatus()
	{
		if (_isLoading) return; // Don't poll if we're already loading

		try
		{
			var newStatus = await ScanService.GetScanStatusAsync();
			
			// Check if scan just completed
			if (_wasScanningPreviously && newStatus.Success && !newStatus.IsScanning)
			{
				_successMessage = "Scan completed successfully!";
				_errorMessage = string.Empty;
				await InvokeAsync(StateHasChanged);
			}
			
			// Update status and track previous state
			_wasScanningPreviously = _scanStatus.IsScanning;
			_scanStatus = newStatus;
			
			if (!_scanStatus.Success && !_isLoading)
			{
				_errorMessage = $"Failed to get scan status: {_scanStatus.Message}";
			}
			
			await InvokeAsync(StateHasChanged);
		}
		catch (Exception ex)
		{
			if (!_isLoading)
			{
				_errorMessage = $"Error polling scan status: {ex.Message}";
				await InvokeAsync(StateHasChanged);
			}
		}
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
				_wasScanningPreviously = false; // Reset to detect completion
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

	public void Dispose()
	{
		_statusPollingTimer?.Dispose();
	}
}