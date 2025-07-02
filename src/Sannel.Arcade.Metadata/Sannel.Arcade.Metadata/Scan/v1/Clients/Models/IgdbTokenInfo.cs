namespace Sannel.Arcade.Metadata.Scan.v1.Clients.Models;

public class IgdbTokenInfo
{
	public string AccessToken { get; set; } = string.Empty;
	public DateTimeOffset ExpiresAt { get; set; }
	public string TokenType { get; set; } = string.Empty;

	public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAt;
}