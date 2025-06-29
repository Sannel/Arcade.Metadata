using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Sannel.Arcade.Metadata.Settings.v1.Models;
using Sannel.Arcade.Metadata.Settings.v1.Services;

namespace Sannel.Arcade.Metadata.Settings.v1.Controllers;

/// <summary>
/// Controller for managing runtime settings.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class SettingsController : ControllerBase
{
	private readonly IRuntimeSettingsService _settingsService;
	private readonly IInsecureSettings _insecureSettings;

	/// <summary>
	/// Initializes a new instance of the <see cref="SettingsController"/> class.
	/// </summary>
	/// <param name="settingsService">The runtime settings service.</param>
	/// <param name="insecureSettings">The insecure settings service.</param>
	public SettingsController(IRuntimeSettingsService settingsService, IInsecureSettings insecureSettings)
	{
		_settingsService = settingsService;
		_insecureSettings = insecureSettings;
	}

	/// <summary>
	/// Gets a runtime setting by key.
	/// </summary>
	/// <param name="key">The setting key.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The runtime setting response.</returns>
	[HttpGet("{key}")]
	public async Task<ActionResult<GetRuntimeSettingResponse>> GetSetting(
		string key, 
		CancellationToken cancellationToken = default)
	{
		try
		{
			if (string.IsNullOrWhiteSpace(key))
			{
				return BadRequest(new GetRuntimeSettingResponse
				{
					Success = false,
					ErrorMessage = "Setting key cannot be empty."
				});
			}

			string? value = await _settingsService.GetSettingAsync(key, cancellationToken: cancellationToken);
			
			if (value is null)
			{
				return NotFound(new GetRuntimeSettingResponse
				{
					Success = false,
					ErrorMessage = $"Setting with key '{key}' not found."
				});
			}

			RuntimeSetting setting = new()
			{
				Key = key,
				Value = value,
				CreatedAt = DateTimeOffset.UtcNow, // Note: We don't have creation time stored
				UpdatedAt = DateTimeOffset.UtcNow   // Note: We don't have update time stored
			};

			return Ok(new GetRuntimeSettingResponse
			{
				Success = true,
				Setting = setting
			});
		}
		catch (Exception ex)
		{
			return StatusCode(500, new GetRuntimeSettingResponse
			{
				Success = false,
				ErrorMessage = $"An error occurred while retrieving the setting: {ex.Message}"
			});
		}
	}

	/// <summary>
	/// Sets a runtime setting by key and value using route parameters.
	/// </summary>
	/// <param name="key">The setting key.</param>
	/// <param name="value">The setting value.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The runtime setting response.</returns>
	[HttpPut("{key}")]
	public async Task<ActionResult<SetRuntimeSettingResponse>> SetSetting(
		string key,
		[FromQuery] string value,
		CancellationToken cancellationToken = default)
	{
		try
		{
			if (string.IsNullOrWhiteSpace(key))
			{
				return BadRequest(new SetRuntimeSettingResponse
				{
					Success = false,
					ErrorMessage = "Setting key cannot be empty."
				});
			}

			if (value is null)
			{
				return BadRequest(new SetRuntimeSettingResponse
				{
					Success = false,
					ErrorMessage = "Setting value cannot be null."
				});
			}

			await _settingsService.SetSettingAsync(key, value, cancellationToken);

			return Ok(new SetRuntimeSettingResponse
			{
				Success = true
			});
		}
		catch (Exception ex)
		{
			return StatusCode(500, new SetRuntimeSettingResponse
			{
				Success = false,
				ErrorMessage = $"An error occurred while setting the setting: {ex.Message}"
			});
		}
	}

	/// <summary>
	/// Gets an insecure setting by key.
	/// </summary>
	/// <param name="key">The setting key.</param>
	/// <returns>The runtime setting response.</returns>
	[HttpGet("insecure/{key}")]
	public ActionResult<GetRuntimeSettingResponse> GetInsecureSetting(string key)
	{
		try
		{
			if (string.IsNullOrWhiteSpace(key))
			{
				return BadRequest(new GetRuntimeSettingResponse
				{
					Success = false,
					ErrorMessage = "Setting key cannot be empty."
				});
			}

			string? value = _insecureSettings.GetValue(key);
			
			if (value is null)
			{
				return NotFound(new GetRuntimeSettingResponse
				{
					Success = false,
					ErrorMessage = $"Setting with key '{key}' not found."
				});
			}

			RuntimeSetting setting = new()
			{
				Key = key,
				Value = value,
				CreatedAt = DateTimeOffset.UtcNow, // Note: We don't have creation time stored
				UpdatedAt = DateTimeOffset.UtcNow   // Note: We don't have update time stored
			};

			return Ok(new GetRuntimeSettingResponse
			{
				Success = true,
				Setting = setting
			});
		}
		catch (Exception ex)
		{
			return StatusCode(500, new GetRuntimeSettingResponse
			{
				Success = false,
				ErrorMessage = $"An error occurred while retrieving the setting: {ex.Message}"
			});
		}
	}

	/// <summary>
	/// Sets an insecure setting by key and value.
	/// </summary>
	/// <param name="key">The setting key.</param>
	/// <param name="value">The setting value.</param>
	/// <returns>The runtime setting response.</returns>
	[HttpPut("insecure/{key}")]
	public ActionResult<SetRuntimeSettingResponse> SetInsecureSetting(
		string key,
		[FromQuery] string value)
	{
		try
		{
			if (string.IsNullOrWhiteSpace(key))
			{
				return BadRequest(new SetRuntimeSettingResponse
				{
					Success = false,
					ErrorMessage = "Setting key cannot be empty."
				});
			}

			if (value is null)
			{
				return BadRequest(new SetRuntimeSettingResponse
				{
					Success = false,
					ErrorMessage = "Setting value cannot be null."
				});
			}

			_insecureSettings.SetValue(key, value);

			return Ok(new SetRuntimeSettingResponse
			{
				Success = true
			});
		}
		catch (Exception ex)
		{
			return StatusCode(500, new SetRuntimeSettingResponse
			{
				Success = false,
				ErrorMessage = $"An error occurred while setting the setting: {ex.Message}"
			});
		}
	}
}