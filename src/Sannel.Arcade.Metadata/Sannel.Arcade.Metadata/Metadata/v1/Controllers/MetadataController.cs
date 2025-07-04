using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sannel.Arcade.Metadata.Metadata.v1.Models;
using Sannel.Arcade.Metadata.Metadata.v1.Services;
using MediatR;
using Sannel.Arcade.Metadata.Common.Settings;
using Sannel.Arcade.Metadata.Scan.v1.Clients.Models;
using System.Security.Cryptography;
using System.Text;

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
	/// Serves an image file from the metadata directory.
	/// </summary>
	/// <param name="platformName">The platform name.</param>
	/// <param name="imagePath">The relative path to the image.</param>
	/// <returns>The image file.</returns>
	[HttpGet("platforms/{platformName}/images/{**imagePath}")]
	public async Task<IActionResult> GetImage(string platformName, string imagePath, CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrWhiteSpace(platformName) || string.IsNullOrWhiteSpace(imagePath))
		{
			return BadRequest("Platform name and image path are required");
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

			// Construct the full image path
			var fullImagePath = Path.Combine(platformDirectory, ".metadata", imagePath);
			
			// Security check: ensure the resolved path is within the metadata directory
			var metadataDirectory = Path.Combine(platformDirectory, ".metadata");
			var resolvedPath = Path.GetFullPath(fullImagePath);
			var resolvedMetadataDirectory = Path.GetFullPath(metadataDirectory);
			
			if (!resolvedPath.StartsWith(resolvedMetadataDirectory, StringComparison.OrdinalIgnoreCase))
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
				_ => "application/octet-stream"
			};

			// Set caching headers
			SetImageCacheHeaders(etag, lastModified);

			// Return the image file
			var fileStream = new FileStream(fullImagePath, FileMode.Open, FileAccess.Read, FileShare.Read);
			return File(fileStream, contentType);
		}
		catch (Exception ex)
		{
			return BadRequest($"Error serving image: {ex.Message}");
		}
	}

	/// <summary>
	/// Downloads a game image by PlatformId and image path.
	/// </summary>
	/// <param name="platformId">The platform ID enum value.</param>
	/// <param name="imagePath">The relative path to the image.</param>
	/// <returns>The image file.</returns>
	[HttpGet("game-images/{platformId}/{**imagePath}")]
	public async Task<IActionResult> GetGameImage(PlatformId platformId, string imagePath, CancellationToken cancellationToken = default)
	{
		if (platformId == PlatformId.None || string.IsNullOrWhiteSpace(imagePath))
		{
			return BadRequest("Valid platform ID and image path are required");
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

			// Convert PlatformId enum to directory name (assumes directory names match enum values)
			var platformName = platformId.ToString();
			var platformDirectory = Path.Combine(romsDirectory, platformName);
			
			if (!Directory.Exists(platformDirectory))
			{
				return NotFound($"Platform directory not found for {platformName}");
			}

			// Construct the full image path
			var fullImagePath = Path.Combine(platformDirectory, ".metadata", imagePath);
			
			// Security check: ensure the resolved path is within the metadata directory
			var metadataDirectory = Path.Combine(platformDirectory, ".metadata");
			var resolvedPath = Path.GetFullPath(fullImagePath);
			var resolvedMetadataDirectory = Path.GetFullPath(metadataDirectory);
			
			if (!resolvedPath.StartsWith(resolvedMetadataDirectory, StringComparison.OrdinalIgnoreCase))
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
			var fileStream = new FileStream(fullImagePath, FileMode.Open, FileAccess.Read, FileShare.Read);
			return File(fileStream, contentType);
		}
		catch (Exception ex)
		{
			return BadRequest($"Error serving image: {ex.Message}");
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
}