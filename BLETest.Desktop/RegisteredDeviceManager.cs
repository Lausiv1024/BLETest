using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using BLETest.Settings;
using Windows.ApplicationModel.VoiceCommands;
namespace BLETest;

internal class RegisteredDeviceManager
{
    private List<RegisteredDevice> registeredDevices = new List<RegisteredDevice>();
    private string filePath = "registered_devices.json"; //一旦Jsonで保存。今後秘密鍵の安全な保存方法を検討。
    private RegisteredDevice? NewDevice = null;
    public static RegisteredDeviceManager Default { get; } = new RegisteredDeviceManager();

    public RegisteredDeviceManager() 
    {

    }
    private void FromJson()
    {
        if (File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, filePath)))
        {
            string jsonString = File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, filePath));
            registeredDevices = JsonSerializer.Deserialize<List<RegisteredDevice>>(jsonString) ?? new List<RegisteredDevice>();
        }
        // Implementation for deserializing JSON to RegisteredDevice objects
    }

    public RegisteredDevice CreateNew()
    {
        var keyPair = CryptoUtil.GenerateECDHKeyPair();
        var deviceNew = new RegisteredDevice
        {
            DeviceId = Guid.NewGuid().ToString(),
            PubKey = Convert.ToBase64String(CryptoUtil.PubKeyToByte(keyPair.Public)),
            PrivKey = Convert.ToBase64String(CryptoUtil.PriKeyToByte(keyPair.Private))
        };
        NewDevice = deviceNew;
        return deviceNew;
    }

    public void DisposeNew()
    {
        NewDevice = null;
    }
}

public class  RegisteredDevice
{
    public string DeviceName { get; set; } = string.Empty;
    public string DeviceId { get; set; } = string.Empty;
    public string PubKey { get; set; } = string.Empty;
    public string PrivKey { get; set; } = string.Empty;
}
