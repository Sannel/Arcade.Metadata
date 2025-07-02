using System.Text.Json.Serialization;

namespace Sannel.Arcade.Metadata.Scan.v1.Clients.Models;

public class IgdbAuthResponse
{
	[JsonPropertyName("access_token")]
	public string AccessToken { get; set; } = string.Empty;

	[JsonPropertyName("expires_in")]
	public int ExpiresIn { get; set; }

	[JsonPropertyName("token_type")]
	public string TokenType { get; set; } = string.Empty;
}