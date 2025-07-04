using System.Runtime.CompilerServices;
using System.Text.Json;

using IGDB;
using IGDB.Models;

using MediatR;

using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

using Sannel.Arcade.Metadata.Common.Settings;
using Sannel.Arcade.Metadata.Scan.v1.Clients.Models;

namespace Sannel.Arcade.Metadata.Scan.v1.Clients;

public class IgdbClient : IMetadataClient, IDisposable
{
	private readonly IMediator _mediator;
	private readonly SemaphoreSlim _authSemaphore = new(1,1);
	private IGDBClient? _client;

	public string ProviderName => nameof(IgdbClient);

	public IgdbClient(IMediator mediator)
	{
		_mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
	}

	public async Task<bool> AuthenticateAsync(CancellationToken cancellationToken = default)
	{
		if(_client is not null)
		{
			return true;
		}
		await _authSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
		try
		{
			var clientId = await _mediator.Send(new GetSettingRequest()
			{
				Key = "igdb.clientId"
			}, cancellationToken).ConfigureAwait(false);
			var clientSecret = await _mediator.Send(new GetSettingRequest()
			{
				Key = "igdb.clientSecret"
			}, cancellationToken).ConfigureAwait(false);

			_client = IGDBClient.CreateWithDefaults(clientId, clientSecret);
			return true;
		}
		finally
		{
			_authSemaphore.Release();
		}

	}

	protected virtual void Dispose(bool disposing)
	{
		if (disposing)
		{
			_authSemaphore?.Dispose();
		}
	}

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	private IGDBClient GetClient()
	{
		if (_client is null)
		{
			throw new InvalidOperationException("Client has not been created. Call AuthenticateAsync first.");
		}

		return _client;
	}

	public async IAsyncEnumerable<GameInfo> FindGameAsync(string name, PlatformId platformId, [EnumeratorCancellation]CancellationToken cancellationToken = default)
	{
		var client = GetClient();

		var games = await client.QueryAsync<Game>(IGDBClient.Endpoints.Games,
			$"fields name,summary,cover.url,artworks.url,alternative_names.name,url,screenshots.url,genres.name; search \"{name}\"; where platforms = [{(int)platformId}];");

		foreach (var game in games)
		{
			var info = new GameInfo
			{
				MetadataProvider = ProviderName,
				ProviderId = game.Id.ToString(),
				Name = game.Name,
				Description = game.Summary,
				Platform = platformId,
				CoverUrl = game.Cover?.Value?.Url,
				ArtworkUrls = game.Artworks?.Values?.Select(a => a?.Url)?.ToList()?? [],
				ScreenShots = game.Screenshots?.Values?.Select(s => s?.Url)?.ToList()?? [],
				Genres = game.Genres?.Values?.Select(g => g?.Name)?.ToList() ?? [],
				AlternateNames = game.AlternativeNames?.Values?.Select(an => an?.Name)?.Where(n => !string.IsNullOrEmpty(n))?.ToList() ?? []
			};

			yield return info;
		}
	}
}