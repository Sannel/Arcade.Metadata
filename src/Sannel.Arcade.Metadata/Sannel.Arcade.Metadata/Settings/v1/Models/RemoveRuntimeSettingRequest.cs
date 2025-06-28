namespace Sannel.Arcade.Metadata.Settings.v1.Models;

/// <summary>
/// Request model for removing a runtime setting.
/// </summary>
public class RemoveRuntimeSettingRequest
{
	/// <summary>
	/// The key of the setting to remove.
	/// </summary>
	public string Key { get; set; } = string.Empty;
}