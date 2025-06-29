using System.Text.Json;

namespace Sannel.Arcade.Metadata.Settings.v1.Services;

/// <summary>
/// Implementation of IInsecureSettings that stores settings as JSON file in ~/.sannel/arcade/metadata/settings.json
/// </summary>
public class JsonFileInsecureSettings : IInsecureSettings
{
	private readonly string _filePath;
	private readonly SemaphoreSlim _semaphore = new(1, 1);
	private readonly JsonSerializerOptions _jsonOptions;

	public JsonFileInsecureSettings()
	{
		string homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
		string settingsDirectory = Path.Combine(homeDirectory, ".sannel", "arcade", "metadata");
		_filePath = Path.Combine(settingsDirectory, "settings.json");
		
		_jsonOptions = new JsonSerializerOptions
		{
			WriteIndented = true,
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase
		};

		// Ensure directory exists
		Directory.CreateDirectory(settingsDirectory);
	}

	public string? GetValue(string key)
	{
		_semaphore.Wait();
		try
		{
			Dictionary<string, string> settings = LoadSettings();
			return settings.TryGetValue(key, out string? value) ? value : null;
		}
		finally
		{
			_semaphore.Release();
		}
	}

	public void SetValue(string key, string value)
	{
		_semaphore.Wait();
		try
		{
			Dictionary<string, string> settings = LoadSettings();
			settings[key] = value;
			SaveSettings(settings);
		}
		finally
		{
			_semaphore.Release();
		}
	}

	private Dictionary<string, string> LoadSettings()
	{
		if (!File.Exists(_filePath))
		{
			return new Dictionary<string, string>();
		}

		try
		{
			string json = File.ReadAllText(_filePath);
			return JsonSerializer.Deserialize<Dictionary<string, string>>(json, _jsonOptions) 
				   ?? new Dictionary<string, string>();
		}
		catch
		{
			// Return empty dictionary if file is corrupted or unreadable
			return new Dictionary<string, string>();
		}
	}

	private void SaveSettings(Dictionary<string, string> settings)
	{
		string json = JsonSerializer.Serialize(settings, _jsonOptions);
		File.WriteAllText(_filePath, json);
	}
}