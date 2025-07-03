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
	private readonly HttpClient _httpClient;

	// Regex patterns for region detection
	private static readonly Regex RegionPattern = new(@"\s*\((USA|Europe|Japan|World|U|E|J|W)\)\s*", RegexOptions.IgnoreCase | RegexOptions.Compiled);
	
	// Regex pattern for IGDB image URLs
	private static readonly Regex IgdbImagePattern = new(@"https://images\.igdb\.com/igdb/image/upload/t_([^/]+)/([^/.]+)\.jpg", RegexOptions.IgnoreCase | RegexOptions.Compiled);

	public FileScanService(IMetadataClient metadataClient, IMediator mediator, HttpClient httpClient)
	{
		_metadataClient = metadataClient ?? throw new ArgumentNullException(nameof(metadataClient));
		_mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
		_httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
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

		if (string.IsNullOrEmpty(romsDirectory))
		{
			return; // No ROMs directory configured
		}

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
		
		string? region = null;
		string cleanName = fileNameWithoutExtension;
		
		if (match.Success)
		{
			region = match.Groups[1].Value.ToUpperInvariant();
			cleanName = RegionPattern.Replace(fileNameWithoutExtension, "").Trim();
		}

		// Remove trailing I or 1 characters from the clean name
		cleanName = RemoveTrailingSequenceCharacters(cleanName);

		return (cleanName, region);
	}

	private static string RemoveTrailingSequenceCharacters(string name)
	{
		if (string.IsNullOrEmpty(name))
			return name;

		// Remove trailing I or 1 characters (common in ROM naming for sequels)
		while (name.Length > 0 && (name[^1] == 'I' || name[^1] == 'i' || name[^1] == '1'))
		{
			name = name[..^1].Trim();
		}

		return name;
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

		 // Calculate similarity for all candidates
		var scoredCandidates = candidates
			.Select(game => new
			{
				Game = game,
				Similarity = CalculateSimilarity(gameName, game.Name),
				IsExactMatch = string.Equals(gameName, game.Name, StringComparison.OrdinalIgnoreCase),
				IsBaseVersion = IsBaseVersionOfGame(gameName, game.Name)
			})
			.ToList();

		// First, try to find an exact match
		var exactMatch = scoredCandidates.FirstOrDefault(x => x.IsExactMatch);
		if (exactMatch != null)
			return exactMatch.Game;

		// If no exact match, look for base version matches (e.g., "Adventure Island" when searching for stripped "Adventure Island")
		var baseVersionMatches = scoredCandidates
			.Where(x => x.IsBaseVersion && x.Similarity >= 0.8)
			.OrderByDescending(x => x.Similarity)
			.ToList();

		if (baseVersionMatches.Count > 0)
		{
			// Prefer games without version indicators in the title
			var preferredBaseMatch = baseVersionMatches
				.FirstOrDefault(x => !ContainsVersionIndicators(x.Game.Name));
			
			if (preferredBaseMatch != null)
				return preferredBaseMatch.Game;
			
			// If all have version indicators, return the one with highest similarity
			return baseVersionMatches.First().Game;
		}

		// Fall back to highest similarity match if above 70%
		var bestMatch = scoredCandidates
			.OrderByDescending(x => x.Similarity)
			.First();

		return bestMatch.Similarity >= 0.7 ? bestMatch.Game : null;
	}

	private static bool IsBaseVersionOfGame(string searchName, string gameName)
	{
		// Check if the game name starts with the search name and might be a base version
		var searchLower = searchName.ToLowerInvariant().Trim();
		var gameLower = gameName.ToLowerInvariant().Trim();

		// Exact match (case insensitive)
		if (searchLower == gameLower)
			return true;

		// Check if game name starts with search name followed by common version patterns
		if (gameLower.StartsWith(searchLower, StringComparison.Ordinal))
		{
			var remainder = gameLower[searchLower.Length..].Trim();
			
			// If remainder is empty or just whitespace, it's the base version
			if (string.IsNullOrWhiteSpace(remainder))
				return true;

			// Check for common version separators and patterns
			if (remainder.StartsWith(":", StringComparison.Ordinal) ||
				remainder.StartsWith("-", StringComparison.Ordinal) ||
				remainder.StartsWith(".", StringComparison.Ordinal))
			{
				return true;
			}
		}

		return false;
	}

	private static bool ContainsVersionIndicators(string gameName)
	{
		var nameLower = gameName.ToLowerInvariant();
		
		// Check for common version indicators
		var versionPatterns = new[]
		{
			@"\b(ii|iii|iv|v|vi|vii|viii|ix|x)\b", // Roman numerals
			@"\b[2-9]\b", // Numbers 2-9
			@"\b(two|three|four|five|six|seven|eight|nine|ten)\b", // Written numbers
			@"\b(sequel|part)\s+[2-9]\b", // "Sequel 2", "Part 3", etc.
			@":\s*(ii|iii|iv|v|vi|vii|viii|ix|x|[2-9])\b" // Colon followed by version
		};

		foreach (var pattern in versionPatterns)
		{
			if (Regex.IsMatch(nameLower, pattern, RegexOptions.IgnoreCase))
				return true;
		}

		return false;
	}

	private string GetMetadataFilePath(string romFilePath, string platformDirectory)
	{
		var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(romFilePath);
		var metadataDirectory = Path.Combine(platformDirectory, ".metadata");
		
		// Create the .metadata directory if it doesn't exist
		Directory.CreateDirectory(metadataDirectory);
		
		// Get the relative path from platform directory to ROM file
		var relativePath = Path.GetRelativePath(platformDirectory, romFilePath);
		var relativeDirectory = Path.GetDirectoryName(relativePath) ?? string.Empty;
		
		// Create the corresponding subdirectory structure in .metadata
		var metadataSubDirectory = Path.Combine(metadataDirectory, relativeDirectory);
		Directory.CreateDirectory(metadataSubDirectory);
		
		return Path.Combine(metadataSubDirectory, $"{fileNameWithoutExtension}.json");
	}

	private string GetImagesDirectory(string romFilePath, string platformDirectory)
	{
		var metadataDirectory = Path.Combine(platformDirectory, ".metadata");
		
		// Get the relative path from platform directory to ROM file
		var relativePath = Path.GetRelativePath(platformDirectory, romFilePath);
		var relativeDirectory = Path.GetDirectoryName(relativePath) ?? string.Empty;
		
		// Create images directory structure
		var imagesDirectory = Path.Combine(metadataDirectory, relativeDirectory, "images");
		Directory.CreateDirectory(imagesDirectory);
		
		return imagesDirectory;
	}

	private static string? FixImageUrl(string? imageUrl)
	{
		if (string.IsNullOrEmpty(imageUrl))
			return null;

		// Check if URL starts with "//" (missing schema)
		if (imageUrl.StartsWith("//", StringComparison.Ordinal))
		{
			return $"https:{imageUrl}";
		}

		// Return as-is if it already has a schema or is invalid
		return imageUrl;
	}

	private static string? UpgradeIgdbImageToLargestSize(string? imageUrl, string imageType)
	{
		if (string.IsNullOrEmpty(imageUrl))
			return null;

		// Fix the URL schema first
		var fixedUrl = FixImageUrl(imageUrl);
		if (string.IsNullOrEmpty(fixedUrl))
			return null;

		// Check if this is an IGDB image URL
		var match = IgdbImagePattern.Match(fixedUrl);
		if (!match.Success)
			return fixedUrl; // Not an IGDB URL, return as-is

		var currentSize = match.Groups[1].Value;
		var hash = match.Groups[2].Value;

		// Determine the best size based on image type
		string targetSize = imageType.ToLowerInvariant() switch
		{
			"cover" => "1080p", // 1920 x 1080 - largest available for covers
			"screenshot" => "screenshot_huge", // 1280 x 720 - largest for screenshots
			"artwork" => "1080p", // 1920 x 1080 - largest for artwork
			_ => "1080p" // Default to largest
		};

		// Construct the upgraded URL
		return $"https://images.igdb.com/igdb/image/upload/t_{targetSize}/{hash}.jpg";
	}

	private static List<string?> UpgradeIgdbImageUrls(List<string?> imageUrls, string imageType)
	{
		return imageUrls.Select(url => UpgradeIgdbImageToLargestSize(url, imageType)).ToList();
	}

	private async Task<string?> DownloadImageWithFallbackAsync(string? imageUrl, string imagesDirectory, string filePrefix, string imageType, CancellationToken cancellationToken)
	{
		if (string.IsNullOrEmpty(imageUrl))
			return null;

		// Try the upgraded/largest size first
		var upgradedUrl = UpgradeIgdbImageToLargestSize(imageUrl, imageType);
		var result = await DownloadImageAsync(upgradedUrl, imagesDirectory, filePrefix, cancellationToken);
		
		// If that fails, try the original URL
		if (result == null && !string.Equals(upgradedUrl, imageUrl, StringComparison.OrdinalIgnoreCase))
		{
			result = await DownloadImageAsync(imageUrl, imagesDirectory, filePrefix, cancellationToken);
		}

		return result;
	}

	private async Task<string?> DownloadImageAsync(string? imageUrl, string imagesDirectory, string filePrefix, CancellationToken cancellationToken)
	{
		if (string.IsNullOrEmpty(imageUrl))
			return null;

		// Fix the URL by adding https: schema if missing
		var fixedUrl = FixImageUrl(imageUrl);
		if (string.IsNullOrEmpty(fixedUrl))
			return null;

		try
		{
			// Get the file extension from the URL
			var uri = new Uri(fixedUrl);
			var extension = Path.GetExtension(uri.LocalPath);
			if (string.IsNullOrEmpty(extension))
			{
				// Try to determine from content type when downloading
				extension = ".jpg"; // Default fallback
			}

			// Generate a filename
			var fileName = $"{filePrefix}{extension}";
			var filePath = Path.Combine(imagesDirectory, fileName);

			// Skip if file already exists
			if (File.Exists(filePath))
				return $"images/{fileName}";

			// Download the image
			using var response = await _httpClient.GetAsync(fixedUrl, cancellationToken);
			if (response.IsSuccessStatusCode)
			{
				// Try to get better extension from content type
				if (response.Content.Headers.ContentType?.MediaType is string contentType)
				{
					extension = contentType switch
					{
						"image/jpeg" => ".jpg",
						"image/png" => ".png",
						"image/gif" => ".gif",
						"image/webp" => ".webp",
						_ => extension
					};
					fileName = $"{filePrefix}{extension}";
					filePath = Path.Combine(imagesDirectory, fileName);
				}

				// Save the image
				using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
				await response.Content.CopyToAsync(fileStream, cancellationToken);

				// Return relative path from JSON file location
				return $"images/{fileName}";
			}
		}
		catch (Exception)
		{
			// Log error if needed, but don't fail the entire process for one image
		}

		return null;
	}

	private async Task<List<string>> DownloadImagesWithFallbackAsync(List<string?> imageUrls, string imagesDirectory, string filePrefix, string imageType, CancellationToken cancellationToken)
	{
		var relativePaths = new List<string>();
		
		for (int i = 0; i < imageUrls.Count; i++)
		{
			var imageUrl = imageUrls[i];
			if (!string.IsNullOrEmpty(imageUrl))
			{
				var relativePath = await DownloadImageWithFallbackAsync(imageUrl, imagesDirectory, $"{filePrefix}_{i + 1}", imageType, cancellationToken);
				if (relativePath != null)
				{
					relativePaths.Add(relativePath);
				}
			}
		}

		return relativePaths;
	}

	private async Task SaveGameInfoAsync(string romFilePath, string platformDirectory, GameInfo gameInfo, string? region, CancellationToken cancellationToken)
	{
		var jsonFilePath = GetMetadataFilePath(romFilePath, platformDirectory);
		var imagesDirectory = GetImagesDirectory(romFilePath, platformDirectory);
		var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(romFilePath);

		// Download images with fallback to original size if largest size fails
		var coverImagePath = await DownloadImageWithFallbackAsync(gameInfo.CoverUrl, imagesDirectory, $"{fileNameWithoutExtension}_cover", "cover", cancellationToken);
		var artworkPaths = await DownloadImagesWithFallbackAsync(gameInfo.ArtworkUrls, imagesDirectory, $"{fileNameWithoutExtension}_artwork", "artwork", cancellationToken);
		var screenshotPaths = await DownloadImagesWithFallbackAsync(gameInfo.ScreenShots, imagesDirectory, $"{fileNameWithoutExtension}_screenshot", "screenshot", cancellationToken);

		// Create an anonymous object that includes the original GameInfo plus the region and local image paths
		var gameMetadata = new
		{
			gameInfo.Name,
			gameInfo.Description,
			gameInfo.Platform,
			gameInfo.MetadataProvider,
			gameInfo.ProviderId,
			CoverUrl = coverImagePath, // Use local path instead of remote URL
			ArtworkUrls = artworkPaths, // Use local paths instead of remote URLs
			ScreenShots = screenshotPaths, // Use local paths instead of remote URLs
			gameInfo.Genres,
			Region = region // Add the region field
		};

		var options = new JsonSerializerOptions
		{
			WriteIndented = true,
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase
		};

		var jsonContent = JsonSerializer.Serialize(gameMetadata, options);
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
				// Check if metadata JSON already exists in .metadata folder
				var jsonFilePath = GetMetadataFilePath(file, directory);
				if (File.Exists(jsonFilePath))
					continue; // Skip if metadata already exists

				// Extract region information from filename
				var (cleanName, region) = ExtractRegionFromFileName(fileName);

				// Find the best matching game from metadata client
				var matchedGame = await FindBestMatchAsync(cleanName, region, platformId, cancellationToken);

				 // If no match found, create a simple metadata file based on the filename
				if (matchedGame is null)
				{
					matchedGame = CreateFallbackGameInfo(cleanName, platformId);
				}

				// Save metadata
				await SaveGameInfoAsync(file, directory, matchedGame, region, cancellationToken);
			}
		}
	}

	private static GameInfo CreateFallbackGameInfo(string gameName, PlatformId platformId)
	{
		return new GameInfo
		{
			Name = gameName,
			Description = null, // No description available
			Platform = platformId,
			MetadataProvider = "Local", // Indicate this is locally generated
			ProviderId = null, // No provider ID
			CoverUrl = null, // No cover image
			ArtworkUrls = new List<string?>(), // No artwork
			ScreenShots = new List<string?>(), // No screenshots
			Genres = new List<string>() // No genre information
		};
	}
}
