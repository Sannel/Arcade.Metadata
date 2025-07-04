namespace Sannel.Arcade.Metadata.Metadata.v1.Models;

public class GetPlatformsResponse
{
	public bool Success { get; set; }
	public string Message { get; set; } = string.Empty;
	public List<PlatformInfo> Platforms { get; set; } = [];
}