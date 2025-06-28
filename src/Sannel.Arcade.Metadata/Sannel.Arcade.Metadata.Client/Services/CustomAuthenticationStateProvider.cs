using Microsoft.AspNetCore.Components.Authorization;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

using Sannel.Arcade.Metadata.Client.Services;

namespace Sannel.Arcade.Metadata.Client.Services;

public class CustomAuthenticationStateProvider : AuthenticationStateProvider
{
	private readonly IAuthService _authService;
	private readonly ILocalStorageService _localStorage;

	public CustomAuthenticationStateProvider(IAuthService authService, ILocalStorageService localStorage)
	{
		_authService = authService;
		_localStorage = localStorage;
	}

	public override async Task<AuthenticationState> GetAuthenticationStateAsync()
	{
		string? token = await _authService.GetTokenAsync();

		if (string.IsNullOrEmpty(token))
		{
			return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
		}

		try
		{
			JwtSecurityTokenHandler tokenHandler = new();
			JwtSecurityToken jwtToken = tokenHandler.ReadJwtToken(token);

			// Check if token is expired
			if (jwtToken.ValidTo < DateTime.UtcNow)
			{
				await _authService.LogoutAsync();
				return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
			}

			ClaimsIdentity identity = new(jwtToken.Claims, "jwt");
			ClaimsPrincipal user = new(identity);

			return new AuthenticationState(user);
		}
		catch
		{
			await _authService.LogoutAsync();
			return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
		}
	}

	public void NotifyUserAuthentication(string token)
	{
		try
		{
			JwtSecurityTokenHandler tokenHandler = new();
			JwtSecurityToken jwtToken = tokenHandler.ReadJwtToken(token);

			ClaimsIdentity identity = new(jwtToken.Claims, "jwt");
			ClaimsPrincipal user = new(identity);

			AuthenticationState authState = new(user);
			NotifyAuthenticationStateChanged(Task.FromResult(authState));
		}
		catch
		{
			// If token parsing fails, notify as unauthenticated
			NotifyUserLogout();
		}
	}

	public void NotifyUserLogout()
	{
		AuthenticationState authState = new(new ClaimsPrincipal(new ClaimsIdentity()));
		NotifyAuthenticationStateChanged(Task.FromResult(authState));
	}
}