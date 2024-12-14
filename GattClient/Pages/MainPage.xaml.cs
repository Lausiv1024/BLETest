using BLETest.Settings;
using Plugin.BLE;
using Plugin.BLE.Abstractions.Contracts;
using System.Text;

namespace GattClient
{
    public partial class MainPage : ContentPage
    {
        int count = 0;
        BleCommunicationClientManager? manager;
        bool isBleSetupDone = false;

        public MainPage()
        {
            InitializeComponent();
            RealtimeStateSend.Pressed += async (s, e) =>
            {
                await manager.SendDataAsync([0xFF, 0x80]);
            };
            RealtimeStateSend.Released += async (s, e) =>
            {
                await manager.SendDataAsync([0xFF, 0x81]);
            };
        }

        private async void OnCounterClicked(object sender, EventArgs e)
        {
            count++;

            if (count == 1)
                CounterBtn.Text = $"Clicked {count} time";
            else
                CounterBtn.Text = $"Clicked {count} times";

            SemanticScreenReader.Announce(CounterBtn.Text);
            if (manager.IsConfiguring)
                return;
            if (manager.IsConnected)
            {
                if (SendValue.Text == string.Empty)
                    return;
                var data = Encoding.UTF8.GetBytes(SendValue.Text);
                long time = await manager.SendDataAsync(data);
                ReceivedValue.Text += $"Sending time {time}ticks\n";
                //SendValue.Text = string.Empty;
            } else
                await manager.ConfigureCharacteristic(BLESettings.ServiceIdEsp, BLESettings.BleCommunicationCCharacteristicEsp);

        }
        private async void ContentPage_Loaded(object sender, EventArgs e)
        {
            var locPermissionStatus = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (locPermissionStatus != PermissionStatus.Granted)
            {
                var locPermissionRequest = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                if (locPermissionRequest != PermissionStatus.Granted)
                {
                    await DisplayAlert("Location Permission Required", "This app requires location permission to scan for BLE devices.", "OK");
                    return;
                }
            }
            var bluetoothPermission = await Permissions.CheckStatusAsync<Permissions.Bluetooth>();
            if (bluetoothPermission != PermissionStatus.Granted)
            {
                var bluetoothPermissionRequest = await Permissions.RequestAsync<Permissions.Bluetooth>();
                if (bluetoothPermissionRequest != PermissionStatus.Granted)
                {
                    await DisplayAlert("Bluetooth Permission Required", "This app requires bluetooth permission to scan for BLE devices.", "OK");
                    return;
                }
            }

            if (manager == null)
            {
                //初回起動時のみインスタンスを生成・初期化
                manager = new BleCommunicationClientManager(CrossBluetoothLE.Current, CrossBluetoothLE.Current.Adapter);
                manager.OnReceive += Manager_OnReceive;
                manager.OnDataSent += Manager_OnDataSent;
            }
            if (!manager.IsConnected)
                await manager.ConfigureCharacteristic(BLESettings.ServiceIdEsp, BLESettings.BleCommunicationCCharacteristicEsp);
            if (manager.IsConnected)
                ReceivedValue.Text += $"Setup done\n";
            else
                ReceivedValue.Text += "Setup failed\n";
            Console.WriteLine(manager.IsConnected);
            isBleSetupDone = true;
        }

        private void Manager_OnDataSent(object sender, DataSentEventArgs e)
        {
            Dispatcher.Dispatch(() => ReceivedValue.Text += $"Sending Spent Ticks : {e.SentTime}");
        }

        private void Manager_OnReceive(object sender, ReceivedNotificationEventArgs e)
        {
            var s = Encoding.UTF8.GetString(e.Data);
            Console.WriteLine($"Received: {s}");
            Dispatcher.DispatchAsync(() =>
            {
                ReceivedValue.Text += s + "\n";
            });
        }
    }
}
