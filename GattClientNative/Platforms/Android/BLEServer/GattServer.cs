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
using Random = System.Random;
namespace GattServerNative.Platforms.Android.BLEServer;

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
        bluetoothServerCallback = new();
        bluetoothServer = bluetoothManager.OpenGattServer(ctx, bluetoothServerCallback);
        
        var bluetoothService = new BluetoothGattService(Util.FromGuid(BLESettings.BLEMobileServerService), GattServiceType.Primary);
        characteristic = new BluetoothGattCharacteristic(Util.FromGuid(BLESettings.BLEMobileServerCharacteristic),
            GattProperty.Read | GattProperty.Write | GattProperty.Notify,
            GattPermission.Read | GattPermission.Write);
        bluetoothService.AddCharacteristic(characteristic);
        bluetoothServer.AddService(bluetoothService);

        bluetoothServerCallback.CharacteristicReadRequest += BluetoothServerCallback_CharacteristicReadRequest;
        bluetoothServerCallback.NotificationSent += BluetoothServerCallback_NotificationSent;

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

    private void BluetoothServerCallback_NotificationSent(object? sender, BleEventArgs e)
    {
        throw new NotImplementedException();
    }

    private void BluetoothServerCallback_CharacteristicReadRequest(object? sender, BleEventArgs e)
    {
        throw new NotImplementedException();
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
