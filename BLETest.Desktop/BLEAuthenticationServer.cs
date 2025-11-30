using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.GenericAttributeProfile;

namespace BLETest;

internal class BLEAuthenticationServer
{
    public const int BufferSize = 1024;
    private NewDeviceContext newDeviceContext = null;

    private GattLocalCharacteristic authCharacteristic;
    public BLEAuthenticationServer() { }

    public void StartNewDeviceAcceptance()
    {
        // Implementation for starting new device acceptance
    }

    public void StopNewDeviceAcceptance()
    {
        // Implementation for stopping new device acceptance
    }
}
internal class NewDeviceContext
{
    public string DeviceId { get; set; }
    public string PublicKey { get; set; }
    public string PrivateKey { get; set; }
}

internal class BleDevice
{
    public string DeviceId { get; set; }
    public string DeviceName { get; set; }
    public string PublicKey { get; set; }
}

internal class BleDeviceManager 
{
    public static BleDeviceManager Default { get; } = new BleDeviceManager();
    const string DeviceStorageFileName = "RegisteredBleDevices.json";
    private string storageFilePath = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        DeviceStorageFileName);

    private List<BleDevice> registeredDevices = new List<BleDevice>();

    public BleDeviceManager() 
    {

    }

    private void LoadRegisteredDevices()
    {
        if (!File.Exists(storageFilePath))
        {
            registeredDevices = new List<BleDevice>();
            var serialized = JsonSerializer.Serialize(registeredDevices);
            File.WriteAllText(storageFilePath, serialized);
            return;
        }

        var fileContent = File.ReadAllText(storageFilePath);
        registeredDevices = JsonSerializer.Deserialize<List<BleDevice>>(fileContent);
    }

}
