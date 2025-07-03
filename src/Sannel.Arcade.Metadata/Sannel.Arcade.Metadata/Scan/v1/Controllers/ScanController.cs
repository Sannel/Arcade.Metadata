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
	/// <returns>Success response.</returns>
	[HttpPost("start")]
	public IActionResult StartScan()
	{
		try
		{
			_scanBackground.ShouldScan = true;
			return Ok(new { Success = true, Message = "Scan started successfully." });
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
			return Ok(new 
			{ 
				Success = true, 
				IsScanning = _scanBackground.ShouldScan,
				Message = _scanBackground.ShouldScan ? "Scanning is active" : "Scanning is inactive"
			});
		}
		catch (Exception ex)
		{
			return StatusCode(500, new { Success = false, Message = $"Failed to get scan status: {ex.Message}" });
		}
	}
}