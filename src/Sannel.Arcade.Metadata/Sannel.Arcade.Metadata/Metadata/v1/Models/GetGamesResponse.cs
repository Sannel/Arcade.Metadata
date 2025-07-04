namespace Sannel.Arcade.Metadata.Metadata.v1.Models;

public class GetGamesResponse
{
	public bool Success { get; set; }
	public string Message { get; set; } = string.Empty;
	public string PlatformName { get; set; } = string.Empty;
	public List<GameMetadata> Games { get; set; } = [];
}