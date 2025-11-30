using System;
using System.Collections.Generic;
using System.IO;
namespace BLETest;

internal class RegisteredDeviceManager
{
    private List<RegisteredDevice> registeredDevices = new List<RegisteredDevice>();
    private string filePath = "registered_devices.json"; //一旦Jsonで保存。今後秘密鍵の安全な保存方法を検討。
    public RegisteredDeviceManager() 
    {

    }
    private void FromJson(string json)
    {
        if (File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, filePath)))
        {
            string jsonString = File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, filePath));
            //registeredDevices = JsonSerializer;
        }
        // Implementation for deserializing JSON to RegisteredDevice objects
    }
}

public class  RegisteredDevice
{
    public string DeviceName { get; set; } = string.Empty;
    public string DeviceId { get; set; } = string.Empty;
    public string PubKey { get; set; } = string.Empty;
    public string PrivKey { get; set; } = string.Empty;
}
