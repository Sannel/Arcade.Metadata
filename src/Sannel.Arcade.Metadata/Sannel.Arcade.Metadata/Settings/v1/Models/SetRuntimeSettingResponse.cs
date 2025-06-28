namespace Sannel.Arcade.Metadata.Settings.v1.Models;

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