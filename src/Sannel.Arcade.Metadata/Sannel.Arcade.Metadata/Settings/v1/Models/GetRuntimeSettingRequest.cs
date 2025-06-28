namespace Sannel.Arcade.Metadata.Settings.v1.Models;

/// <summary>
/// Request model for getting a runtime setting.
/// </summary>
public class GetRuntimeSettingRequest
{
	/// <summary>
	/// The key of the setting to retrieve.
	/// </summary>
	public string Key { get; set; } = string.Empty;
}