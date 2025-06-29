using System.Security.Claims;
using System.Text.Json;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

using Sannel.Arcade.Metadata.Auth.v1.Models;

namespace Sannel.Arcade.Metadata.Auth.v1.Controllers;
/// <summary>
/// Controller for managing runtime settings.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[AllowAnonymous]
public class PostLogin : ControllerBase
{
	private readonly IDataProtectionProvider _dataProtectionProvider;
	private readonly IOptions<AuthenticationConfig> _authConfig;

	public PostLogin(IDataProtectionProvider dataProtectionProvider, IOptions<AuthenticationConfig> authConfig)
	{
		_dataProtectionProvider = dataProtectionProvider;
		_authConfig = authConfig ?? throw new ArgumentNullException(nameof(authConfig));
	}

	[HttpGet]
	public async Task<IActionResult> Get([FromQuery] string data)
	{
		if (string.IsNullOrWhiteSpace(data))
		{
			return new BadRequestObjectResult("Data cannot be empty.");
		}
		var protector = _dataProtectionProvider.CreateProtector("Sannel.Arcade.Metadata.Auth.v1.Login");
		try
		{
			var json = protector.Unprotect(data);
			var model = JsonSerializer.Deserialize<LoginModel>(json);
			if (model == null)
			{
				return new BadRequestObjectResult("Invalid data format.");
			}

			if (model.Username == _authConfig.Value.Username && model.Password == _authConfig.Value.ForwardPassword)
			{
				// Create claims for the user
				var claims = new[]
				{
					new Claim(ClaimTypes.Name, model.Username),
					new Claim(ClaimTypes.NameIdentifier, model.Username),
					new Claim("username", model.Username)
				};

				var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
				var principal = new ClaimsPrincipal(identity);

				return SignIn(principal, new AuthenticationProperties()
				{
					RedirectUri = "/"
				}, CookieAuthenticationDefaults.AuthenticationScheme);

			}
			else
			{
				return new UnauthorizedObjectResult("Invalid username or password.");
			}
		}
		catch (Exception ex)
		{
			return new BadRequestObjectResult($"Failed to unprotect data: {ex.Message}");
		}
	}
}
