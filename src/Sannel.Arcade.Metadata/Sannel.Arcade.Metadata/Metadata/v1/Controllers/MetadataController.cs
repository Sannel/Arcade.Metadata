using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sannel.Arcade.Metadata.Metadata.v1.Models;
using Sannel.Arcade.Metadata.Metadata.v1.Services;
using MediatR;
using Sannel.Arcade.Metadata.Common.Settings;
using Sannel.Arcade.Metadata.Scan.v1.Clients.Models;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Sannel.Arcade.Metadata.Metadata.v1.Controllers;

/// <summary>
/// Controller for managing metadata operations.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class MetadataController : ControllerBase
{
	private readonly IMetadataService _metadataService;
	private readonly IMediator _mediator;

	/// <summary>
	/// Initializes a new instance of the <see cref="MetadataController"/> class.
	/// </summary>
	/// <param name="metadataService">The metadata service.</param>
	/// <param name="mediator">The mediator.</param>
	public MetadataController(IMetadataService metadataService, IMediator mediator)
	{
		_metadataService = metadataService ?? throw new ArgumentNullException(nameof(metadataService));
		_mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
	}

	/// <summary>
	/// Gets all available platforms from the ROMs directory.
	/// </summary>
	/// <returns>List of platforms with game counts.</returns>
	[HttpGet("platforms")]
	public async Task<ActionResult<GetPlatformsResponse>> GetPlatforms(CancellationToken cancellationToken = default)
	{
		var result = await _metadataService.GetPlatformsAsync(cancellationToken);
		
		if (!result.Success)
		{
			return BadRequest(result);
		}

		return Ok(result);
	}

	/// <summary>
	/// Gets all games for a specific platform.
	/// </summary>
	/// <param name="platformName">The name of the platform.</param>
	/// <returns>List of games for the specified platform.</returns>
	[HttpGet("platforms/{platformName}/games")]
	public async Task<ActionResult<GetGamesResponse>> GetGames(string platformName, CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrWhiteSpace(platformName))
		{
			return BadRequest(new GetGamesResponse
			{
				Success = false,
				Message = "Platform name is required"
			});
		}

		var result = await _metadataService.GetGamesAsync(platformName, cancellationToken);
		
		if (!result.Success)
		{
			return BadRequest(result);
		}

		return Ok(result);
	}

	/// <summary>
	/// Gets a specific game's metadata by ID.
	/// </summary>
	/// <param name="platformName">The name of the platform.</param>
	/// <param name="gameId">The ID of the game.</param>
	/// <returns>The game metadata.</returns>
	[HttpGet("platforms/{platformName}/games/{gameId}")]
	public async Task<ActionResult<GameMetadata>> GetGame(string platformName, string gameId, CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrWhiteSpace(platformName) || string.IsNullOrWhiteSpace(gameId))
		{
			return BadRequest("Platform name and game ID are required");
		}

		try
		{
			var romsDirectory = await _mediator.Send(new GetSettingRequest()
			{
				Key = "roms.root"
			}, cancellationToken);

			if (string.IsNullOrEmpty(romsDirectory))
			{
				return BadRequest("ROMs directory is not configured");
			}

			var platformDirectory = Path.Combine(romsDirectory, platformName);
			if (!Directory.Exists(platformDirectory))
			{
				return NotFound("Platform directory not found");
			}

			var metadataDirectory = Path.Combine(platformDirectory, ".metadata");
			var gameMetadataPath = Path.Combine(metadataDirectory, $"{gameId}.json");

			if (!System.IO.File.Exists(gameMetadataPath))
			{
				return NotFound("Game metadata not found");
			}

			var jsonContent = await System.IO.File.ReadAllTextAsync(gameMetadataPath, cancellationToken);
			var gameMetadata = JsonSerializer.Deserialize<GameMetadata>(jsonContent, new JsonSerializerOptions
			{
				PropertyNamingPolicy = JsonNamingPolicy.CamelCase
			});

			return Ok(gameMetadata);
		}
		catch (Exception ex)
		{
			return BadRequest($"Error retrieving game metadata: {ex.Message}");
		}
	}

	/// <summary>
	/// Updates a specific game's metadata by ID.
	/// </summary>
	/// <param name="platformName">The name of the platform.</param>
	/// <param name="gameId">The ID of the game.</param>
	/// <param name="gameMetadata">The updated game metadata.</param>
	/// <returns>Success or error response.</returns>
	[HttpPut("platforms/{platformName}/games/{gameId}/metadata")]
	public async Task<IActionResult> UpdateGameMetadata(string platformName, string gameId, [FromBody] GameMetadata gameMetadata, CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrWhiteSpace(platformName) || string.IsNullOrWhiteSpace(gameId))
		{
			return BadRequest("Platform name and game ID are required");
		}

		if (gameMetadata == null)
		{
			return BadRequest("Game metadata is required");
		}

		try
		{
			var romsDirectory = await _mediator.Send(new GetSettingRequest()
			{
				Key = "roms.root"
			}, cancellationToken);

			if (string.IsNullOrEmpty(romsDirectory))
			{
				return BadRequest("ROMs directory is not configured");
			}

			var platformDirectory = Path.Combine(romsDirectory, platformName);
			if (!Directory.Exists(platformDirectory))
			{
				return NotFound("Platform directory not found");
			}

			var metadataDirectory = Path.Combine(platformDirectory, ".metadata");
			if (!Directory.Exists(metadataDirectory))
			{
				return NotFound("Metadata directory not found");
			}

			var gameMetadataPath = Path.Combine(metadataDirectory, $"{gameId}.json");

			if (!System.IO.File.Exists(gameMetadataPath))
			{
				return NotFound("Game metadata not found");
			}

			// Security check: ensure the resolved path is within the expected directory structure
			var resolvedPath = Path.GetFullPath(gameMetadataPath);
			var resolvedExpectedDirectory = Path.GetFullPath(metadataDirectory);
			
			if (!resolvedPath.StartsWith(resolvedExpectedDirectory, StringComparison.OrdinalIgnoreCase))
			{
				return BadRequest("Invalid metadata file path");
			}

			// Ensure the game ID matches what's in the URL
			gameMetadata.Id = gameId;

			// Serialize the updated metadata with proper formatting
			var jsonOptions = new JsonSerializerOptions
			{
				PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
				WriteIndented = true,
				DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
			};

			var updatedJsonContent = JsonSerializer.Serialize(gameMetadata, jsonOptions);

			// Write the updated metadata to file
			await System.IO.File.WriteAllTextAsync(gameMetadataPath, updatedJsonContent, cancellationToken);

			// Update the games.json file after successful metadata update
			await UpdatePlatformGamesListAsync(platformDirectory, cancellationToken);

			return Ok(new { Success = true, Message = "Game metadata updated successfully" });
		}
		catch (Exception ex)
		{
			return BadRequest($"Error updating game metadata: {ex.Message}");
		}
	}

	/// <summary>
	/// Updates the games.json file for a platform by reading all individual game metadata files.
	/// </summary>
	/// <param name="platformDirectory">The platform directory path.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Task representing the async operation.</returns>
	private async Task UpdatePlatformGamesListAsync(string platformDirectory, CancellationToken cancellationToken)
	{
		var metadataDirectory = Path.Combine(platformDirectory, ".metadata");
		var gamesListPath = Path.Combine(metadataDirectory, "games.json");
		
		var gamesList = new List<object>();

		if (Directory.Exists(metadataDirectory))
		{
			// Look for JSON files directly in the .metadata directory
			foreach (var jsonFile in Directory.EnumerateFiles(metadataDirectory, "*.json"))
			{
				if (cancellationToken.IsCancellationRequested)
					break;

				// Skip the games.json file itself
				if (Path.GetFileName(jsonFile).Equals("games.json", StringComparison.OrdinalIgnoreCase))
					continue;

				try
				{
					var jsonContent = await System.IO.File.ReadAllTextAsync(jsonFile, cancellationToken);
					using var document = JsonDocument.Parse(jsonContent);
					var root = document.RootElement;

					// Extract basic game information
					var gameEntry = new
					{
						Id = root.TryGetProperty("id", out var idElement) ? idElement.GetString() ?? "unknown" : "unknown",
						Name = root.TryGetProperty("name", out var nameElement) ? nameElement.GetString() ?? "Unknown" : "Unknown",
						Description = root.TryGetProperty("description", out var descElement) ? descElement.GetString() : null,
						RomFileName = root.TryGetProperty("romFileName", out var romFileNameElement) ? romFileNameElement.GetString() : null,
						Region = root.TryGetProperty("region", out var regionElement) ? regionElement.GetString() : null,
						MetadataProvider = root.TryGetProperty("metadataProvider", out var providerElement) ? providerElement.GetString() ?? "Unknown" : "Unknown",
						ProviderId = root.TryGetProperty("providerId", out var providerIdElement) ? providerIdElement.GetString() : null,
						CoverUrl = root.TryGetProperty("coverUrl", out var coverElement) ? coverElement.GetString() : null,
						ArtworkUrls = ExtractStringArray(root, "artworkUrls"),
						ScreenShots = ExtractStringArray(root, "screenShots"),
						VideoUrls = ExtractStringArray(root, "videoUrls"),
						Genres = ExtractStringArray(root, "genres"),
						AlternateNames = ExtractStringArray(root, "alternateNames"),
						MetadataFilePath = Path.GetFileName(jsonFile)
					};

					gamesList.Add(gameEntry);
				}
				catch (Exception)
				{
					// Continue processing other files if one fails to parse
					continue;
				}
			}
		}

		// Sort games by name
		var sortedGames = gamesList.OrderBy(g => ((dynamic)g).Name).ToList();

		// Create the games list JSON
		var gamesListData = new
		{
			Platform = Path.GetFileName(platformDirectory),
			TotalGames = sortedGames.Count,
			LastUpdated = DateTime.UtcNow,
			Games = sortedGames
		};

		var gamesListJsonOptions = new JsonSerializerOptions
		{
			WriteIndented = true,
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase
		};

		var gamesListJson = JsonSerializer.Serialize(gamesListData, gamesListJsonOptions);
		await System.IO.File.WriteAllTextAsync(gamesListPath, gamesListJson, Encoding.UTF8, cancellationToken);
	}

	/// <summary>
	/// Extracts a string array from a JSON element property.
	/// </summary>
	/// <param name="root">The root JSON element.</param>
	/// <param name="propertyName">The property name to extract.</param>
	/// <returns>List of strings from the array property.</returns>
	private static List<string> ExtractStringArray(JsonElement root, string propertyName)
	{
		if (root.TryGetProperty(propertyName, out var arrayElement) && arrayElement.ValueKind == JsonValueKind.Array)
		{
			return arrayElement.EnumerateArray()
				.Where(e => e.ValueKind == JsonValueKind.String && !string.IsNullOrEmpty(e.GetString()))
				.Select(e => e.GetString()!)
				.ToList();
		}
		return new List<string>();
	}

	/// <summary>
	/// Downloads a game image by game ID and image filename.
	/// </summary>
	/// <param name="platformName">The platform name.</param>
	/// <param name="gameId">The game ID.</param>
	/// <param name="imageFileName">The image filename.</param>
	/// <returns>The image file.</returns>
	[HttpGet("platforms/{platformName}/games/{gameId}/images/{imageFileName}")]
	public async Task<IActionResult> GetGameImage(string platformName, string gameId, string imageFileName, CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrWhiteSpace(platformName) || string.IsNullOrWhiteSpace(gameId) || string.IsNullOrWhiteSpace(imageFileName))
		{
			return BadRequest("Platform name, game ID, and image filename are required");
		}

		try
		{
			var romsDirectory = await _mediator.Send(new GetSettingRequest()
			{
				Key = "roms.root"
			}, cancellationToken);

			if (string.IsNullOrEmpty(romsDirectory))
			{
				return BadRequest("ROMs directory is not configured");
			}

			var platformDirectory = Path.Combine(romsDirectory, platformName);
			if (!Directory.Exists(platformDirectory))
			{
				return NotFound("Platform directory not found");
			}

			// Construct the full image path using new ID-based structure
			var fullImagePath = Path.Combine(platformDirectory, ".metadata", "images", gameId, imageFileName);
			
			// Security check: ensure the resolved path is within the expected directory structure
			var expectedImageDirectory = Path.Combine(platformDirectory, ".metadata", "images", gameId);
			var resolvedPath = Path.GetFullPath(fullImagePath);
			var resolvedExpectedDirectory = Path.GetFullPath(expectedImageDirectory);
			
			if (!resolvedPath.StartsWith(resolvedExpectedDirectory, StringComparison.OrdinalIgnoreCase))
			{
				return BadRequest("Invalid image path");
			}

			if (!System.IO.File.Exists(fullImagePath))
			{
				return NotFound("Image not found");
			}

			// Get file info for caching
			var fileInfo = new FileInfo(fullImagePath);
			var lastModified = fileInfo.LastWriteTimeUtc;
			var fileSize = fileInfo.Length;
			
			// Generate ETag based on file path, size, and last modified time
			var etag = GenerateETag(fullImagePath, fileSize, lastModified);
			
			// Check if client has cached version
			if (IsClientCacheValid(etag, lastModified))
			{
				return new StatusCodeResult(304); // Not Modified
			}

			// Determine content type based on file extension
			var extension = Path.GetExtension(fullImagePath).ToLowerInvariant();
			var contentType = extension switch
			{
				".jpg" or ".jpeg" => "image/jpeg",
				".png" => "image/png",
				".gif" => "image/gif",
				".webp" => "image/webp",
				".bmp" => "image/bmp",
				".tiff" => "image/tiff",
				_ => "application/octet-stream"
			};

			// Set caching headers
			SetImageCacheHeaders(etag, lastModified);

			// Return the image file
			var data = System.IO.File.ReadAllBytes(fullImagePath); // Ensure file exists before opening stream
			return File(data, contentType);
		}
		catch (Exception ex)
		{
			return BadRequest($"Error serving image: {ex.Message}");
		}
	}

	/// <summary>
	/// Downloads the metadata file for a specific game by platform name and GameId.
	/// </summary>
	/// <param name="platformName">The platform name (e.g., "N64", "NES", "SNES").</param>
	/// <param name="gameId">The game ID.</param>
	/// <returns>The metadata file (JSON or XML).</returns>
	[HttpGet("platforms/{platformName}/games/{gameId}/metadata")]
	public async Task<IActionResult> GetGameMetadataFile(string platformName, string gameId, CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrWhiteSpace(platformName))
		{
			return BadRequest("Platform name is required");
		}

		if (string.IsNullOrWhiteSpace(gameId))
		{
			return BadRequest("Game ID is required");
		}

		// Validate that the platform name is a valid enum value
		if (!Enum.TryParse<PlatformId>(platformName, true, out var platformId) || platformId == PlatformId.None)
		{
			return BadRequest($"Invalid platform name: {platformName}");
		}

		try
		{
			var romsDirectory = await _mediator.Send(new GetSettingRequest()
			{
				Key = "roms.root"
			}, cancellationToken);

			if (string.IsNullOrEmpty(romsDirectory))
			{
				return BadRequest("ROMs directory is not configured");
			}

			var platformDirectory = Path.Combine(romsDirectory, platformName);
			
			if (!Directory.Exists(platformDirectory))
			{
				return NotFound("Platform directory not found");
			}

			var metadataDirectory = Path.Combine(platformDirectory, ".metadata");
			if (!Directory.Exists(metadataDirectory))
			{
				return NotFound("Metadata directory not found");
			}

			// Look for both JSON and XML metadata files
			var jsonMetadataPath = Path.Combine(metadataDirectory, $"{gameId}.json");

			string metadataFilePath;
			string contentType;
			string fileName;

			if (System.IO.File.Exists(jsonMetadataPath))
			{
				metadataFilePath = jsonMetadataPath;
				contentType = "application/json";
				fileName = $"{gameId}.json";
			}
			else
			{
				return NotFound("Game metadata file not found");
			}

			// Security check: ensure the resolved path is within the expected directory structure
			var resolvedPath = Path.GetFullPath(metadataFilePath);
			var resolvedExpectedDirectory = Path.GetFullPath(metadataDirectory);
			
			if (!resolvedPath.StartsWith(resolvedExpectedDirectory, StringComparison.OrdinalIgnoreCase))
			{
				return BadRequest("Invalid metadata file path");
			}

			// Get file info for caching
			var fileInfo = new FileInfo(metadataFilePath);
			var lastModified = fileInfo.LastWriteTimeUtc;
			var fileSize = fileInfo.Length;
			
			// Generate ETag based on file path, size, and last modified time
			var etag = GenerateETag(metadataFilePath, fileSize, lastModified);
			
			// Check if client has cached version
			if (IsClientCacheValid(etag, lastModified))
			{
				return new StatusCodeResult(304); // Not Modified
			 }

			// Set caching headers
			SetMetadataCacheHeaders(etag, lastModified);

			// Read and return the metadata file
			var stream = System.IO.File.Open(metadataFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
			return File(stream, contentType, $"{platformId}_{fileName}");
		}
		catch (Exception ex)
		{
			return BadRequest($"Error serving metadata file: {ex.Message}");
		}
	}

	/// <summary>
	/// Gets a representative cover image for the platform.
	/// </summary>
	/// <param name="platformName">The platform name.</param>
	/// <returns>The platform cover image URL.</returns>
	[HttpGet("platforms/{platformName}/cover")]
	public async Task<ActionResult<string?>> GetPlatformCover(string platformName, CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrWhiteSpace(platformName))
		{
			return BadRequest("Platform name is required");
		}

		try
		{
			var coverImagePath = await _metadataService.GetPlatformCoverImageAsync(platformName, cancellationToken);
			return Ok(coverImagePath);
		}
		catch (Exception ex)
		{
			return BadRequest($"Error getting platform cover: {ex.Message}");
		}
	}

	/// <summary>
	/// Downloads the platform image by platform name.
	/// </summary>
	/// <param name="platformName">The platform name.</param>
	/// <returns>The platform image file.</returns>
	[HttpGet("platforms/{platformName}/image")]
	public async Task<IActionResult> GetPlatformImage(string platformName, CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrWhiteSpace(platformName))
		{
			return BadRequest("Platform name is required");
		}

		try
		{
			var romsDirectory = await _mediator.Send(new GetSettingRequest()
			{
				Key = "roms.root"
			}, cancellationToken);

			if (string.IsNullOrEmpty(romsDirectory))
			{
				return BadRequest("ROMs directory is not configured");
			}

			var platformDirectory = Path.Combine(romsDirectory, platformName);
			if (!Directory.Exists(platformDirectory))
			{
				return NotFound("Platform directory not found");
			}

			// Look for platform.jpg in the .metadata directory
			var platformImagePath = Path.Combine(platformDirectory, ".metadata", "platform.jpg");
			
			// Security check: ensure the resolved path is within the expected directory structure
			var expectedMetadataDirectory = Path.Combine(platformDirectory, ".metadata");
			var resolvedPath = Path.GetFullPath(platformImagePath);
			var resolvedExpectedDirectory = Path.GetFullPath(expectedMetadataDirectory);
			
			if (!resolvedPath.StartsWith(resolvedExpectedDirectory, StringComparison.OrdinalIgnoreCase))
			{
				return BadRequest("Invalid image path");
			}

			if (!System.IO.File.Exists(platformImagePath))
			{
				return NotFound("Platform image not found");
			}

			// Get file info for caching
			var fileInfo = new FileInfo(platformImagePath);
			var lastModified = fileInfo.LastWriteTimeUtc;
			var fileSize = fileInfo.Length;
			
			// Generate ETag based on file path, size, and last modified time
			var etag = GenerateETag(platformImagePath, fileSize, lastModified);
			
			// Check if client has cached version
			if (IsClientCacheValid(etag, lastModified))
			{
				return new StatusCodeResult(304); // Not Modified
			}

			// Determine content type based on file extension
			var extension = Path.GetExtension(platformImagePath).ToLowerInvariant();
			var contentType = extension switch
			{
				".jpg" or ".jpeg" => "image/jpeg",
				".png" => "image/png",
				".gif" => "image/gif",
				".webp" => "image/webp",
				".bmp" => "image/bmp",
				".tiff" => "image/tiff",
				_ => "application/octet-stream"
			};

			// Set caching headers
			SetImageCacheHeaders(etag, lastModified);

			// Return the image file
			var data = await System.IO.File.ReadAllBytesAsync(platformImagePath, cancellationToken);
			return File(data, contentType);
		}
		catch (Exception ex)
		{
			return BadRequest($"Error serving platform image: {ex.Message}");
		}
	}

	/// <summary>
	/// Generates an ETag for the file based on its path, size, and last modified time.
	/// </summary>
	/// <param name="filePath">The file path.</param>
	/// <param name="fileSize">The file size.</param>
	/// <param name="lastModified">The last modified time.</param>
	/// <returns>The ETag string.</returns>
	private static string GenerateETag(string filePath, long fileSize, DateTime lastModified)
	{
		var input = $"{filePath}:{fileSize}:{lastModified:yyyy-MM-ddTHH:mm:ss.fffZ}";
		var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
		var hash = Convert.ToHexString(hashBytes)[..16]; // Use first 16 characters for shorter ETag
		return $"\"{hash}\"";
	}

	/// <summary>
	/// Checks if the client's cached version is still valid.
	/// </summary>
	/// <param name="etag">The current ETag.</param>
	/// <param name="lastModified">The last modified time.</param>
	/// <returns>True if the client cache is valid.</returns>
	private bool IsClientCacheValid(string etag, DateTime lastModified)
	{
		// Check If-None-Match header (ETag)
		if (Request.Headers.ContainsKey("If-None-Match"))
		{
			var clientETag = Request.Headers["If-None-Match"].ToString();
			if (string.Equals(etag, clientETag, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}

		// Check If-Modified-Since header
		if (Request.Headers.ContainsKey("If-Modified-Since"))
		{
			if (DateTime.TryParse(Request.Headers["If-Modified-Since"], out var clientLastModified))
			{
				// Round down to seconds for comparison (HTTP dates don't include milliseconds)
				var serverLastModified = new DateTime(lastModified.Ticks - (lastModified.Ticks % TimeSpan.TicksPerSecond), DateTimeKind.Utc);
				if (serverLastModified <= clientLastModified)
				{
					return true;
				}
			}
		}

		return false;
	}

	/// <summary>
	/// Sets the caching headers for image responses.
	/// </summary>
	/// <param name="etag">The ETag to set.</param>
	/// <param name="lastModified">The last modified time.</param>
	private void SetImageCacheHeaders(string etag, DateTime lastModified)
	{
		// Set ETag
		Response.Headers.ETag = etag;
		
		// Set Last-Modified
		Response.Headers.LastModified = lastModified.ToString("R"); // RFC1123 format
		
		// Set Cache-Control for aggressive caching since images rarely change
		// Cache for 1 year, but allow revalidation
		Response.Headers.CacheControl = "public, max-age=31536000, must-revalidate";
		
		// Set Expires header as fallback for older clients
		Response.Headers.Expires = DateTime.UtcNow.AddYears(1).ToString("R");
		
		// Add Vary header to indicate that response may vary based on request headers
		Response.Headers.Vary = "If-None-Match, If-Modified-Since";
	}

	/// <summary>
	/// Sets the caching headers for metadata file responses.
	/// </summary>
	/// <param name="etag">The ETag to set.</param>
	/// <param name="lastModified">The last modified time.</param>
	private void SetMetadataCacheHeaders(string etag, DateTime lastModified)
	{
		// Set ETag
		Response.Headers.ETag = etag;
		
		// Set Last-Modified
		Response.Headers.LastModified = lastModified.ToString("R"); // RFC1123 format
		
		// Set Cache-Control for moderate caching since metadata may change
		// Cache for 1 hour, but allow revalidation
		Response.Headers.CacheControl = "public, max-age=3600, must-revalidate";
		
		// Set Expires header as fallback for older clients
		Response.Headers.Expires = DateTime.UtcNow.AddHours(1).ToString("R");
		
		// Add Vary header to indicate that response may vary based on request headers
		Response.Headers.Vary = "If-None-Match, If-Modified-Since";
	}
}