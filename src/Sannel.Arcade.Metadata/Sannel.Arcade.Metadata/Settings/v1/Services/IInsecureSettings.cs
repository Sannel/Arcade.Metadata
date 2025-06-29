namespace Sannel.Arcade.Metadata.Settings.v1.Services;

/// <summary>
/// Interface for managing insecure settings that accept key-value pairs.
/// </summary>
public interface IInsecureSettings
{
	/// <summary>
	/// Gets a setting value by key.
	/// </summary>
	/// <param name="key">The setting key.</param>
	/// <returns>The setting value if found, otherwise null.</returns>
	string? GetValue(string key);

	/// <summary>
	/// Sets a setting value by key.
	/// </summary>
	/// <param name="key">The setting key.</param>
	/// <param name="value">The setting value.</param>
	void SetValue(string key, string value);
}