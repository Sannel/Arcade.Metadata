using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Sannel.Arcade.Metadata.Scan.v1.Controllers;

/// <summary>
/// Controller for managing scan operations.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class ScanController : ControllerBase
{
	private readonly ScanBackground _scanBackground;

	/// <summary>
	/// Initializes a new instance of the <see cref="ScanController"/> class.
	/// </summary>
	/// <param name="scanBackground">The scan background service.</param>
	public ScanController(ScanBackground scanBackground)
	{
		_scanBackground = scanBackground ?? throw new ArgumentNullException(nameof(scanBackground));
	}

	/// <summary>
	/// Starts the background scanning process.
	/// </summary>
	/// <param name="forceRebuild">Whether to rebuild metadata even if it already exists.</param>
	/// <returns>Success response.</returns>
	[HttpPost("start")]
	public IActionResult StartScan([FromQuery] bool forceRebuild = false)
	{
		try
		{
			_scanBackground.ForceRebuild = forceRebuild;
			_scanBackground.ShouldScan = true;
			return Ok(new { Success = true, Message = $"Scan started successfully{(forceRebuild ? " with force rebuild enabled" : "")}." });
		}
		catch (Exception ex)
		{
			return StatusCode(500, new { Success = false, Message = $"Failed to start scan: {ex.Message}" });
		}
	}

	/// <summary>
	/// Stops the background scanning process.
	/// </summary>
	/// <returns>Success response.</returns>
	[HttpPost("stop")]
	public IActionResult StopScan()
	{
		try
		{
			_scanBackground.ShouldScan = false;
			_scanBackground.ForceRebuild = false;
			return Ok(new { Success = true, Message = "Scan stopped successfully." });
		}
		catch (Exception ex)
		{
			return StatusCode(500, new { Success = false, Message = $"Failed to stop scan: {ex.Message}" });
		}
	}

	/// <summary>
	/// Gets the current scan status.
	/// </summary>
	/// <returns>The scan status.</returns>
	[HttpGet("status")]
	public IActionResult GetScanStatus()
	{
		try
		{
			var message = _scanBackground.ShouldScan 
				? $"Scanning is active{(_scanBackground.ForceRebuild ? " (force rebuild enabled)" : "")}" 
				: "Scanning is inactive";
			
			return Ok(new 
			{ 
				Success = true, 
				IsScanning = _scanBackground.ShouldScan,
				ForceRebuild = _scanBackground.ForceRebuild,
				Message = message
			});
		}
		catch (Exception ex)
		{
			return StatusCode(500, new { Success = false, Message = $"Failed to get scan status: {ex.Message}" });
		}
	}
}