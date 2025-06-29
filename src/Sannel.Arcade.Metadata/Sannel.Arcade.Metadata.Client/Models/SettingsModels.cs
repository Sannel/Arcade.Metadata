using System.ComponentModel.DataAnnotations;

namespace Sannel.Arcade.Metadata.Client.Models;

/// <summary>
/// Represents a runtime setting with key-value pair and metadata.
/// </summary>
public class RuntimeSetting
{
	/// <summary>
	/// The unique key for the setting.
	/// </summary>
	public string Key { get; set; } = string.Empty;

	/// <summary>
	/// The value of the setting.
	/// </summary>
	public string Value { get; set; } = string.Empty;

	/// <summary>
	/// When the setting was created.
	/// </summary>
	public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

	/// <summary>
	/// When the setting was last updated.
	/// </summary>
	public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

	/// <summary>
	/// Optional description of what this setting controls.
	/// </summary>
	public string? Description { get; set; }

	/// <summary>
	/// Whether this setting is considered sensitive and should be handled with care.
	/// </summary>
	public bool IsSensitive { get; set; }
}

/// <summary>
/// Response model for getting a runtime setting.
/// </summary>
public class GetRuntimeSettingResponse
{
	/// <summary>
	/// Whether the operation was successful.
	/// </summary>
	public bool Success { get; set; }

	/// <summary>
	/// The runtime setting if found.
	/// </summary>
	public RuntimeSetting? Setting { get; set; }

	/// <summary>
	/// Error message if the operation failed.
	/// </summary>
	public string? ErrorMessage { get; set; }
}

/// <summary>
/// Request model for setting a runtime setting.
/// </summary>
public class SetRuntimeSettingRequest
{
	/// <summary>
	/// The key of the setting.
	/// </summary>
	public string Key { get; set; } = string.Empty;

	/// <summary>
	/// The value of the setting.
	/// </summary>
	public string Value { get; set; } = string.Empty;

	/// <summary>
	/// Optional description of the setting.
	/// </summary>
	public string? Description { get; set; }

	/// <summary>
	/// Whether this setting is considered sensitive.
	/// </summary>
	public bool IsSensitive { get; set; }
}

/// <summary>
/// Response model for setting a runtime setting.
/// </summary>
public class SetRuntimeSettingResponse
{
	/// <summary>
	/// Whether the operation was successful.
	/// </summary>
	public bool Success { get; set; }

	/// <summary>
	/// Error message if the operation failed.
	/// </summary>
	public string? ErrorMessage { get; set; }
}

/// <summary>
/// Model for IGDB settings form.
/// </summary>
public class IgdbSettings
{
	[Required(ErrorMessage = "Client ID is required")]
	public string ClientId { get; set; } = string.Empty;

	[Required(ErrorMessage = "Client Secret is required")]
	public string ClientSecret { get; set; } = string.Empty;
}