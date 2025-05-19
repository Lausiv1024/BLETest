using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.Bluetooth;
using Android.Bluetooth.LE;
using Android.Content;
using Android.Runtime;
using BLETest.Settings;
using GattServerNative;
using Microsoft.Maui.Animations;
using Random = System.Random;
namespace GattClientNative.BLEServer;

internal class GattServer
{
    private readonly BluetoothManager bluetoothManager;
    private BluetoothAdapter bluetoothAdapter;
    private BluetoothGattServer bluetoothServer;
    private BluetoothGattCharacteristic characteristic;
    private BLEServerCallback bluetoothServerCallback;
    
    public GattServer(Context ctx)
    {
        bluetoothManager =(BluetoothManager)ctx.GetSystemService(Context.BluetoothService);
        bluetoothAdapter = bluetoothManager.Adapter;

        bluetoothServerCallback = new BLEServerCallback();
        bluetoothServerCallback.CharacteristicReadRequest += BluetoothServerCallback_CharacteristicReadRequest;
        bluetoothServerCallback.CharacteristicWriteRequest += BluetoothServerCallback_CharacteristicWriteRequest;
        bluetoothServerCallback.NotificationSent += BluetoothServerCallback_NotificationSent;
        bluetoothServer = bluetoothManager.OpenGattServer(ctx, bluetoothServerCallback);
        
        var bluetoothService = new BluetoothGattService(GattServerNative.Util.FromGuid(BLESettings.BLEMobileServerService), GattServiceType.Primary);
        characteristic = new BluetoothGattCharacteristic(GattServerNative.Util.FromGuid(BLESettings.BLEMobileServerCharacteristic),
            GattProperty.Read | GattProperty.Write | GattProperty.Notify,
            GattPermission.Read | GattPermission.Write);
        characteristic.AddDescriptor(new BluetoothGattDescriptor(GattServerNative.Util.FromGuid(Guid.Parse("0196ddba-55c5-725b-a6f4-ffec13f8e1d5")),
            GattDescriptorPermission.Read | GattDescriptorPermission.Write));

        bluetoothService.AddCharacteristic(characteristic);

        bluetoothServer.AddService(bluetoothService);

        Console.WriteLine("Server created");

        BluetoothLeAdvertiser advertiser = bluetoothAdapter.BluetoothLeAdvertiser;

        var builder = new AdvertiseSettings.Builder();
        builder.SetAdvertiseMode(AdvertiseMode.LowLatency);
        builder.SetConnectable(true);
        builder.SetTimeout(0);
        builder.SetTxPowerLevel(AdvertiseTx.PowerHigh);
        var dataBuilder = new AdvertiseData.Builder();
        dataBuilder.SetIncludeDeviceName(true);
        dataBuilder.SetIncludeTxPowerLevel(true);

        advertiser.StartAdvertising(builder.Build(), dataBuilder.Build(), new BleAdvertiseCallback());
    }

    private void BluetoothServerCallback_CharacteristicWriteRequest(object? sender, BleEventArgs e)
    {
        Console.WriteLine("Received Value : {0}", Encoding.UTF8.GetString(e.Value));
        bluetoothServer.SendResponse(e.Device, e.RequestId, GattStatus.Success, e.Offset, [0x00]);
    }

    public void NotifyData(byte[] data)
    {
        var devices = bluetoothManager.GetConnectedDevices(ProfileType.Gatt);
        if (devices.Count == 0)
            return;
        bluetoothServer.NotifyCharacteristicChanged(devices[0], characteristic, false, data);
    }

    private void BluetoothServerCallback_NotificationSent(object? sender, BleEventArgs e)
    {
        bluetoothServer.NotifyCharacteristicChanged(e.Device, characteristic, false);
    }

    int readRequestCount = 0;

    private void BluetoothServerCallback_CharacteristicReadRequest(object? sender, BleEventArgs e)
    {
        readRequestCount++;
        e.Characteristic.SetValue($"Value Updated : {readRequestCount}");
        Console.WriteLine("Read Requested");
        bluetoothServer.SendResponse(e.Device, e.RequestId, GattStatus.Success, e.Offset, e.Characteristic.GetValue());
    }

    public class BleAdvertiseCallback : AdvertiseCallback
    {
        public override void OnStartFailure([GeneratedEnum] AdvertiseFailure errorCode)
        {
            Console.WriteLine("Advertise start failure {0}", errorCode);
            base.OnStartFailure(errorCode);
        }

        public override void OnStartSuccess(AdvertiseSettings? settingsInEffect)
        {
            Console.WriteLine("Advertise Start Success {0}", settingsInEffect.Mode);
            base.OnStartSuccess(settingsInEffect);
        }
    }
}
