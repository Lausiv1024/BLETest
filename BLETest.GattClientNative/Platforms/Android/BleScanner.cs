using Android.Bluetooth;
using Android.Bluetooth.LE;
using Android.OS;
using Android.Runtime;

namespace BLETest.GattClientNative.Platforms.Android
{
    /// <summary>
    /// BLEデバイスのスキャナー
    /// </summary>
    public class BleScanner
    {
        private readonly BluetoothLeScanner? _scanner;
        private readonly ScanCallback _scanCallback;
        private bool _isScanning;

        public event EventHandler<BluetoothDevice>? DeviceFound;
        public List<BluetoothDevice> DiscoveredDevices { get; } = new List<BluetoothDevice>();

        public BleScanner(BluetoothAdapter adapter)
        {
            _scanner = adapter?.BluetoothLeScanner;
            _scanCallback = new BleScanCallback(this);
        }

        /// <summary>
        /// スキャンを開始
        /// </summary>
        public void StartScan()
        {
            StartScan(null);
        }

        /// <summary>
        /// サービスUUIDでフィルタリングしてスキャンを開始
        /// </summary>
        /// <param name="serviceUuid">フィルタリングするサービスUUID（nullの場合はフィルタなし）</param>
        public void StartScan(Guid? serviceUuid)
        {
            if (_scanner == null || _isScanning)
            {
                return;
            }

            DiscoveredDevices.Clear();
            _isScanning = true;

            var settings = new ScanSettings.Builder()
                .SetScanMode(global::Android.Bluetooth.LE.ScanMode.LowLatency)
                .Build();

            List<ScanFilter>? filters = null;
            if (serviceUuid.HasValue)
            {
                filters = new List<ScanFilter>
                {
                    new ScanFilter.Builder()
                        .SetServiceUuid(ParcelUuid.FromString(serviceUuid.Value.ToString()))
                        .Build()
                };
            }

            _scanner.StartScan(filters, settings, _scanCallback);
        }

        /// <summary>
        /// スキャンを停止
        /// </summary>
        public void StopScan()
        {
            if (_scanner == null || !_isScanning)
            {
                return;
            }

            _scanner.StopScan(_scanCallback);
            _isScanning = false;
        }

        private void OnDeviceFound(BluetoothDevice device)
        {
            if (!DiscoveredDevices.Any(d => d.Address == device.Address))
            {
                DiscoveredDevices.Add(device);
                DeviceFound?.Invoke(this, device);
            }
        }

        private class BleScanCallback : ScanCallback
        {
            private readonly BleScanner _scanner;

            public BleScanCallback(BleScanner scanner)
            {
                _scanner = scanner;
            }

            public override void OnScanResult([GeneratedEnum] ScanCallbackType callbackType, ScanResult? result)
            {
                base.OnScanResult(callbackType, result);

                if (result?.Device != null)
                {
                    _scanner.OnDeviceFound(result.Device);
                }
            }

            public override void OnBatchScanResults(IList<ScanResult>? results)
            {
                base.OnBatchScanResults(results);

                if (results != null)
                {
                    foreach (var result in results)
                    {
                        if (result?.Device != null)
                        {
                            _scanner.OnDeviceFound(result.Device);
                        }
                    }
                }
            }

            public override void OnScanFailed([GeneratedEnum] ScanFailure errorCode)
            {
                base.OnScanFailed(errorCode);
                System.Diagnostics.Debug.WriteLine($"Scan failed: {errorCode}");
            }
        }
    }
}
