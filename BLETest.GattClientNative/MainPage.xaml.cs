using BLETest.GattClientNative.Services;

namespace BLETest.GattClientNative
{
    public partial class MainPage : ContentPage
    {
        private readonly IBleService _bleService;

        public MainPage(IBleService bleService)
        {
            InitializeComponent();
            _bleService = bleService;

            // イベントハンドラーを登録
            _bleService.MessageReceived += OnMessageReceived;
            _bleService.ConnectionStateChanged += OnConnectionStateChanged;

            // 初期化
            InitializeBleAsync();
        }

        private async void InitializeBleAsync()
        {
            try
            {
                await _bleService.InitializeAsync();

                // パーミッションをリクエスト
                await RequestPermissionsAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to initialize BLE: {ex.Message}", "OK");
            }
        }

        private async Task RequestPermissionsAsync()
        {
            // Bluetooth権限
            var bluetoothStatus = await Permissions.CheckStatusAsync<Permissions.Bluetooth>();
            if (bluetoothStatus != PermissionStatus.Granted)
            {
                bluetoothStatus = await Permissions.RequestAsync<Permissions.Bluetooth>();
                if (bluetoothStatus != PermissionStatus.Granted)
                {
                    await DisplayAlert("Permission Required", "Bluetooth permission is required", "OK");
                    return;
                }
            }

            // 位置情報権限 (Androidの場合、BLEスキャンに必要)
            var locationStatus = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (locationStatus != PermissionStatus.Granted)
            {
                locationStatus = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                if (locationStatus != PermissionStatus.Granted)
                {
                    await DisplayAlert("Permission Required", "Location permission is required for BLE scanning", "OK");
                }
            }
        }

        private async void OnAutoConnectButtonClicked(object? sender, EventArgs e)
        {
            try
            {
                AutoConnectButton.IsEnabled = false;
                ScanButton.IsEnabled = false;
                AutoConnectButton.Text = "Scanning...";

                var connected = await _bleService.ScanAndConnectAsync(16);

                if (!connected)
                {
                    await DisplayAlert("Connection Failed", "No device with the specified service found or connection failed", "OK");
                }
                // 接続成功の場合は OnConnectionStateChanged イベントでUI更新される
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Auto connect failed: {ex.Message}", "OK");
            }
            finally
            {
                AutoConnectButton.IsEnabled = true;
                ScanButton.IsEnabled = true;
                AutoConnectButton.Text = "Scan and Auto Connect";
            }
        }

        private async void OnScanButtonClicked(object? sender, EventArgs e)
        {
            try
            {
                AutoConnectButton.IsEnabled = false;
                ScanButton.IsEnabled = false;
                ScanButton.Text = "Scanning...";

                var devices = await _bleService.ScanDevicesAsync(16);

                if (devices.Count > 0)
                {
                    DevicesCollectionView.ItemsSource = devices;
                    DevicesCollectionView.IsVisible = true;
                    DevicesLabel.IsVisible = true;
                }
                else
                {
                    await DisplayAlert("No Devices", "No BLE devices found", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Scan failed: {ex.Message}", "OK");
            }
            finally
            {
                AutoConnectButton.IsEnabled = true;
                ScanButton.IsEnabled = true;
                ScanButton.Text = "Scan Devices (Manual)";
            }
        }

        private async void OnDeviceSelected(object? sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is BleDeviceInfo selectedDevice)
            {
                try
                {
                    var result = await _bleService.ConnectAsync(selectedDevice.Address);
                    if (!result)
                    {
                        await DisplayAlert("Connection Failed", "Failed to connect to device", "OK");
                    }
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Error", $"Connection error: {ex.Message}", "OK");
                }

                // 選択を解除
                DevicesCollectionView.SelectedItem = null;
            }
        }

        private async void OnDisconnectButtonClicked(object? sender, EventArgs e)
        {
            try
            {
                await _bleService.DisconnectAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Disconnect error: {ex.Message}", "OK");
            }
        }

        private async void OnSendButtonClicked(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(MessageEntry.Text))
            {
                await DisplayAlert("Invalid Input", "Please enter a message", "OK");
                return;
            }
            try
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();

                var success = await _bleService.WriteTextAsync(MessageEntry.Text);
                sw.Stop();
                System.Diagnostics.Debug.WriteLine($"WriteTextAsync took {sw.ElapsedMilliseconds} ms");
                if (success)
                {
                    // 送信成功したら入力欄をクリア
                    MessageEntry.Text = string.Empty;
                }
                else
                {
                    await DisplayAlert("Send Failed", "Failed to send message", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Send error: {ex.Message}", "OK");
            }
        }

        private void OnMessageReceived(object? sender, string message)
        {
            // UIスレッドで実行
            MainThread.BeginInvokeOnMainThread(() =>
            {
                ReceivedMessagesLabel.Text += $"{DateTime.Now:HH:mm:ss}: {message}\n";
            });
        }

        private void OnConnectionStateChanged(object? sender, string state)
        {
            // UIスレッドで実行
            MainThread.BeginInvokeOnMainThread(() =>
            {
                ConnectionStatusLabel.Text = state;

                switch (state)
                {
                    case "Connected":
                        ConnectionStatusLabel.TextColor = Colors.Orange;
                        break;
                    case "Ready":
                        ConnectionStatusLabel.TextColor = Colors.Green;
                        SendButton.IsEnabled = true;
                        RealtimeStateSend.IsEnabled = true;
                        DisconnectButton.IsVisible = true;
                        DevicesCollectionView.IsVisible = false;
                        DevicesLabel.IsVisible = false;
                        ScanButton.IsVisible = false;
                        AutoConnectButton.IsVisible = false;
                        ReceivedMessagesLabel.Text += $"{DateTime.Now:HH:mm:ss}: Connected and ready\n";
                        break;
                    case "Disconnected":
                        ConnectionStatusLabel.TextColor = Colors.Red;
                        SendButton.IsEnabled = false;
                        RealtimeStateSend.IsEnabled = false;
                        DisconnectButton.IsVisible = false;
                        ScanButton.IsVisible = true;
                        AutoConnectButton.IsVisible = true;
                        break;
                }
            });
        }

        private void RealtimeStateSend_Pressed(object sender, EventArgs e)
        {
            _bleService.WriteByteAsync([0xFF, 0x80]);
        }

        private void RealtimeStateSend_Released(object sender, EventArgs e)
        {
            _bleService.WriteByteAsync([0xFF, 0x81]);
        }
    }
}
