using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using FluentAvalonia.UI.Controls;
using System.Threading.Tasks;
using Avalonia.Threading;
using BLETest.Common;

namespace BLETest.Desktop;

public partial class MainWindow : Window
{
    public byte cnt = 0;
    public const int BufferSize = 1024;
    BLECommunicationServer? _bleCommunicationServer;
    //BleSecurityManager _bleSecurityManager = new BleSecurityManager();

    public MainWindow()
    {
        InitializeComponent();
        AddDevice.Closing += (_, _) => RegisteredDeviceManager.Default.DisposeNew();
    }

    public async Task BleMain()
    {
        _bleCommunicationServer = new BLECommunicationServer(
            BLESettings.ServiceId,
            BLESettings.WriteCharacteristic,
            BLESettings.NotifyCharacteristic,
            "BLETest");
        _bleCommunicationServer.OnDataReceived += OndataReceived;
        await _bleCommunicationServer.BLEInitializeAsync();
    }

    private void OndataReceived(object? sender, OnDataReceivedEventArgs e)
    {
        if (e.Data.Length >= 2 && e.Data[0] == 0xFF)
        {
            if (e.Data[1] == 0x80)
            {
                //Dispatcher.InvokeAsync(() => NotifyBut.Content = "1");
                KeyControl.KeyDown(0x31); // '1' key down
            } else if (e.Data[1] == 0x81)
            {
                //Dispatcher.InvokeAsync(() => NotifyBut.Content = "0");
                KeyControl.KeyUp(0x31); // '1' key up
            }
            return;
        }
        string str = System.Text.Encoding.UTF8.GetString(e.Data);
        Dispatcher.UIThread.Post(() => ReceivedVal.Text += $"[{e.DeviceId}] : {str}\n");
    }

    private async Task Notify(string message)
    {
        if (_bleCommunicationServer != null)
        {
            byte[] data = System.Text.Encoding.UTF8.GetBytes(message);
            await _bleCommunicationServer.NotifyAsync(data);
        }
    }

    private async void SendVal_KeyDown(object? sender, Avalonia.Input.KeyEventArgs e)
    {
        if (e.Key == Avalonia.Input.Key.Enter)
        {
            if (!string.IsNullOrEmpty(SendVal.Text))
            {
                await Notify(SendVal.Text);
                //SendVal.Text = "";
            }
        }
    }

    private async void NotifyBut_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(SendVal.Text))
        {
            await Notify(SendVal.Text);
        }
    }

    private async void NewDeviceMenu_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var device = RegisteredDeviceManager.Default.CreateNew();
        using var qrGenerator = new QRCoder.QRCodeGenerator();
        var qrCodeData = qrGenerator.CreateQrCode($"{device.DeviceId},{device.PubKey}", QRCoder.QRCodeGenerator.ECCLevel.Q);
        var qrCode = new QRCoder.PngByteQRCode(qrCodeData);

        using var ms = new System.IO.MemoryStream(qrCode.GetGraphic(20));
        QRImage.Source = new Bitmap(ms);
        await AddDevice.ShowAsync();

    }

    private async void Window_Initialized(object? sender, System.EventArgs e)
    {
        await BleMain();
    }

    private void Window_Closing(object? sender, WindowClosingEventArgs e)
    {
        _bleCommunicationServer?.Stop();
    }
}