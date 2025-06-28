namespace Sannel.Arcade.Metadata.Settings.v1.Models;

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