using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

using Sannel.Arcade.Metadata.Models;

namespace Sannel.Arcade.Metadata.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
	private readonly AuthenticationConfig _authConfig;

	public AuthController(IOptions<AuthenticationConfig> authConfig)
	{
		_authConfig = authConfig.Value;
	}

	[HttpPost("login")]
	public ActionResult<LoginResponse> Login([FromBody] LoginRequest request)
	{
		// Validate credentials against configuration
		if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
		{
			return BadRequest(new LoginResponse
			{
				Success = false,
				Message = "Username and password are required."
			});
		}

		if (request.Username != _authConfig.Username || request.Password != _authConfig.Password)
		{
			return Unauthorized(new LoginResponse
			{
				Success = false,
				Message = "Invalid username or password."
			});
		}

		// Generate JWT token
		string token = GenerateJwtToken(request.Username);

		return Ok(new LoginResponse
		{
			Success = true,
			Token = token,
			Message = "Login successful."
		});
	}

	private string GenerateJwtToken(string username)
	{
		JwtSecurityTokenHandler tokenHandler = new();
		byte[] key = Encoding.ASCII.GetBytes(_authConfig.JwtSecret);

		ClaimsIdentity claims = new(new[]
		{
			new Claim(ClaimTypes.Name, username),
			new Claim(ClaimTypes.NameIdentifier, username),
			new Claim("username", username)
		});

		SecurityTokenDescriptor tokenDescriptor = new()
		{
			Subject = claims,
			Expires = DateTime.UtcNow.AddHours(_authConfig.JwtExpirationHours),
			Issuer = _authConfig.JwtIssuer,
			Audience = _authConfig.JwtAudience,
			SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
		};

		SecurityToken token = tokenHandler.CreateToken(tokenDescriptor);
		return tokenHandler.WriteToken(token);
	}
}