using Android.Bluetooth;
using Android.Content;
using BLETest.GattClientNative.Services;

namespace BLETest.GattClientNative.Platforms.Android;

public class BleService(Guid serviceUuid, Guid writeCharacteristicUuid, Guid notifyCharacteristicUuid, Guid authCharacteristicWriteUuid, Guid authCharacteristicReadUuid) : IBleService
{
    private readonly Context _context = global::Android.App.Application.Context;
    private readonly Guid _serviceUuid = serviceUuid;
    private readonly Guid _writeCharacteristicUuid = writeCharacteristicUuid;
    private readonly Guid _notifyCharacteristicUuid = notifyCharacteristicUuid;
    private readonly Guid _authCharacteristicWriteUuid = authCharacteristicWriteUuid;
    private readonly Guid _authCharacteristicReadUuid = authCharacteristicReadUuid;
    private BluetoothAdapter? _bluetoothAdapter;
    private BleScanner? _scanner;
    private BleGattClient? _gattClient;

    public event EventHandler<string>? MessageReceived;
    public event EventHandler<string>? ConnectionStateChanged;
    public event EventHandler<bool>? AuthenticationCompleted;
    public event EventHandler<byte[]>? AuthenticationDataReceived;
    public bool IsConnected => _gattClient?.IsConnected ?? false;

    public Task InitializeAsync()
    {
        var bluetoothManager = (BluetoothManager?)_context.GetSystemService(Context.BluetoothService);
        _bluetoothAdapter = bluetoothManager?.Adapter;

        if (_bluetoothAdapter == null)
        {
            throw new Exception("Bluetooth adapter not found");
        }

        _scanner = new BleScanner(_bluetoothAdapter);
        return Task.CompletedTask;
    }

    public async Task<List<BleDeviceInfo>> ScanDevicesAsync(int scanDurationSeconds = 10)
    {
        if (_scanner == null)
        {
            throw new InvalidOperationException("Scanner not initialized. Call InitializeAsync first.");
        }

        _scanner.StartScan();
        await Task.Delay(scanDurationSeconds * 1000);
        _scanner.StopScan();

        return _scanner.DiscoveredDevices
            .Select(d => new BleDeviceInfo
            {
                Name = d.Name ?? "Unknown",
                Address = d.Address ?? string.Empty
            })
            .ToList();
    }

    public async Task<bool> ScanAndConnectAsync(int scanDurationSeconds = 10)
    {
        if (_scanner == null)
        {
            throw new InvalidOperationException("Scanner not initialized. Call InitializeAsync first.");
        }

        // サービスUUIDでフィルタリングしてスキャン開始
        _scanner.StartScan(_serviceUuid);

        // デバイスが見つかったら自動接続
        var tcs = new TaskCompletionSource<bool>();
        var timeout = Task.Delay(scanDurationSeconds * 1000);

        EventHandler<BluetoothDevice>? handler = null;
        handler = async (sender, device) =>
        {
            // スキャンを停止
            _scanner.StopScan();
            _scanner.DeviceFound -= handler;

            // 接続を試みる
            var connected = await ConnectAsync(device.Address ?? string.Empty);
            tcs.TrySetResult(connected);
        };

        _scanner.DeviceFound += handler;

        // タイムアウトまたはデバイス発見を待つ
        var completedTask = await Task.WhenAny(tcs.Task, timeout);

        if (completedTask == timeout)
        {
            // タイムアウト
            _scanner.DeviceFound -= handler;
            _scanner.StopScan();
            return false;
        }

        return await tcs.Task;
    }

    public Task<bool> ConnectAsync(string deviceAddress)
    {
        if (_bluetoothAdapter == null)
        {
            return Task.FromResult(false);
        }

        var device = _bluetoothAdapter.GetRemoteDevice(deviceAddress);
        if (device == null)
        {
            return Task.FromResult(false);
        }

        _gattClient = new BleGattClient(_context, _serviceUuid, _writeCharacteristicUuid, _notifyCharacteristicUuid, _authCharacteristicWriteUuid, _authCharacteristicReadUuid);
        _gattClient.MessageReceived += (s, e) => MessageReceived?.Invoke(this, e);
        _gattClient.ConnectionStateChanged += (s, e) => ConnectionStateChanged?.Invoke(this, e);
        _gattClient.AuthenticationCompleted += (s, e) => AuthenticationCompleted?.Invoke(this, e);
        _gattClient.AuthenticationDataReceived += (s, e ) => AuthenticationDataReceived?.Invoke(this, e);

        _gattClient.Connect(device);
        return Task.FromResult(true);
    }

    public Task DisconnectAsync()
    {
        _gattClient?.Disconnect();
        _gattClient = null;
        return Task.CompletedTask;
    }

    public async Task<bool> WriteTextAsync(string text)
    {
        if (_gattClient == null)
        {
            return false;
        }

        return await _gattClient.WriteTextAsync(text);
    }

    public async Task<bool> WriteByteAsync(byte[] data)
    {
        if (_gattClient == null)
        {
            return false;
        }
        return await _gattClient.WriteByteAsnyc(data);
    }

    public async Task<bool> WriteAuthenticationDataAsync(byte[] data)
    {
        if (_gattClient == null)
        {
            return false;
        }
        return await _gattClient.WriteAuthenticationDataAsync(data);
    }

    public async Task ReadAuthenticationDataAsync()
    {
        if (_gattClient == null)
        {
            throw new InvalidOperationException("Not connected to any device.");
        }

        await _gattClient.ReadAuthenticationDataAsync();
    }
}
