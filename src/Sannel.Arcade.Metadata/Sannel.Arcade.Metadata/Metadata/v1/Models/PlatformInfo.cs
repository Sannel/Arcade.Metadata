using Sannel.Arcade.Metadata.Scan.v1.Clients.Models;

namespace Sannel.Arcade.Metadata.Metadata.v1.Models;

public class PlatformInfo
{
	public string Name { get; set; } = string.Empty;
	public string DirectoryPath { get; set; } = string.Empty;
	public int GameCount { get; set; }
	public PlatformId PlatformId { get; set; }
	public string? CoverImageUrl { get; set; }
}