using System.Text.Json;
using MediatR;
using Sannel.Arcade.Metadata.Common.Settings;
using Sannel.Arcade.Metadata.Metadata.v1.Models;
using Sannel.Arcade.Metadata.Scan.v1.Clients.Models;

namespace Sannel.Arcade.Metadata.Metadata.v1.Services;

public class MetadataService : IMetadataService
{
	private readonly IMediator _mediator;

	public MetadataService(IMediator mediator)
	{
		_mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
	}

	public async Task<GetPlatformsResponse> GetPlatformsAsync(CancellationToken cancellationToken = default)
	{
		try
		{
			var romsDirectory = await _mediator.Send(new GetSettingRequest()
			{
				Key = "roms.root"
			}, cancellationToken);

			if (string.IsNullOrEmpty(romsDirectory))
			{
				return new GetPlatformsResponse
				{
					Success = false,
					Message = "ROMs directory is not configured"
				};
			}

			if (!Directory.Exists(romsDirectory))
			{
				return new GetPlatformsResponse
				{
					Success = false,
					Message = "ROMs directory does not exist"
				};
			}

			var platforms = new List<PlatformInfo>();

			foreach (var platformDir in Directory.EnumerateDirectories(romsDirectory))
			{
				var platformName = Path.GetFileName(platformDir);
				
				if (Enum.TryParse<PlatformId>(platformName, true, out var platformId) && platformId != PlatformId.None)
				{
					var gameCount = await CountGamesInPlatformAsync(platformDir, platformId, cancellationToken);
					var coverImagePath = await GetPlatformCoverImageAsync(platformName, cancellationToken);
					
					// Use the new game image endpoint with PlatformId
					var coverImageUrl = !string.IsNullOrEmpty(coverImagePath) 
						? $"/api/v1/metadata/game-images/{platformId}/{coverImagePath}"
						: null;
					
					platforms.Add(new PlatformInfo
					{
						Name = platformName,
						DirectoryPath = platformDir,
						GameCount = gameCount,
						PlatformId = platformId,
						CoverImageUrl = coverImageUrl
					});
				}
			}

			return new GetPlatformsResponse
			{
				Success = true,
				Message = "Platforms retrieved successfully",
				Platforms = platforms.OrderBy(p => p.Name).ToList()
			};
		}
		catch (Exception ex)
		{
			return new GetPlatformsResponse
			{
				Success = false,
				Message = $"Error retrieving platforms: {ex.Message}"
			};
		}
	}

	public async Task<GetGamesResponse> GetGamesAsync(string platformName, CancellationToken cancellationToken = default)
	{
		try
		{
			var romsDirectory = await _mediator.Send(new GetSettingRequest()
			{
				Key = "roms.root"
			}, cancellationToken);

			if (string.IsNullOrEmpty(romsDirectory))
			{
				return new GetGamesResponse
				{
					Success = false,
					Message = "ROMs directory is not configured"
				};
			}

			var platformDirectory = Path.Combine(romsDirectory, platformName);
			
			if (!Directory.Exists(platformDirectory))
			{
				return new GetGamesResponse
				{
					Success = false,
					Message = $"Platform directory '{platformName}' does not exist"
				};
			}

			if (!Enum.TryParse<PlatformId>(platformName, true, out var platformId) || platformId == PlatformId.None)
			{
				return new GetGamesResponse
				{
					Success = false,
					Message = $"Unknown platform: {platformName}"
				};
			}

			var games = new List<GameMetadata>();
			var metadataDirectory = Path.Combine(platformDirectory, ".metadata");

			if (Directory.Exists(metadataDirectory))
			{
				await LoadGamesFromMetadataAsync(games, metadataDirectory, platformDirectory, platformName, cancellationToken);
			}

			return new GetGamesResponse
			{
				Success = true,
				Message = "Games retrieved successfully",
				PlatformName = platformName,
				Games = games.OrderBy(g => g.Name).ToList()
			};
		}
		catch (Exception ex)
		{
			return new GetGamesResponse
			{
				Success = false,
				Message = $"Error retrieving games: {ex.Message}",
				PlatformName = platformName
			};
		}
	}

	public async Task<string?> GetPlatformCoverImageAsync(string platformName, CancellationToken cancellationToken = default)
	{
		try
		{
			var romsDirectory = await _mediator.Send(new GetSettingRequest()
			{
				Key = "roms.root"
			}, cancellationToken);

			if (string.IsNullOrEmpty(romsDirectory))
			{
				return null;
			}

			var platformDirectory = Path.Combine(romsDirectory, platformName);
			if (!Directory.Exists(platformDirectory))
			{
				return null;
			}

			var metadataDirectory = Path.Combine(platformDirectory, ".metadata");
			if (!Directory.Exists(metadataDirectory))
			{
				return null;
			}

			// Look for the first available cover image in any game's metadata
			foreach (var jsonFile in Directory.EnumerateFiles(metadataDirectory, "*.json", SearchOption.AllDirectories))
			{
				if (cancellationToken.IsCancellationRequested)
					break;

				try
				{
					var jsonContent = await File.ReadAllTextAsync(jsonFile, cancellationToken);
					using var document = JsonDocument.Parse(jsonContent);
					var root = document.RootElement;

					if (root.TryGetProperty("coverUrl", out var coverElement) && 
						coverElement.ValueKind == JsonValueKind.String &&
						!string.IsNullOrEmpty(coverElement.GetString()))
					{
						var coverPath = coverElement.GetString();
						if (!string.IsNullOrEmpty(coverPath))
						{
							// Get the directory of the JSON file to resolve relative image path
							var jsonDirectory = Path.GetDirectoryName(jsonFile) ?? string.Empty;
							var fullImagePath = Path.Combine(jsonDirectory, coverPath);
							
							if (File.Exists(fullImagePath))
							{
								// Return the relative path from platform directory for API consumption
								var relativePath = Path.GetRelativePath(metadataDirectory, fullImagePath);
								return relativePath.Replace('\\', '/'); // Ensure forward slashes for URLs
							}
						}
					}
				}
				catch (Exception)
				{
					// Continue to next file if this one fails to parse
					continue;
				}
			}

			return null;
		}
		catch (Exception)
		{
			return null;
		}
	}

	private async Task<int> CountGamesInPlatformAsync(string platformDirectory, PlatformId platformId, CancellationToken cancellationToken)
	{
		var count = 0;
		var metadataDirectory = Path.Combine(platformDirectory, ".metadata");

		if (Directory.Exists(metadataDirectory))
		{
			foreach (var jsonFile in Directory.EnumerateFiles(metadataDirectory, "*.json", SearchOption.AllDirectories))
			{
				if (cancellationToken.IsCancellationRequested)
					break;
				count++;
			}
		}

		return count;
	}

	private async Task LoadGamesFromMetadataAsync(List<GameMetadata> games, string metadataDirectory, string platformDirectory, string platformName, CancellationToken cancellationToken)
	{
		// Parse the platform name to get PlatformId for constructing image URLs
		if (!Enum.TryParse<PlatformId>(platformName, true, out var platformId) || platformId == PlatformId.None)
		{
			// If we can't parse the platform ID, we'll fall back to relative paths
			platformId = PlatformId.None;
		}

		foreach (var jsonFile in Directory.EnumerateFiles(metadataDirectory, "*.json", SearchOption.AllDirectories))
		{
			if (cancellationToken.IsCancellationRequested)
				break;

			try
			{
				var jsonContent = await File.ReadAllTextAsync(jsonFile, cancellationToken);
				
				var options = new JsonSerializerOptions
				{
					PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
					PropertyNameCaseInsensitive = true
				};

				// Parse the JSON as a JsonDocument first to handle the structure flexibly
				using var document = JsonDocument.Parse(jsonContent);
				var root = document.RootElement;

				var gameMetadata = new GameMetadata
				{
					Name = root.TryGetProperty("name", out var nameElement) ? nameElement.GetString() ?? "Unknown" : "Unknown",
					Description = root.TryGetProperty("description", out var descElement) ? descElement.GetString() : null,
					Platform = platformName,
					MetadataProvider = root.TryGetProperty("metadataProvider", out var providerElement) ? providerElement.GetString() ?? "Unknown" : "Unknown",
					ProviderId = root.TryGetProperty("providerId", out var idElement) ? idElement.GetString() : null,
					Region = root.TryGetProperty("region", out var regionElement) ? regionElement.GetString() : null
				};

				// Handle cover URL - convert relative path to full API URL
				if (root.TryGetProperty("coverUrl", out var coverElement) && !string.IsNullOrEmpty(coverElement.GetString()))
				{
					var coverPath = coverElement.GetString();
					if (!string.IsNullOrEmpty(coverPath) && platformId != PlatformId.None)
					{
						gameMetadata.CoverUrl = $"/api/v1/metadata/game-images/{platformId}/{coverPath}";
					}
					else
					{
						gameMetadata.CoverUrl = coverPath;
					}
				}

				// Handle artwork URLs array - convert relative paths to full API URLs
				if (root.TryGetProperty("artworkUrls", out var artworkElement) && artworkElement.ValueKind == JsonValueKind.Array)
				{
					gameMetadata.ArtworkUrls = artworkElement.EnumerateArray()
						.Where(e => e.ValueKind == JsonValueKind.String && !string.IsNullOrEmpty(e.GetString()))
						.Select(e => 
						{
							var imagePath = e.GetString()!;
							return platformId != PlatformId.None 
								? $"/api/v1/metadata/game-images/{platformId}/{imagePath}"
								: imagePath;
						})
						.ToList();
				}

				// Handle screenshots array - convert relative paths to full API URLs
				if (root.TryGetProperty("screenShots", out var screenshotsElement) && screenshotsElement.ValueKind == JsonValueKind.Array)
				{
					gameMetadata.ScreenShots = screenshotsElement.EnumerateArray()
						.Where(e => e.ValueKind == JsonValueKind.String && !string.IsNullOrEmpty(e.GetString()))
						.Select(e => 
						{
							var imagePath = e.GetString()!;
							return platformId != PlatformId.None 
								? $"/api/v1/metadata/game-images/{platformId}/{imagePath}"
								: imagePath;
						})
						.ToList();
				}

				// Handle genres array
				if (root.TryGetProperty("genres", out var genresElement) && genresElement.ValueKind == JsonValueKind.Array)
				{
					gameMetadata.Genres = genresElement.EnumerateArray()
						.Where(e => e.ValueKind == JsonValueKind.String && !string.IsNullOrEmpty(e.GetString()))
						.Select(e => e.GetString()!)
						.ToList();
				}

				// Calculate relative path and ROM file path
				var relativePath = Path.GetRelativePath(metadataDirectory, jsonFile);
				var relativeDir = Path.GetDirectoryName(relativePath) ?? string.Empty;
				var fileNameWithoutExt = Path.GetFileNameWithoutExtension(jsonFile);
				
				gameMetadata.RelativePath = relativePath;
				
				// Try to find the corresponding ROM file
				var romDirectory = Path.Combine(platformDirectory, relativeDir);
				var possibleRomFiles = Directory.GetFiles(romDirectory, $"{fileNameWithoutExt}.*")
					.Where(f => !Path.GetExtension(f).Equals(".json", StringComparison.OrdinalIgnoreCase))
					.ToList();

				if (possibleRomFiles.Count > 0)
				{
					gameMetadata.RomFilePath = possibleRomFiles.First();
				}

				games.Add(gameMetadata);
			}
			catch (Exception ex)
			{
				// Log error but continue processing other files
				Console.WriteLine($"Error parsing metadata file {jsonFile}: {ex.Message}");
			}
		}
	}
}