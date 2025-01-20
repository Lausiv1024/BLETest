namespace GattClientNative
{
    public partial class MainPage : ContentPage
    {
        int count = 0;

        public MainPage()
        {
            InitializeComponent();
        }

        private void OnCounterClicked(object sender, EventArgs e)
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
            // Check for required permissions
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
                    await DisplayAlert("Bluetooth Permission Required", "This app requires Bluetooth permission to scan for BLE devices.", "OK");
                    return;
                }
            }
        }
    }

}
