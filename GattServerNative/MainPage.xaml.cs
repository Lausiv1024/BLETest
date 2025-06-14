﻿using GattServerNative.GattServerImpl;

namespace GattServerNative
{
    public partial class MainPage : ContentPage
    {
        int count = 0;
        IBLEGattServer gattServer;

        public MainPage()
        {
            InitializeComponent();
            var bleServer = DependencyService.Get<IBLEGattServer>();
            bleServer.StartServer();
        }

        private void OnCounterClicked(object? sender, EventArgs e)
        {
            count++;

            if (count == 1)
                CounterBtn.Text = $"Clicked {count} time";
            else
                CounterBtn.Text = $"Clicked {count} times";

            SemanticScreenReader.Announce(CounterBtn.Text);
        }

        private async void ContentPage_Loaded(object sender, EventArgs e)
        {
            //位置情報とBluetoothのパーミッションを確認
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
        }
    }
}
