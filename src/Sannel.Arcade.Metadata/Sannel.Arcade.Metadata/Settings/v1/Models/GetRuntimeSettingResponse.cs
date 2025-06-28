namespace Sannel.Arcade.Metadata.Settings.v1.Models;

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