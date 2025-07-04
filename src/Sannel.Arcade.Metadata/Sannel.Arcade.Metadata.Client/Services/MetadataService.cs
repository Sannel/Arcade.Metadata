using System.Net.Http.Json;
using Sannel.Arcade.Metadata.Client.Models;

namespace Sannel.Arcade.Metadata.Client.Services;

public interface IMetadataService
{
	Task<GetPlatformsResponse> GetPlatformsAsync();
	Task<GetGamesResponse> GetGamesAsync(string platformName);
}

public class MetadataService : IMetadataService
{
	private readonly HttpClient _httpClient;

	public MetadataService(HttpClient httpClient)
	{
		_httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
	}

	public async Task<GetPlatformsResponse> GetPlatformsAsync()
	{
		try
		{
			var response = await _httpClient.GetAsync("api/v1/metadata/platforms");
			if (response.IsSuccessStatusCode)
			{
				var result = await response.Content.ReadFromJsonAsync<GetPlatformsResponse>();
				return result ?? new GetPlatformsResponse { Success = false, Message = "Failed to parse response" };
			}
			return new GetPlatformsResponse { Success = false, Message = $"HTTP {response.StatusCode}" };
		}
		catch (Exception ex)
		{
			return new GetPlatformsResponse { Success = false, Message = ex.Message };
		}
	}

	public async Task<GetGamesResponse> GetGamesAsync(string platformName)
	{
		try
		{
			var response = await _httpClient.GetAsync($"api/v1/metadata/platforms/{Uri.EscapeDataString(platformName)}/games");
			if (response.IsSuccessStatusCode)
			{
				var result = await response.Content.ReadFromJsonAsync<GetGamesResponse>();
				return result ?? new GetGamesResponse { Success = false, Message = "Failed to parse response" };
			}
			return new GetGamesResponse { Success = false, Message = $"HTTP {response.StatusCode}" };
		}
		catch (Exception ex)
		{
			return new GetGamesResponse { Success = false, Message = ex.Message };
		}
	}
}