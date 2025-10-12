using Android.Bluetooth;
using Android.Content;
using Android.Runtime;
using System.Text;

namespace BLETest.GattClientNative.Platforms.Android
{
    /// <summary>
    /// AndroidネイティブのBLE Gattクライアント実装
    /// </summary>
    public class BleGattClient
    {
        private BluetoothGatt? _bluetoothGatt;
        private BluetoothGattCharacteristic? _characteristic;
        private readonly Context _context;
        private readonly Guid _serviceUuid;
        private readonly Guid _characteristicUuid;
        private GattCallback? _gattCallback;

        public event EventHandler<string>? MessageReceived;
        public event EventHandler<string>? ConnectionStateChanged;
        public bool IsConnected { get; private set; }

        public BleGattClient(Context context, Guid serviceUuid, Guid characteristicUuid)
        {
            _context = context;
            _serviceUuid = serviceUuid;
            _characteristicUuid = characteristicUuid;
        }

        /// <summary>
        /// BLEデバイスに接続
        /// </summary>
        public void Connect(BluetoothDevice device)
        {
            _gattCallback = new GattCallback(this);
            _bluetoothGatt = device.ConnectGatt(_context, false, _gattCallback);
        }

        /// <summary>
        /// BLEデバイスから切断
        /// </summary>
        public void Disconnect()
        {
            _bluetoothGatt?.Disconnect();
            _bluetoothGatt?.Close();
            _bluetoothGatt = null;
            IsConnected = false;
        }

        public async Task<bool> WriteByteAsnyc(byte[] data)
        {
            if (_bluetoothGatt == null || _characteristic == null || !IsConnected)
                return false;

            try
            {
                if (global::Android.OS.Build.VERSION.SdkInt >= global::Android.OS.BuildVersionCodes.Tiramisu)
                {
                    var result = _bluetoothGatt.WriteCharacteristic(_characteristic, data,(int) GattWriteType.Default);
                    System.Diagnostics.Debug.WriteLine($"WriteCharacteristic result: {result}");
                    return result == 0;
                } else
                {
                    // 古いAPIでは setValue してから writeCharacteristic を呼ぶ
#pragma warning disable CS0618 // 型またはメンバーが旧型式です
                    _characteristic.SetValue(data);
                    return _bluetoothGatt.WriteCharacteristic(_characteristic);
#pragma warning restore CS0618
                }
            } catch(Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"WriteTextAsync error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// UTF-8テキストを送信
        /// </summary>
        public async Task<bool> WriteTextAsync(string text)
        {
            if (_bluetoothGatt == null || _characteristic == null || !IsConnected)
            {
                return false;
            }

            return await WriteByteAsnyc(Encoding.UTF8.GetBytes(text));
        }

        private void OnConnectionStateChange(BluetoothGatt gatt, GattStatus status, ProfileState newState)
        {
            if (newState == ProfileState.Connected)
            {
                IsConnected = true;
                ConnectionStateChanged?.Invoke(this, "Connected");
                // サービスの検索を開始
                gatt.DiscoverServices();
            }
            else if (newState == ProfileState.Disconnected)
            {
                IsConnected = false;
                ConnectionStateChanged?.Invoke(this, "Disconnected");
            }
        }

        private void OnServicesDiscovered(BluetoothGatt gatt, GattStatus status)
        {
            if (status == GattStatus.Success)
            {
                var service = gatt.GetService(Java.Util.UUID.FromString(_serviceUuid.ToString()));
                if (service != null)
                {
                    _characteristic = service.GetCharacteristic(Java.Util.UUID.FromString(_characteristicUuid.ToString()));
                    if (_characteristic != null)
                    {
                        // Notificationを有効化
                        gatt.SetCharacteristicNotification(_characteristic, true);

                        // CCCDを設定してNotificationを有効化
                        var descriptor = _characteristic.GetDescriptor(
                            Java.Util.UUID.FromString("00002902-0000-1000-8000-00805f9b34fb")); // Client Characteristic Configuration Descriptor

                        if (descriptor != null)
                        {
                            if (global::Android.OS.Build.VERSION.SdkInt >= global::Android.OS.BuildVersionCodes.Tiramisu)
                            {
                                gatt.WriteDescriptor(descriptor, BluetoothGattDescriptor.EnableNotificationValue!.ToArray()!);
                            }
                            else
                            {
                                #pragma warning disable CS0618
                                descriptor.SetValue(BluetoothGattDescriptor.EnableNotificationValue!.ToArray());
                                gatt.WriteDescriptor(descriptor);
                                #pragma warning restore CS0618
                            }
                        }

                        ConnectionStateChanged?.Invoke(this, "Ready");
                    }
                }
            }
        }

        private void OnCharacteristicChanged(BluetoothGatt gatt, BluetoothGattCharacteristic characteristic)
        {
            byte[]? data = null;

            if (global::Android.OS.Build.VERSION.SdkInt >= global::Android.OS.BuildVersionCodes.Tiramisu)
            {
                data = characteristic.GetValue();
            }
            else
            {
                #pragma warning disable CS0618
                data = characteristic.GetValue();
                #pragma warning restore CS0618
            }

            if (data != null && data.Length > 0)
            {
                string text = Encoding.UTF8.GetString(data);
                MessageReceived?.Invoke(this, text);
            }
        }

        /// <summary>
        /// BluetoothGattCallbackの実装
        /// </summary>
        private class GattCallback : BluetoothGattCallback
        {
            private readonly BleGattClient _client;

            public GattCallback(BleGattClient client)
            {
                _client = client;
            }

            public override void OnConnectionStateChange(BluetoothGatt? gatt, [GeneratedEnum] GattStatus status, [GeneratedEnum] ProfileState newState)
            {
                if (gatt != null)
                {
                    _client.OnConnectionStateChange(gatt, status, newState);
                }
            }

            public override void OnServicesDiscovered(BluetoothGatt? gatt, [GeneratedEnum] GattStatus status)
            {
                if (gatt != null)
                {
                    _client.OnServicesDiscovered(gatt, status);
                }
            }

            public override void OnCharacteristicChanged(BluetoothGatt? gatt, BluetoothGattCharacteristic? characteristic)
            {
                if (gatt != null && characteristic != null)
                {
                    _client.OnCharacteristicChanged(gatt, characteristic);
                }
            }
        }
    }
}
