namespace Sannel.Arcade.Metadata.Scan.v1.Clients.Models;

public class GameInfo
{
	public string Name { get; set; } = string.Empty;
	public string? Description { get; set; }

	public PlatformId Platform { get; set; } = PlatformId.None;
	public required string MetadataProvider { get; init; }
	public string? ProviderId { get; internal set; }
	public string? CoverUrl { get; internal set; }
	public List<string?> ArtworkUrls { get; internal set; } = [];
	public List<string?> ScreenShots { get; internal set; } = [];
	public List<string?> Genres { get; internal set; } = [];
	public List<string> AlternateNames { get; internal set; } = [];
}
