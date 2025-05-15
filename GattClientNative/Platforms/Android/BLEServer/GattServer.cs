using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.Bluetooth;
using Android.Bluetooth.LE;
using Android.Content;
using BLETest.Settings;
using Random = System.Random;
namespace GattServerNative.Platforms.Android.BLEServer
{
    internal class GattServer
    {
        private readonly BluetoothManager bluetoothManager;
        private BluetoothAdapter bluetoothAdapter;
        private BluetoothGattServer bluetoothServer;
        private BluetoothGattCharacteristic characteristic;
        
        public GattServer(Context ctx)
        {
            bluetoothManager =(BluetoothManager) ctx.GetSystemService(Context.BluetoothService);
            bluetoothAdapter = bluetoothManager.Adapter;
            
            var bluetoothService = new BluetoothGattService(Util.FromGuid(BLESettings.BLEMobileServerService), GattServiceType.Primary);
            characteristic = new BluetoothGattCharacteristic(Util.FromGuid(BLESettings.BLEMobileServerCharacteristic),
                GattProperty.Read | GattProperty.Write | GattProperty.Notify,
                GattPermission.Read | GattPermission.Write);
            
        }
    }
}
