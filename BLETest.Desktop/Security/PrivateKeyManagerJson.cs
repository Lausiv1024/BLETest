using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using BLETest.Settings.Security;
namespace BLETest.Desktop.Security;
/// <summary>
/// とりあえずJsonで秘密鍵を保存するためのクラス
/// </summary>
internal class PrivateKeyManagerJson : IPrivateKeyManager
{
    private readonly string _filePath = Path.Combine(Directory.GetParent( Assembly.GetExecutingAssembly().Location).FullName, "privateKeys.json");

    public void AddPrivateKey(Guid guid, string privateKey)
    {
        var keys = new List<PrivateKeyEntry>();
        if (File.Exists(_filePath))
        {
            var json = File.ReadAllText(_filePath);
            keys = System.Text.Json.JsonSerializer.Deserialize<List<PrivateKeyEntry>>(json) ?? new List<PrivateKeyEntry>();
        }
        keys.Add(new PrivateKeyEntry { Guid = guid, PrivateKey = privateKey });
        var newJson = System.Text.Json.JsonSerializer.Serialize(keys, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_filePath, newJson);
    }

    public string GetPrivateKey(Guid guid)
    {
        if (!File.Exists(_filePath))
        {
            throw new FileNotFoundException("Private key file not found.");
        }
        var json = File.ReadAllText(_filePath);
        var keys = System.Text.Json.JsonSerializer.Deserialize<List<PrivateKeyEntry>>(json);
        var entry = keys?.FirstOrDefault(k => k.Guid == guid);
        if (entry == null)
        {
            throw new KeyNotFoundException("Private key not found for the given GUID.");
        }
        return entry.PrivateKey;
    }
}
class PrivateKeyEntry
{
    public Guid Guid { get; set; }
    public string PrivateKey { get; set; } = null!;
}