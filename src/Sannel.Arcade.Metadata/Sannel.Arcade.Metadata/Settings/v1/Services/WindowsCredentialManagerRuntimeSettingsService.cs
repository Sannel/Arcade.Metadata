using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Sannel.Arcade.Metadata.Settings.v1.Services;

/// <summary>
/// Windows-specific implementation of IRuntimeSettingsService using Windows Credential Manager for secure storage.
/// This implementation only works on Windows systems and uses the native Windows Credential Manager APIs via P/Invoke.
/// </summary>
public class WindowsCredentialManagerRuntimeSettingsService : IRuntimeSettingsService
{
	private const string TargetPrefix = "Sannel.Arcade.Metadata:";
	private const string KeysListTarget = "Sannel.Arcade.Metadata:Keys";
	private bool _isInitialized;
	private readonly SemaphoreSlim _semaphore = new(1, 1);

	public async Task InitializeAsync(CancellationToken cancellationToken = default)
	{
		await _semaphore.WaitAsync(cancellationToken);
		try
		{
			if (!OperatingSystem.IsWindows())
			{
				throw new PlatformNotSupportedException("Windows Credential Manager is only supported on Windows.");
			}

			// Ensure the keys list exists
			if (!await HasSettingInternalAsync(KeysListTarget))
			{
				await SetSettingInternalAsync(KeysListTarget, string.Empty);
			}

			_isInitialized = true;
		}
		finally
		{
			_semaphore.Release();
		}
	}

	public Task<string?> GetSettingAsync(string key, string? defaultValue = null, CancellationToken cancellationToken = default)
	{
		ThrowIfNotInitialized();
		ArgumentException.ThrowIfNullOrWhiteSpace(key);

		string target = GetTargetName(key);
		return Task.FromResult(GetCredential(target) ?? defaultValue);
	}

	public async Task SetSettingAsync(string key, string value, CancellationToken cancellationToken = default)
	{
		ThrowIfNotInitialized();
		ArgumentException.ThrowIfNullOrWhiteSpace(key);
		ArgumentNullException.ThrowIfNull(value);

		string target = GetTargetName(key);

		// Add key to keys list if it doesn't exist
		await AddKeyToListAsync(key);

		// Store the credential
		WriteCredential(target, value);
	}

	public async Task RemoveSettingAsync(string key, CancellationToken cancellationToken = default)
	{
		ThrowIfNotInitialized();
		ArgumentException.ThrowIfNullOrWhiteSpace(key);

		string target = GetTargetName(key);

		// Remove from keys list
		await RemoveKeyFromListAsync(key);

		// Delete the credential
		DeleteCredential(target);
	}

	public Task<bool> HasSettingAsync(string key, CancellationToken cancellationToken = default)
	{
		ThrowIfNotInitialized();
		ArgumentException.ThrowIfNullOrWhiteSpace(key);

		string target = GetTargetName(key);
		return HasSettingInternalAsync(target);
	}

	public async IAsyncEnumerable<string> GetAllKeysAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		ThrowIfNotInitialized();

		string? keysList = GetCredential(KeysListTarget);
		if (string.IsNullOrEmpty(keysList))
		{
			yield break;
		}

		string[] keys = keysList.Split('|', StringSplitOptions.RemoveEmptyEntries);
		foreach (string key in keys)
		{
			if (!string.IsNullOrWhiteSpace(key))
			{
				yield return key;
			}
		}
		
		await Task.CompletedTask; // Required to make this method async enumerable
	}

	private void ThrowIfNotInitialized()
	{
		if (!_isInitialized)
		{
			throw new InvalidOperationException("Service is not initialized. Call InitializeAsync first.");
		}
	}

	private static string GetTargetName(string key) => $"{TargetPrefix}{key}";

	private Task<bool> HasSettingInternalAsync(string target)
	{
		return Task.FromResult(GetCredential(target) != null);
	}

	private Task SetSettingInternalAsync(string target, string value)
	{
		WriteCredential(target, value);
		return Task.CompletedTask;
	}

	private async Task AddKeyToListAsync(string key)
	{
		string? currentKeys = GetCredential(KeysListTarget) ?? string.Empty;
		List<string> keysList = currentKeys.Split('|', StringSplitOptions.RemoveEmptyEntries).ToList();

		if (!keysList.Contains(key))
		{
			keysList.Add(key);
			string newKeysList = string.Join("|", keysList);
			await SetSettingInternalAsync(KeysListTarget, newKeysList);
		}
	}

	private async Task RemoveKeyFromListAsync(string key)
	{
		string? currentKeys = GetCredential(KeysListTarget) ?? string.Empty;
		List<string> keysList = currentKeys.Split('|', StringSplitOptions.RemoveEmptyEntries).ToList();

		if (keysList.Remove(key))
		{
			string newKeysList = string.Join("|", keysList);
			await SetSettingInternalAsync(KeysListTarget, newKeysList);
		}
	}

	private static string? GetCredential(string target)
	{
		if (!CredRead(target, CRED_TYPE_GENERIC, 0, out IntPtr credPtr))
		{
			return null;
		}

		try
		{
			CREDENTIAL cred = Marshal.PtrToStructure<CREDENTIAL>(credPtr);
			if (cred.CredentialBlob != IntPtr.Zero && cred.CredentialBlobSize > 0)
			{
				byte[] credentialBytes = new byte[cred.CredentialBlobSize];
				Marshal.Copy(cred.CredentialBlob, credentialBytes, 0, (int)cred.CredentialBlobSize);
				return Encoding.UTF8.GetString(credentialBytes);
			}
		}
		finally
		{
			CredFree(credPtr);
		}

		return null;
	}

	private static void WriteCredential(string target, string secret)
	{
		byte[] secretBytes = Encoding.UTF8.GetBytes(secret);

		CREDENTIAL credential = new()
		{
			TargetName = target,
			Type = CRED_TYPE_GENERIC,
			UserName = Environment.UserName,
			CredentialBlob = Marshal.AllocHGlobal(secretBytes.Length),
			CredentialBlobSize = (uint)secretBytes.Length,
			Persist = CRED_PERSIST_LOCAL_MACHINE
		};

		try
		{
			Marshal.Copy(secretBytes, 0, credential.CredentialBlob, secretBytes.Length);

			if (!CredWrite(ref credential, 0))
			{
				int error = Marshal.GetLastWin32Error();
				throw new InvalidOperationException($"Failed to write credential. Win32 error: {error}");
			}
		}
		finally
		{
			if (credential.CredentialBlob != IntPtr.Zero)
			{
				Marshal.FreeHGlobal(credential.CredentialBlob);
			}
		}
	}

	private static void DeleteCredential(string target)
	{
		CredDelete(target, CRED_TYPE_GENERIC, 0);
	}

	// Windows API constants
	private const uint CRED_TYPE_GENERIC = 1;
	private const uint CRED_PERSIST_LOCAL_MACHINE = 2;

	// Windows API structures
	[StructLayout(LayoutKind.Sequential)]
	private struct CREDENTIAL
	{
		public uint Flags;
		public uint Type;
		[MarshalAs(UnmanagedType.LPWStr)]
		public string TargetName;
		[MarshalAs(UnmanagedType.LPWStr)]
		public string? Comment;
		public FILETIME LastWritten;
		public uint CredentialBlobSize;
		public IntPtr CredentialBlob;
		public uint Persist;
		public uint AttributeCount;
		public IntPtr Attributes;
		[MarshalAs(UnmanagedType.LPWStr)]
		public string? TargetAlias;
		[MarshalAs(UnmanagedType.LPWStr)]
		public string? UserName;
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct FILETIME
	{
		public uint dwLowDateTime;
		public uint dwHighDateTime;
	}

	// Windows API imports
	[DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
	private static extern bool CredRead(
		string target,
		uint type,
		uint reservedFlag,
		out IntPtr credentialPtr);

	[DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
	private static extern bool CredWrite(
		ref CREDENTIAL userCredential,
		uint flags);

	[DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
	private static extern bool CredDelete(
		string target,
		uint type,
		uint flags);

	[DllImport("advapi32.dll")]
	private static extern void CredFree(IntPtr cred);
}