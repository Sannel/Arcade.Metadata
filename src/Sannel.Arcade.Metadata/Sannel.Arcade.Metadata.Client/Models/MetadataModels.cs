namespace Sannel.Arcade.Metadata.Client.Models;

public class PlatformInfo
{
	public string Name { get; set; } = string.Empty;
	public string DirectoryPath { get; set; } = string.Empty;
	public int GameCount { get; set; }
	public int PlatformId { get; set; }
	public string? CoverImageUrl { get; set; }
}

public class GameMetadata
{
	public string Id { get; set; } = string.Empty; // Add ID field
	public string Name { get; set; } = string.Empty;
	public string? Description { get; set; }
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

public class GetPlatformsResponse
{
	public bool Success { get; set; }
	public string Message { get; set; } = string.Empty;
	public List<PlatformInfo> Platforms { get; set; } = [];
}

public class GetGamesResponse
{
	public bool Success { get; set; }
	public string Message { get; set; } = string.Empty;
	public string PlatformName { get; set; } = string.Empty;
	public List<GameMetadata> Games { get; set; } = [];
}