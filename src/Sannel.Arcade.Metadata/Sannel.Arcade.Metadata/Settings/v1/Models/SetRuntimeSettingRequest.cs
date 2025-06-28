namespace Sannel.Arcade.Metadata.Settings.v1.Models;

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