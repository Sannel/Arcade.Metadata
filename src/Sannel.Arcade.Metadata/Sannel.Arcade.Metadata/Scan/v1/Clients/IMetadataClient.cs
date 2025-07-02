using Sannel.Arcade.Metadata.Scan.v1.Clients.Models;

namespace Sannel.Arcade.Metadata.Scan.v1.Clients;

public interface IMetadataClient
{
	string ProviderName { get; }
	Task<bool> AuthenticateAsync(CancellationToken cancellationToken = default);

	IAsyncEnumerable<GameInfo> FindGameAsync(
		string name, 
		PlatformId platformId,
		CancellationToken cancellationToken = default);
}