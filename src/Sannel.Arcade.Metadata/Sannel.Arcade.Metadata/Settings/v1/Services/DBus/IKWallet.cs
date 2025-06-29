using Tmds.DBus;

namespace Sannel.Arcade.Metadata.Settings.v1.Services.DBus;

[DBusInterface("org.kde.KWallet")]
public interface IKWallet : IDBusObject
{
	Task<int> openAsync(string wallet, long wId, string appid);
	Task<bool> isOpenAsync(int handle);
	Task<bool> hasFolderAsync(int handle, string folder, string appid);
	Task<bool> createFolderAsync(int handle, string folder, string appid);
	Task<int> writePasswordAsync(int handle, string folder, string key, string value, string appid);
	Task<string> readPasswordAsync(int handle, string folder, string key, string appid);
	Task<bool> hasEntryAsync(int handle, string folder, string key, string appid);
	Task<string[]> entryListAsync(int handle, string folder, string appid);
	Task<int> removeEntryAsync(int handle, string folder, string key, string appid);
}
