using BLETest.Settings;
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
    private NewDeviceContext newDeviceContext = null!;

    private GattLocalCharacteristic authCharacteristicRead = null!;
    private GattLocalCharacteristic authCharacteristicWrite = null!;
    private GattServiceProvider gattServiceProvider = null!;
    public BLEAuthenticationServer() { }

    public async Task BLEInitializeAsync()
    {
        var gattSvcProviderRes = await GattServiceProvider.CreateAsync(BLESettings.AuthCharacteristicRead);
        if (gattSvcProviderRes.Error != Windows.Devices.Bluetooth.BluetoothError.Success)
        {
            Console.WriteLine("Failed to create GattServiceProvider::" + gattSvcProviderRes.Error);
            return;
        }
        var gattSvcProvider = gattSvcProviderRes.ServiceProvider;
        gattServiceProvider = gattSvcProvider;
        // Authentication Characteristic
        var authParamRead = new GattLocalCharacteristicParameters
        {
            CharacteristicProperties =
                GattCharacteristicProperties.Read,
            WriteProtectionLevel = GattProtectionLevel.Plain,
            ReadProtectionLevel = GattProtectionLevel.Plain,
            UserDescription = "Authentication Characteristic"
        };
        var authParamWrite = new GattLocalCharacteristicParameters
        {
            CharacteristicProperties = GattCharacteristicProperties.Read|
                GattCharacteristicProperties.Write,
            WriteProtectionLevel = GattProtectionLevel.Plain,
            ReadProtectionLevel = GattProtectionLevel.Plain,
            UserDescription = "Authentication Characteristic"
        };


        var authCharResult = await gattSvcProvider.Service.CreateCharacteristicAsync(
            Guid.Parse("00002A9E-0000-1000-8000-00805F9B34FB"),
            authParamRead);
        if (authCharResult.Error != Windows.Devices.Bluetooth.BluetoothError.Success)
        {
            Console.WriteLine("Failed to create Authentication Characteristic::" + authCharResult.Error);
            return;
        }
        authCharacteristicRead = authCharResult.Characteristic;
        gattServiceProvider.StartAdvertising(new GattServiceProviderAdvertisingParameters
        {
            IsDiscoverable = true,
            IsConnectable = true
        });
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
    private string storageFilePath = Path.Combine(
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
