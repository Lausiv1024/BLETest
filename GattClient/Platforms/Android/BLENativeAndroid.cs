using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.Bluetooth;
using Android.Bluetooth.LE;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Plugin.BLE.Android.CallbackEventArgs;

namespace GattClient
{
    public class BLENativeAndroid
    {

        public BluetoothAdapter BluetoothAdapter { get; } = BLENativeManager.Current.Adapter;

        public BLENativeAndroid(Guid serviceId, Guid characteristicId)
        {

        }
    }

    /// <summary>
    /// ここにBluetoothAdapterを格納する。
    /// </summary>
    public class BLENativeManager
    {
        public static BLENativeManager Current { get { return c; } }
        private static BLENativeManager c;
        public BluetoothAdapter Adapter { get; }
        public BLENativeManager(BluetoothAdapter adapter)
        {

            Adapter = adapter;
            if (c != null) //2回目の呼び出しはとりあえず許可しない
            {
                throw new InvalidOperationException("Current is Already Initialized");
            }
            c = this;
        }
    }

    public class BLEServiceScannerNativeAndroid
    {
        private bool isScanning = false;
        private BluetoothLeScanner scanner;
        private Handler handler = new Handler();
        private ScanCallback callback = new BLEScanCallback();
        public ReadOnlySpan<BluetoothDevice> Devices => ((BLEScanCallback)callback).Devices;
        public BLEServiceScannerNativeAndroid()
        {
            scanner = BLENativeManager.Current.Adapter.BluetoothLeScanner;
        }
        private　async Task scanLeDevice()
        {
            if (!isScanning)
            {
                isScanning = true;
                handler.PostDelayed(() =>
                {
                    //デバイスのスキャンを10秒後に停止
                    isScanning = false;
                    scanner.StopScan(callback);
                }, 10000);
                //デバイスのスキャンを開始
                scanner.StartScan(new List<ScanFilter>(), new ScanSettings.Builder().Build(), callback);
                while (isScanning)
                {
                    await Task.Delay(100); //スキャンが終わるまで待機
                }
            } else
            {
                isScanning = false;
                scanner.StopScan(callback);
            }
        }

        private class BLEScanCallback : ScanCallback
        {
            private List<BluetoothDevice> devices = new List<BluetoothDevice>();
            public ReadOnlySpan<BluetoothDevice> Devices => devices.ToArray();
            public override void OnScanResult([GeneratedEnum] ScanCallbackType callbackType, ScanResult result)
            {
                base.OnScanResult(callbackType, result);
                devices.Add(result.Device);
            }
        }
    }
}