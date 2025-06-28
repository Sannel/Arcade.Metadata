using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

using Sannel.Arcade.Metadata.Components;
using Sannel.Arcade.Metadata.Settings.v1.Services.DBus;

using Tmds.DBus;

namespace Sannel.Arcade.Metadata.Settings.v1.Services;

/// <summary>
/// Linux-specific implementation of IRuntimeSettingsService using KWallet for secure storage.
/// This implementation only works on Linux systems with KDE's KWallet service available.
/// Uses kwallet-query command-line tool for KWallet interaction.
/// </summary>
public class KWalletRuntimeSettingsService : IRuntimeSettingsService, IDisposable
{
	const string WalletName = "kdewallet";
	const string FolderName = "Sannel.Arcade.Metadata";
	const string AppId = "com.sannel.arcade.metadata";
	private readonly Connection _connection;
	private bool _isInitialized;
	private SemaphoreSlim _semaphore = new(1, 1);
	private IKWallet? _kwallet = null;
	private int _handle = -1;

	public KWalletRuntimeSettingsService()
	{
		_connection = new Connection(Address.System);
	}

	public async Task InitializeAsync(CancellationToken cancellationToken = default)
	{
		await _semaphore.WaitAsync(cancellationToken);
		if (_isInitialized)
		{
			_semaphore.Release();
			return;
		}
		try
		{
			await _connection.ConnectAsync();
			await SetupWalletAsync();
			_isInitialized = true;
		}
		finally
		{
			_semaphore.Release();
		}
	}

	protected async Task SetupWalletAsync()
	{
		// Get the KWallet service
		_kwallet = _connection.CreateProxy<IKWallet>(
			"org.kde.kwalletd5",
			"/modules/kwalletd");

		// Open the wallet (windowId = 0 for headless)
		_handle = await _kwallet.openAsync(WalletName, 0, AppId);
		if (_handle < 0)
		{
			Console.WriteLine("Failed to open KWallet.");
			return;
		}

		// Ensure folder exists
		if (!await _kwallet.hasFolderAsync(_handle, FolderName, AppId))
		{
			await _kwallet.createFolderAsync(_handle, FolderName, AppId);
		}
	}

	public void Dispose()
	{
		if (_isInitialized)
		{
			_connection.Dispose();
			_isInitialized = true;
		}
	}

	public async IAsyncEnumerable<string> GetAllKeysAsync([EnumeratorCancellation]CancellationToken cancellationToken = default)
	{
		if (_kwallet is null)
		{
			throw new InvalidOperationException("KWallet is not initialized. Call InitializeAsync first.");
		}

		foreach(var key in await _kwallet.entryListAsync(_handle, FolderName, AppId))
		{
			yield return key;
		}
	}

	public async Task<string?> GetSettingAsync(string key, string? defaultValue = null, CancellationToken cancellationToken = default)
	{
		if (_kwallet is null)
		{
			throw new InvalidOperationException("KWallet is not initialized. Call InitializeAsync first.");
		}
		return await _kwallet.readPasswordAsync(_handle, FolderName, key, AppId);
	}

	public async Task<bool> HasSettingAsync(string key, CancellationToken cancellationToken = default)
	{
		if (_kwallet is null)
		{
			throw new InvalidOperationException("KWallet is not initialized. Call InitializeAsync first.");
		}

		return await _kwallet.hasEntryAsync(_handle, FolderName, key, AppId);
	}


	public Task RemoveSettingAsync(string key, CancellationToken cancellationToken = default)
	{
		if (_kwallet is null)
		{
			throw new InvalidOperationException("KWallet is not initialized. Call InitializeAsync first.");
		}

		return _kwallet.removeEntryAsync(_handle, FolderName, key, AppId);
	}

	public async Task SetSettingAsync(string key, string value, CancellationToken cancellationToken = default)
	{
		if (_kwallet is null)
		{
			throw new InvalidOperationException("KWallet is not initialized. Call InitializeAsync first.");
		}
		await _kwallet.writePasswordAsync(_handle, FolderName, key, value, AppId);
	}
}