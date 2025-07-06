using System.Net.Http.Json;
using Sannel.Arcade.Metadata.Client.Models;

namespace Sannel.Arcade.Metadata.Client.Services;

public interface IMetadataService
{
	Task<GetPlatformsResponse> GetPlatformsAsync();
	Task<GetGamesResponse> GetGamesAsync(string platformName);
	Task<GameMetadata?> GetGameAsync(string platformName, string gameId);
	Task<bool> UpdateGameAsync(string platformName, string gameId, GameMetadata gameMetadata);
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
				var result = await response.Content.ReadFromJsonAsync<GetGamesResponse>(_jsonOptions.Value);
				return result ?? new GetGamesResponse { Success = false, Message = "Failed to parse response" };
			}
			return new GetGamesResponse { Success = false, Message = $"HTTP {response.StatusCode}" };
		}
		catch (Exception ex)
		{
			return new GetGamesResponse { Success = false, Message = ex.Message };
		}
	}

	private readonly static Lazy<System.Text.Json.JsonSerializerOptions> _jsonOptions = new(() =>
	{
		var c = new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web)
		{
			NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString,
			DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
		};
		c.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
		return c;
	});

	public async Task<GameMetadata?> GetGameAsync(string platformName, string gameId)
	{
		try
		{
			var response = await _httpClient.GetAsync($"api/v1/metadata/platforms/{Uri.EscapeDataString(platformName)}/games/{Uri.EscapeDataString(gameId)}/metadata");
			if (response.IsSuccessStatusCode)
			{
				var result = await response.Content.ReadFromJsonAsync<GameMetadata>(_jsonOptions.Value);
				return result;
			}
			return null;
		}
		catch (Exception ex)
		{
			Console.WriteLine(ex);
			return null;
		}
	}

	public async Task<bool> UpdateGameAsync(string platformName, string gameId, GameMetadata gameMetadata)
	{
		try
		{
			var response = await _httpClient.PutAsJsonAsync($"api/v1/metadata/platforms/{Uri.EscapeDataString(platformName)}/games/{Uri.EscapeDataString(gameId)}/metadata", gameMetadata, _jsonOptions.Value);
			return response.IsSuccessStatusCode;
		}
		catch (Exception ex)
		{
			Console.WriteLine(ex);
			return false;
		}
	}
}