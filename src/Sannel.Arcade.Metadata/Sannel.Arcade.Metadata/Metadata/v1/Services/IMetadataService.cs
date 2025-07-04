using Sannel.Arcade.Metadata.Metadata.v1.Models;

namespace Sannel.Arcade.Metadata.Metadata.v1.Services;

public interface IMetadataService
{
	Task<GetPlatformsResponse> GetPlatformsAsync(CancellationToken cancellationToken = default);
	Task<GetGamesResponse> GetGamesAsync(string platformName, CancellationToken cancellationToken = default);
	Task<string?> GetPlatformCoverImageAsync(string platformName, CancellationToken cancellationToken = default);
}