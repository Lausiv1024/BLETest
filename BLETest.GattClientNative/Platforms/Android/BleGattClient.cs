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
        private BluetoothGattCharacteristic? _writeCharacteristic;
        private BluetoothGattCharacteristic? _notifyCharacteristic;
        private readonly Context _context;
        private readonly Guid _serviceUuid;
        private readonly Guid _writeCharacteristicUuid;
        private readonly Guid _notifyCharacteristicUuid;
        private GattCallback? _gattCallback;

        public event EventHandler<string>? MessageReceived;
        public event EventHandler<string>? ConnectionStateChanged;
        public bool IsConnected { get; private set; }

        public BleGattClient(Context context, Guid serviceUuid, Guid writeCharacteristicUuid, Guid notifyCharacteristicUuid)
        {
            _context = context;
            _serviceUuid = serviceUuid;
            _writeCharacteristicUuid = writeCharacteristicUuid;
            _notifyCharacteristicUuid = notifyCharacteristicUuid;
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
            if (_bluetoothGatt == null || _writeCharacteristic == null || !IsConnected)
                return false;

            try
            {
                if (global::Android.OS.Build.VERSION.SdkInt >= global::Android.OS.BuildVersionCodes.Tiramisu)
                {
                    var result = _bluetoothGatt.WriteCharacteristic(_writeCharacteristic, data,(int) GattWriteType.NoResponse);
                    System.Diagnostics.Debug.WriteLine($"WriteCharacteristic result: {result}");
                    return result == 0;
                } else
                {
                    // 古いAPIでは setValue してから writeCharacteristic を呼ぶ
#pragma warning disable CS0618 // 型またはメンバーが旧型式です
                    _writeCharacteristic.SetValue(data);
                    return _bluetoothGatt.WriteCharacteristic(_writeCharacteristic);
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
            if (_bluetoothGatt == null || _notifyCharacteristic == null || _writeCharacteristic == null || !IsConnected)
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
                    // Write用キャラクタリスティックを取得
                    _writeCharacteristic = service.GetCharacteristic(Java.Util.UUID.FromString(_writeCharacteristicUuid.ToString()));

                    // Notify用キャラクタリスティックを取得
                    _notifyCharacteristic = service.GetCharacteristic(Java.Util.UUID.FromString(_notifyCharacteristicUuid.ToString()));

                    if (_writeCharacteristic != null && _notifyCharacteristic != null)
                    {
                        // Notificationを有効化
                        gatt.SetCharacteristicNotification(_notifyCharacteristic, true);

                        // CCCDを設定してNotificationを有効化
                        var descriptor = _notifyCharacteristic.GetDescriptor(
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
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"Characteristics not found - Write: {_writeCharacteristic != null}, Notify: {_notifyCharacteristic != null}");
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
