using System.Net.Http.Json;
using System.Text.Json;

using Sannel.Arcade.Metadata.Client.Models;

namespace Sannel.Arcade.Metadata.Client.Services;

public interface IAuthService
{
	Task<LoginResponse> LoginAsync(LoginRequest loginRequest);
	Task LogoutAsync();
	Task<bool> IsAuthenticatedAsync();
	Task<string?> GetTokenAsync();
}

public class AuthService : IAuthService
{
	private readonly HttpClient _httpClient;
	private readonly ILocalStorageService _localStorage;
	private const string TokenKey = "auth_token";

	public AuthService(HttpClient httpClient, ILocalStorageService localStorage)
	{
		_httpClient = httpClient;
		_localStorage = localStorage;
	}

	public async Task<LoginResponse> LoginAsync(LoginRequest loginRequest)
	{
		try
		{
			HttpResponseMessage response = await _httpClient.PostAsJsonAsync("api/auth/login", loginRequest);
			
			if (response.IsSuccessStatusCode)
			{
				string content = await response.Content.ReadAsStringAsync();
				LoginResponse? loginResponse = JsonSerializer.Deserialize<LoginResponse>(content, new JsonSerializerOptions
				{
					PropertyNameCaseInsensitive = true
				});

				if (loginResponse is not null && loginResponse.Success)
				{
					await _localStorage.SetItemAsync(TokenKey, loginResponse.Token);
				}

				return loginResponse ?? new LoginResponse { Success = false, Message = "Invalid response" };
			}
			else
			{
				string errorContent = await response.Content.ReadAsStringAsync();
				LoginResponse? errorResponse = JsonSerializer.Deserialize<LoginResponse>(errorContent, new JsonSerializerOptions
				{
					PropertyNameCaseInsensitive = true
				});

				return errorResponse ?? new LoginResponse { Success = false, Message = "Login failed" };
			}
		}
		catch (Exception ex)
		{
			return new LoginResponse { Success = false, Message = $"Login error: {ex.Message}" };
		}
	}

	public async Task LogoutAsync()
	{
		await _localStorage.RemoveItemAsync(TokenKey);
	}

	public async Task<bool> IsAuthenticatedAsync()
	{
		string? token = await GetTokenAsync();
		return !string.IsNullOrEmpty(token);
	}

	public async Task<string?> GetTokenAsync()
	{
		return await _localStorage.GetItemAsync(TokenKey);
	}
}