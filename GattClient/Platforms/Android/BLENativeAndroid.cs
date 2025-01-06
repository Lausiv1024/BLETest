using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.Bluetooth;
using Android.Bluetooth.LE;
using Android.Content;
using Android.OS;
using Plugin.BLE.Android.CallbackEventArgs;

namespace GattClient
{
    public class BLENativeAndroid
    {

        public BluetoothAdapter BluetoothAdapter { get; }

        public BLENativeAndroid(Guid serviceId, Guid characteristicId)
        {
            
        }
    }

    /// <summary>
    /// ここにBluetoothAdapterを格納する。
    /// </summary>
    public class BLENativeManager
    {
        public static BLENativeManager Current { get {return c; } }
        private static BLENativeManager c;
        public BluetoothAdapter BluetoothAdapter { get; }
        public BLENativeManager(BluetoothAdapter adapter){

            BluetoothAdapter = adapter;
            if (c != null) //2回目の呼び出しはとりあえず許可しない
            {
                throw new InvalidOperationException("Current is Already Initialized");
            }
            c = this;
        }
    }

    public class BLEServiceScannerNativeAndroid
    {
        public BLEServiceScannerNativeAndroid()
        {
            
        }
        private void scanLeDevice()
        {

        }
    }
}
