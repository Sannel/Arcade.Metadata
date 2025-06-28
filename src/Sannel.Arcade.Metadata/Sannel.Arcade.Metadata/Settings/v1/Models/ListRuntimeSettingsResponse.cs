namespace Sannel.Arcade.Metadata.Settings.v1.Models;

/// <summary>
/// Response model for listing all runtime settings.
/// </summary>
public class ListRuntimeSettingsResponse
{
	/// <summary>
	/// Whether the operation was successful.
	/// </summary>
	public bool Success { get; set; }

	/// <summary>
	/// List of all runtime settings.
	/// </summary>
	public IEnumerable<RuntimeSetting> Settings { get; set; } = [];

	/// <summary>
	/// Error message if the operation failed.
	/// </summary>
	public string? ErrorMessage { get; set; }
}