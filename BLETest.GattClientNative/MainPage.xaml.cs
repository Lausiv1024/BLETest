using BLETest.Common.ComModel;
using BLETest.GattClientNative.Services;
using MessagePack;
using System.Threading.Tasks;

namespace BLETest.GattClientNative;

public partial class MainPage : ContentPage
{
    private readonly IBleService _bleService;
    private readonly IAuthenticationService _authService;
    private readonly IServiceProvider _serviceProvider;
    DateTime sentTime = DateTime.MinValue;

    public MainPage(IBleService bleService, IAuthenticationService authService, IServiceProvider serviceProvider)
    {
        InitializeComponent();
        _bleService = bleService;
        _authService = authService;
        _serviceProvider = serviceProvider;

        // イベントハンドラーを登録
        _bleService.MessageReceived += OnMessageReceived;
        _bleService.ConnectionStateChanged += OnConnectionStateChanged;
        _bleService.AuthenticationCompleted += OnAuthenticationCompleted;
        _bleService.AuthenticationDataReceived += _bleService_AuthenticationDataReceived;

        // 初期化
        InitializeBleAsync();
        UpdateAuthStatus();
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
            UpdateConnectionState("Disconnected");
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
            sentTime = DateTime.Now;
            for (int i = 0; i < 2; i++) 
            {
                var success = await _bleService.WriteTextAsync(MessageEntry.Text);

                if (success)
                {
                    // 送信成功したら入力欄をクリア
                    //MessageEntry.Text = string.Empty;
                } else
                {
                    await DisplayAlert("Send Failed", "Failed to send message", "OK");
                }
                await Task.Delay(100); //100ms遅延であえて2回送信
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
            UpdateConnectionState(state);
        });
    }

    private async void UpdateConnectionState(string state)
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

                // QRコードがスキャン済みで未登録の場合、自動的に認証を実行
                if (!string.IsNullOrEmpty(_authService.ServerDeviceId) && !_authService.IsRegistered)
                {
                    ReceivedMessagesLabel.Text += $"{DateTime.Now:HH:mm:ss}: 認証を開始します...\n";
                    await Task.Delay(500); // 少し待機してから認証
                    OnRegisterDeviceButtonClicked();
                }
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
    }

    private void RealtimeStateSend_Pressed(object sender, EventArgs e)
    {
        _bleService.WriteByteAsync([0xFF, 0x80]);
    }

    private void RealtimeStateSend_Released(object sender, EventArgs e)
    {
        _bleService.WriteByteAsync([0xFF, 0x81]);
    }

    private async void ContentPage_Disappearing(object sender, EventArgs e)
    {
        await _bleService.DisconnectAsync();
    }

    private void UpdateAuthStatus()
    {
        if (_authService.IsRegistered)
        {
            AuthStatusLabel.Text = $"登録済み: {_authService.ServerDeviceId?.Substring(0, 8)}...";
            AuthStatusFrame.BackgroundColor = Color.FromArgb("#D4EDDA");
            AuthStatusFrame.BorderColor = Color.FromArgb("#28A745");
            ScanQRButton.Text = "再登録";
        }
        else
        {
            AuthStatusLabel.Text = "未登録";
            AuthStatusFrame.BackgroundColor = Color.FromArgb("#FFF3CD");
            AuthStatusFrame.BorderColor = Color.FromArgb("#FFC107");
            ScanQRButton.Text = "QRスキャン";
        }
    }

    private async void _bleService_AuthenticationDataReceived(object? sender, byte[] e)
    {
        var comData = MessagePackSerializer.Deserialize<CommunicationBase>(e);
        if (comData == null)
            return;
        if (comData.Command == CommandType.DeviceNewResult)
        {
            var result = MessagePackSerializer.Deserialize<DeviceNewResult>(e);
            await OnInitialSettingCompleted(result.IsSuccess);
        }
    }

    private async void OnScanQRButtonClicked(object? sender, EventArgs e)
    {
        try
        {
            // カメラ権限をリクエスト
            var cameraStatus = await Permissions.CheckStatusAsync<Permissions.Camera>();
            if (cameraStatus != PermissionStatus.Granted)
            {
                cameraStatus = await Permissions.RequestAsync<Permissions.Camera>();
                if (cameraStatus != PermissionStatus.Granted)
                {
                    await DisplayAlert("Permission Required", "Camera permission is required for QR scanning", "OK");
                    return;
                }
            }

            // QRスキャンページを開く
            var qrScanPage = _serviceProvider.GetRequiredService<QRScanPage>();
            qrScanPage.OnQRCodeScanned += async (deviceId, publicKey) =>
            {
                await _authService.ClearCredentialsAsync();
                // サーバー情報を設定
                _authService.SetServerInfo(deviceId, publicKey);

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    ReceivedMessagesLabel.Text += $"{DateTime.Now:HH:mm:ss}: QRコード読み取り成功 - DeviceId: {deviceId.Substring(0, 8)}...\n";
                    UpdateAuthStatus();
                });
            };

            await Navigation.PushAsync(qrScanPage);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"QR scan failed: {ex.Message}", "OK");
        }
    }

    private async void OnRegisterDeviceButtonClicked()
    {
        if (!_bleService.IsConnected)
        {
            await DisplayAlert("Error", "デバイスに接続してから認証を実行してください", "OK");
            return;
        }

        if (string.IsNullOrEmpty(_authService.ServerDeviceId))
        {
            await DisplayAlert("Error", "先にQRコードをスキャンしてください", "OK");
            return;
        }

        try
        {
            // 認証データを作成して送信
            var authData = await _authService.CreateAuthenticationDataAsync();
            var success = await _bleService.WriteAuthenticationDataAsync(authData);

            if (!success)
            {
                await DisplayAlertAsync("Error", "認証データの送信に失敗しました", "OK");
                return;
            }

            await Task.Delay(100);
            await _bleService.ReadAuthenticationDataAsync();
        } catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Authentication failed: {ex.Message}", "OK");
        }
    }

    private async Task OnInitialSettingCompleted(bool success)
    {
        MainThread.BeginInvokeOnMainThread(async() =>
        {
            if (success)
            {
                await _authService.SaveCredentialsAsync();
                ReceivedMessagesLabel.Text += $"{DateTime.Now:HH:mm:ss}: 認証データ送信成功\n";
                UpdateAuthStatus();
            } else
            {
                await DisplayAlertAsync("Error", "認証データの送信に失敗しました", "OK");
            }
        });
    }

    private void OnAuthenticationCompleted(object? sender, bool success)
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            //if (success)
            //{
            //    ReceivedMessagesLabel.Text += $"{DateTime.Now:HH:mm:ss}: 認証完了\n";
            //    await _authService.SaveCredentialsAsync();
            //    UpdateAuthStatus();
            //}
            //else
            //{
            //    ReceivedMessagesLabel.Text += $"{DateTime.Now:HH:mm:ss}: 認証失敗\n";
            //}
        });
    }
}
