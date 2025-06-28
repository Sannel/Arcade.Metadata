namespace Sannel.Arcade.Metadata.Models;

public class AuthenticationConfig
{
	public string Username { get; set; } = string.Empty;
	public string Password { get; set; } = string.Empty;
	public string JwtSecret { get; set; } = string.Empty;
	public string JwtIssuer { get; set; } = string.Empty;
	public string JwtAudience { get; set; } = string.Empty;
	public int JwtExpirationHours { get; set; } = 24;
}