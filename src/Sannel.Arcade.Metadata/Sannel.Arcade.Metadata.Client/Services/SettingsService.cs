using System.Net.Http.Json;
using System.Text.Json;
using System.Text;

using Sannel.Arcade.Metadata.Client.Models;

namespace Sannel.Arcade.Metadata.Client.Services;

public interface ISettingsService
{
	Task<GetRuntimeSettingResponse> GetSettingAsync(string key);
	Task<SetRuntimeSettingResponse> SetSettingAsync(string key, string value);
}

public class SettingsService : ISettingsService
{
	private readonly HttpClient _httpClient;

	public SettingsService(HttpClient httpClient)
	{
		_httpClient = httpClient;
	}

	public async Task<GetRuntimeSettingResponse> GetSettingAsync(string key)
	{
		try
		{
			await SetAuthorizationHeaderAsync();

			HttpResponseMessage response = await _httpClient.GetAsync($"api/v1/settings/{Uri.EscapeDataString(key)}");
			
			if (response.IsSuccessStatusCode)
			{
				string content = await response.Content.ReadAsStringAsync();
				GetRuntimeSettingResponse? settingResponse = JsonSerializer.Deserialize<GetRuntimeSettingResponse>(content, new JsonSerializerOptions
				{
					PropertyNameCaseInsensitive = true
				});

				return settingResponse ?? new GetRuntimeSettingResponse { Success = false, ErrorMessage = "Invalid response" };
			}
			else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
			{
				return new GetRuntimeSettingResponse { Success = false, ErrorMessage = $"Setting '{key}' not found" };
			}
			else
			{
				string errorContent = await response.Content.ReadAsStringAsync();
				GetRuntimeSettingResponse? errorResponse = JsonSerializer.Deserialize<GetRuntimeSettingResponse>(errorContent, new JsonSerializerOptions
				{
					PropertyNameCaseInsensitive = true
				});

				return errorResponse ?? new GetRuntimeSettingResponse { Success = false, ErrorMessage = "Failed to get setting" };
			}
		}
		catch (Exception ex)
		{
			return new GetRuntimeSettingResponse { Success = false, ErrorMessage = $"Error retrieving setting: {ex.Message}" };
		}
	}

	public async Task<SetRuntimeSettingResponse> SetSettingAsync(string key, string value)
	{
		try
		{
			await SetAuthorizationHeaderAsync();

			string encodedKey = Uri.EscapeDataString(key);
			string encodedValue = Uri.EscapeDataString(value);
			string url = $"api/v1/settings/{encodedKey}?value={encodedValue}";

			HttpResponseMessage response = await _httpClient.PutAsync(url, null);
			
			if (response.IsSuccessStatusCode)
			{
				string content = await response.Content.ReadAsStringAsync();
				SetRuntimeSettingResponse? settingResponse = JsonSerializer.Deserialize<SetRuntimeSettingResponse>(content, new JsonSerializerOptions
				{
					PropertyNameCaseInsensitive = true
				});

				return settingResponse ?? new SetRuntimeSettingResponse { Success = false, ErrorMessage = "Invalid response" };
			}
			else
			{
				string errorContent = await response.Content.ReadAsStringAsync();
				SetRuntimeSettingResponse? errorResponse = JsonSerializer.Deserialize<SetRuntimeSettingResponse>(errorContent, new JsonSerializerOptions
				{
					PropertyNameCaseInsensitive = true
				});

				return errorResponse ?? new SetRuntimeSettingResponse { Success = false, ErrorMessage = "Failed to set setting" };
			}
		}
		catch (Exception ex)
		{
			return new SetRuntimeSettingResponse { Success = false, ErrorMessage = $"Error setting value: {ex.Message}" };
		}
	}

	private async Task SetAuthorizationHeaderAsync()
	{
		//string? token = await _authService.GetTokenAsync();
		//if (!string.IsNullOrEmpty(token))
		//{
		//	_httpClient.DefaultRequestHeaders.Authorization = 
		//		new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
		//}
	}
}