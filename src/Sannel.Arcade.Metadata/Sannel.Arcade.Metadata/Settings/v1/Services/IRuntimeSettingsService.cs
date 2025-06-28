namespace Sannel.Arcade.Metadata.Settings.v1.Services;

/// <summary>
/// Service for managing runtime application settings that persist during application lifecycle.
/// </summary>
public interface IRuntimeSettingsService
{
	Task InitializeAsync(CancellationToken cancellationToken = default);
	/// <summary>
	/// Gets a runtime setting value by key.
	/// </summary>
	/// <param name="key">The setting key.</param>
	/// <param name="defaultValue">The default value to return if the setting is not found.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The setting value or the default value if not found.</returns>
	Task<string?> GetSettingAsync(string key, string? defaultValue = null, CancellationToken cancellationToken = default);

	/// <summary>
	/// Sets a runtime setting value.
	/// </summary>
	/// <param name="key">The setting key.</param>
	/// <param name="value">The setting value.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	Task SetSettingAsync(string key, string value, CancellationToken cancellationToken = default);

	/// <summary>
	/// Removes a runtime setting.
	/// </summary>
	/// <param name="key">The setting key to remove.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	Task RemoveSettingAsync(string key, CancellationToken cancellationToken = default);

	/// <summary>
	/// Checks if a runtime setting exists.
	/// </summary>
	/// <param name="key">The setting key.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>True if the setting exists, false otherwise.</returns>
	Task<bool> HasSettingAsync(string key, CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets all runtime setting keys.
	/// </summary>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>A collection of all setting keys.</returns>
	IAsyncEnumerable<string> GetAllKeysAsync(CancellationToken cancellationToken = default);
}