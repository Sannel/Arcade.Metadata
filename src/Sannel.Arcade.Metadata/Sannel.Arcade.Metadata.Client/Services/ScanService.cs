using System.Net.Http.Json;

namespace Sannel.Arcade.Metadata.Client.Services;

public interface IScanService
{
	Task<bool> StartScanAsync();
	Task<bool> StopScanAsync();
	Task<ScanStatusResponse> GetScanStatusAsync();
}

public class ScanService : IScanService
{
	private readonly HttpClient _httpClient;

	public ScanService(HttpClient httpClient)
	{
		_httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
	}

	public async Task<bool> StartScanAsync()
	{
		try
		{
			var response = await _httpClient.PostAsync("api/v1/scan/start", null);
			if (response.IsSuccessStatusCode)
			{
				var result = await response.Content.ReadFromJsonAsync<ScanResponse>();
				return result?.Success ?? false;
			}
			return false;
		}
		catch
		{
			return false;
		}
	}

	public async Task<bool> StopScanAsync()
	{
		try
		{
			var response = await _httpClient.PostAsync("api/v1/scan/stop", null);
			if (response.IsSuccessStatusCode)
			{
				var result = await response.Content.ReadFromJsonAsync<ScanResponse>();
				return result?.Success ?? false;
			}
			return false;
		}
		catch
		{
			return false;
		}
	}

	public async Task<ScanStatusResponse> GetScanStatusAsync()
	{
		try
		{
			var response = await _httpClient.GetAsync("api/v1/scan/status");
			if (response.IsSuccessStatusCode)
			{
				var result = await response.Content.ReadFromJsonAsync<ScanStatusResponse>();
				return result ?? new ScanStatusResponse { Success = false, Message = "Failed to parse response" };
			}
			return new ScanStatusResponse { Success = false, Message = $"HTTP {response.StatusCode}" };
		}
		catch (Exception ex)
		{
			return new ScanStatusResponse { Success = false, Message = ex.Message };
		}
	}
}

public class ScanResponse
{
	public bool Success { get; set; }
	public string Message { get; set; } = string.Empty;
}

public class ScanStatusResponse : ScanResponse
{
	public bool IsScanning { get; set; }
}