using ZXing.Net.Maui;

namespace BLETest.GattClientNative;

public partial class QRScanPage : ContentPage
{
    private bool _isProcessing = false;

    public event Action<string, byte[]>? OnQRCodeScanned;

    public QRScanPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        CheckPermissions().ConfigureAwait(false);
        _isProcessing = false;
        BarcodeReader.IsDetecting = true;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        BarcodeReader.IsDetecting = false;
    }

    private async Task CheckPermissions()
    {
        var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
        if (status != PermissionStatus.Granted)
        {
            status = await Permissions.RequestAsync<Permissions.Camera>();
            if (status != PermissionStatus.Granted)
            {
                await DisplayAlertAsync("権限エラー", "カメラの使用が許可されていません。設定からカメラの権限を許可してください。", "OK");
                await Navigation.PopAsync();
            }
        }
    }

    private void BarcodeReader_BarcodesDetected(object? sender, BarcodeDetectionEventArgs e)
    {
        if (_isProcessing) return;

        var result = e.Results.FirstOrDefault();
        if (result == null) return;

        _isProcessing = true;
        BarcodeReader.IsDetecting = false;

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            try
            {
                // QRコードをパース: "DeviceId,Base64PublicKey"
                var qrValue = result.Value;
                var parts = qrValue.Split(',');

                if (parts.Length >= 2)
                {
                    var deviceId = parts[0];
                    var publicKeyBase64 = parts[1];

                    try
                    {
                        var publicKey = Convert.FromBase64String(publicKeyBase64);
                        StatusLabel.Text = $"デバイスID: {deviceId}\n読み取り成功";

                        // イベントを発火
                        OnQRCodeScanned?.Invoke(deviceId, publicKey);

                        // 少し待ってから戻る
                        await Task.Delay(500);
                        await Navigation.PopAsync();
                    }
                    catch (FormatException)
                    {
                        StatusLabel.Text = "無効なQRコード形式です（公開鍵のデコード失敗）";
                        _isProcessing = false;
                        BarcodeReader.IsDetecting = true;
                    }
                }
                else
                {
                    StatusLabel.Text = "無効なQRコード形式です";
                    _isProcessing = false;
                    BarcodeReader.IsDetecting = true;
                }
            }
            catch (Exception ex)
            {
                StatusLabel.Text = $"エラー: {ex.Message}";
                _isProcessing = false;
                BarcodeReader.IsDetecting = true;
            }
        });
    }

    private async void CancelButton_Clicked(object? sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}
