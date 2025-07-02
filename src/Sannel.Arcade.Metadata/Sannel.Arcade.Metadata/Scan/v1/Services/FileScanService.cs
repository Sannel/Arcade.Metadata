using System.Text.Json;
using System.Text.RegularExpressions;
using MediatR;
using Sannel.Arcade.Metadata.Common.Settings;
using Sannel.Arcade.Metadata.Scan.v1.Clients;
using Sannel.Arcade.Metadata.Scan.v1.Clients.Models;

namespace Sannel.Arcade.Metadata.Scan.v1.Services;

public class FileScanService : IScanService
{
	private readonly IMetadataClient _metadataClient;
	private readonly IMediator _mediator;

	// Regex patterns for region detection
	private static readonly Regex RegionPattern = new(@"\s*\((USA|Europe|Japan|World|U|E|J|W)\)\s*", RegexOptions.IgnoreCase | RegexOptions.Compiled);

	public FileScanService(IMetadataClient metadataClient, IMediator mediator)
	{
		_metadataClient = metadataClient ?? throw new ArgumentNullException(nameof(metadataClient));
		_mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
	}

	public PlatformId GetPlatformID(string directoryName)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(directoryName);

		if(Enum.TryParse<PlatformId>(directoryName, true, out var platformId))
		{
			return platformId;
		}

		return PlatformId.None;
	}

	public async Task ScanAsync(CancellationToken cancellationToken)
	{
		await _metadataClient.AuthenticateAsync(cancellationToken);
		var romsDirectory = await _mediator.Send(new GetSettingRequest()
		{
			Key = "roms.root"
		}, cancellationToken);

		foreach(var dir in Directory.EnumerateDirectories(romsDirectory))
		{
			var name = Path.GetFileName(dir);

			var platformId = GetPlatformID(name);

			if(platformId != PlatformId.None)
			{
				await ScanFilesAsync(dir, platformId, cancellationToken);
			}
		}
	}

	private bool IsSupportedFileExtension(string ext, PlatformId id)
	{
		switch (id)
		{
			case PlatformId.NES:
				return ext.Equals(".nes", StringComparison.OrdinalIgnoreCase) ||
					ext.Equals(".zip", StringComparison.OrdinalIgnoreCase);

			default:
				return false;
		}
	}

	private (string cleanName, string? region) ExtractRegionFromFileName(string fileName)
	{
		var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
		var match = RegionPattern.Match(fileNameWithoutExtension);
		
		if (match.Success)
		{
			var region = match.Groups[1].Value.ToUpperInvariant();
			var cleanName = RegionPattern.Replace(fileNameWithoutExtension, "").Trim();
			return (cleanName, region);
		}

		return (fileNameWithoutExtension, null);
	}

	private static int CalculateLevenshteinDistance(string source, string target)
	{
		if (string.IsNullOrEmpty(source))
			return string.IsNullOrEmpty(target) ? 0 : target.Length;

		if (string.IsNullOrEmpty(target))
			return source.Length;

		var distance = new int[source.Length + 1, target.Length + 1];

		for (int i = 0; i <= source.Length; i++)
			distance[i, 0] = i;

		for (int j = 0; j <= target.Length; j++)
			distance[0, j] = j;

		for (int i = 1; i <= source.Length; i++)
		{
			for (int j = 1; j <= target.Length; j++)
			{
				var cost = target[j - 1] == source[i - 1] ? 0 : 1;
				distance[i, j] = Math.Min(
					Math.Min(distance[i - 1, j] + 1, distance[i, j - 1] + 1),
					distance[i - 1, j - 1] + cost);
			}
		}

		return distance[source.Length, target.Length];
	}

	private static double CalculateSimilarity(string source, string target)
	{
		if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(target))
			return 0.0;

		var maxLength = Math.Max(source.Length, target.Length);
		var distance = CalculateLevenshteinDistance(source.ToLowerInvariant(), target.ToLowerInvariant());
		return 1.0 - (double)distance / maxLength;
	}

	private async Task<GameInfo?> FindBestMatchAsync(string gameName, string? region, PlatformId platformId, CancellationToken cancellationToken)
	{
		var candidates = new List<GameInfo>();

		await foreach (var game in _metadataClient.FindGameAsync(gameName, platformId, cancellationToken))
		{
			candidates.Add(game);
		}

		if (candidates.Count == 0)
			return null;

		// Find the best match based on similarity
		var bestMatch = candidates
			.Select(game => new
			{
				Game = game,
				Similarity = CalculateSimilarity(gameName, game.Name)
			})
			.OrderByDescending(x => x.Similarity)
			.First();

		// Only return if similarity is reasonable (above 70%)
		return bestMatch.Game;
	}

	private async Task SaveGameInfoAsync(string romFilePath, GameInfo gameInfo, CancellationToken cancellationToken)
	{
		var directory = Path.GetDirectoryName(romFilePath);
		var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(romFilePath);
		var jsonFilePath = Path.Combine(directory!, $"{fileNameWithoutExtension}.json");

		var options = new JsonSerializerOptions
		{
			WriteIndented = true,
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase
		};

		var jsonContent = JsonSerializer.Serialize(gameInfo, options);
		await File.WriteAllTextAsync(jsonFilePath, jsonContent, cancellationToken);
	}

	private async Task ScanFilesAsync(string directory, PlatformId platformId,
		CancellationToken cancellationToken)
	{
		foreach (var file in Directory.EnumerateFiles(directory,"*.*", SearchOption.AllDirectories))
		{
			if (cancellationToken.IsCancellationRequested)
				return;
			var fileName = Path.GetFileName(file);
			var ext = Path.GetExtension(fileName);
			if(IsSupportedFileExtension(ext, platformId))
			{
				// Check if metadata JSON already exists
				var jsonFilePath = Path.Combine(Path.GetDirectoryName(file)!, $"{Path.GetFileNameWithoutExtension(file)}.json");
				if (File.Exists(jsonFilePath))
					continue; // Skip if metadata already exists

				// Extract region information from filename
				var (cleanName, region) = ExtractRegionFromFileName(fileName);

				// Find the best matching game from metadata client
				var matchedGame = await FindBestMatchAsync(cleanName, region, platformId, cancellationToken);

				// Save metadata if a match is found
				if (matchedGame is not null)
				{
					await SaveGameInfoAsync(file, matchedGame, cancellationToken);
				}
			}
		}
	}
}
