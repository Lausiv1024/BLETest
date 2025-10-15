using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using BLETest.Settings;

namespace BLETest
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        public byte cnt = 0;
        public const int BufferSize = 1024;
        BLECommunicationServer _bleCommunicationServer;
        public MainWindow()
        {
            InitializeComponent();
        }

        public async Task BLEMain()
        {
            _bleCommunicationServer = new BLECommunicationServer(
                BLESettings.ServiceId,
                BLESettings.WriteCharacteristic,
                BLESettings.NotifyCharacteristic,
                "BLETest");
            _bleCommunicationServer.OnDataReceived += (sender, e) =>
            {
                if (e.Data.Length >= 2 && e.Data[0] == 0xFF){
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
                string str = Encoding.UTF8.GetString(e.Data);
                Dispatcher.Invoke(() => ReceivedVal.Text += $"[{e.DeviceId}] : {str}\n");
            };
            await _bleCommunicationServer.BLEInitializeAsync();
        }

        private async void Window_Initialized(object sender, EventArgs e)
        {
            await BLEMain();
        }
        private int counter = 0;
        private async Task notify(string str)
        {
            byte[] buf = Encoding.UTF8.GetBytes(str + counter);
            await _bleCommunicationServer.NotifyAsync(buf);
            counter++;
        }

        private async void NotifyBut_Click(object sender, RoutedEventArgs e)
        {
            if (SendVal.Text != "")
            {
                await notify(SendVal.Text);
                //SendVal.Text = "";
            }
        }

        private async void SendVal_KeyDown(object sender, KeyEventArgs e)
        {
            if (SendVal.Text != "" && e.Key == Key.Enter)
            {
                await notify(SendVal.Text);
                
            }
        }
    }
}
