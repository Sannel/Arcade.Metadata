namespace Sannel.Arcade.Metadata.Metadata.v1.Models;

public class GameMetadata
{
	public string Id { get; set; } = string.Empty; // Add ID field
	public string Name { get; set; } = string.Empty;
	public string? Description { get; set; }
	public string Platform { get; set; } = string.Empty;
	public string MetadataProvider { get; set; } = string.Empty;
	public string? ProviderId { get; set; }
	public string? CoverUrl { get; set; }
	public List<string> ArtworkUrls { get; set; } = [];
	public List<string> ScreenShots { get; set; } = [];
	public List<string> VideoUrls { get; set; } = [];
	public List<string> Genres { get; set; } = [];
	public List<string> AlternateNames { get; set; } = [];
	public string? Region { get; set; }
	public string RomFilePath { get; set; } = string.Empty;
	public string RelativePath { get; set; } = string.Empty;
	public string? RomFileName { get; set; }
}