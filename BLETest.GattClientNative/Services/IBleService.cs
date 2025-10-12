namespace BLETest.GattClientNative.Services
{
    public interface IBleService
    {
        event EventHandler<string>? MessageReceived;
        event EventHandler<string>? ConnectionStateChanged;
        bool IsConnected { get; }

        Task InitializeAsync();
        Task<List<BleDeviceInfo>> ScanDevicesAsync(int scanDurationSeconds = 10);
        Task<bool> ScanAndConnectAsync(int scanDurationSeconds = 10);
        Task<bool> ConnectAsync(string deviceAddress);
        Task DisconnectAsync();
        Task<bool> WriteTextAsync(string text);
        Task<bool> WriteByteAsync(byte[] data);
    }

    public class BleDeviceInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
    }
}
