using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;
using Sannel.Arcade.Metadata.Auth.v1.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Components;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.DataProtection;
using System.Text.Json;

namespace Sannel.Arcade.Metadata.Auth.v1.Pages;

[AllowAnonymous]
public partial class Login
{
	[Inject] private IOptions<AuthenticationConfig> AuthConfig { get; set; } = default!;
	[Inject] private NavigationManager Navigation { get; set; } = default!;
	[Inject] private IDataProtectionProvider DataProtectionProvider { get; set; } = default!;

	private LoginModel loginModel = new();
	private string? errorMessage;
	private bool isLoading;

	private async Task HandleSubmit()
	{
		isLoading = true;
		errorMessage = null;

		try
		{
			var config = AuthConfig.Value;
			
			// Validate credentials against configuration
			if (loginModel.Username == config.Username && loginModel.Password == config.Password)
			{
				loginModel.Password = config.ForwardPassword;
				var protection = DataProtectionProvider.CreateProtector("Sannel.Arcade.Metadata.Auth.v1.Login");
				var data = protection.Protect(JsonSerializer.Serialize(loginModel));
				Navigation.NavigateTo($"/api/v1/postlogin?data={Uri.EscapeDataString(data)}", forceLoad: true);
				/*// Create claims for the user
				var claims = new[]
				{
					new Claim(ClaimTypes.Name, loginModel.Username),
					new Claim(ClaimTypes.NameIdentifier, loginModel.Username),
					new Claim("username", loginModel.Username)
				};

				var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
				var principal = new ClaimsPrincipal(identity);

				DataProtectionProvider.cre
				// Sign in the user with cookie authentication
				var httpContext = HttpContextAccessor.HttpContext;
				if (httpContext != null)
				{
					await httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
					
					// Redirect to home page after successful login
					Navigation.NavigateTo("/", forceLoad: true);
				}
				*/
			}
			else
			{
				errorMessage = "Invalid username or password.";
			}
		}
		catch (Exception ex)
		{
			errorMessage = $"Login failed: {ex.Message}";
		}
		finally
		{
			isLoading = false;
		}
	}

}
