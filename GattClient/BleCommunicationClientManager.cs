using BLETest.Settings;
using Plugin.BLE.Abstractions;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GattClient
{
    internal class BleCommunicationClientManager
    {
        readonly IBluetoothLE ble;
        readonly IAdapter adapter;
        readonly List<IDevice> devices;
        IDevice? ConnectedDevice;
        public bool IsConfiguring { get; private set; }
        public bool IsConnected => ConnectedDevice != null;
        public delegate void DeviceDisconnectedEventHandler(object sender, EventArgs e);
        public event DeviceDisconnectedEventHandler? DeviceDisconnected;
        public delegate void ReceiveNotificationEventHandler(object sender, ReceivedNotificationEventArgs e);
        public event ReceiveNotificationEventHandler? OnReceive;

        public BleCommunicationClientManager(IBluetoothLE ble, IAdapter adapter)
        {
            devices = new List<IDevice>();
            this.ble = ble;
            this.adapter = adapter;
            this.adapter.DeviceDiscovered += (s, e) =>
            {
                Console.WriteLine($"Device Discovered: {e.Device.Name}");
                devices.Add(e.Device);
            };
            this.adapter.DeviceDisconnected += (s, e) =>
            {
                Console.WriteLine("Device Disconnected");
                ConnectedDevice = null;
                DeviceDisconnected?.Invoke(this, new EventArgs());
            };
        }


        public async Task SendDataAsync(byte[] data)
        {
            if (ConnectedDevice == null)
            {
                Console.WriteLine("Device is not connected");
                return;
            }
            var service = await ConnectedDevice.GetServiceAsync(BLESettings.ServiceId);
            var characteristic = await service.GetCharacteristicAsync(BLESettings.BleCommunicationCCharacteristic);
            long s = DateTime.Now.Ticks;
            await characteristic.WriteAsync(data);
            long e = DateTime.Now.Ticks;
            Console.WriteLine($"Write time: {e - s} ticks");
        }
        private async Task DisconnectDevice()
        {
            if ( ConnectedDevice != null)
            {
                await adapter.DisconnectDeviceAsync(ConnectedDevice);
            }
        }
        public async Task ConfigureCharacteristic(Guid ServiceId, Guid CharacteristicId)
        {
            IsConfiguring = true;
            var foundService = await ConnectToService(ServiceId);
            if (foundService == null)
            {
                await DisconnectDevice();
                IsConfiguring = false;
                return;
            }
            Console.WriteLine("Found service Id: {0}", foundService.Id);
            var c = await foundService.GetCharacteristicAsync(BLESettings.BleCommunicationCCharacteristic);
            c.ValueUpdated += (s, e) =>
            {
                OnReceive?.Invoke(this, new ReceivedNotificationEventArgs(e.Characteristic.Value));
            };
            await c.StartUpdatesAsync();
            IsConfiguring = false;
            Task.Run(KeepAlive);
        }


        /// <summary>
        /// ServiceIdからデバイスを検索する
        /// </summary>
        /// <param name="serviceId"></param>
        /// <returns></returns>
        private async Task FindDeviceFromServiceIdAsync(Guid serviceId)
        {
            devices.Clear();
            Console.WriteLine("Finding Device...");
            var scanOption = new ScanFilterOptions()
            {
                ServiceUuids = [serviceId]
            };
            await adapter.StartScanningForDevicesAsync(scanOption);
            Console.WriteLine("Finding Devices finished");
        }

        private async Task<IService?> ConnectToService(Guid ServiceId)
        {
            await FindDeviceFromServiceIdAsync(ServiceId);//該当するサービスを持つデバイスをスキャンする。
            if (devices.Count == 0) 
            {
                Console.WriteLine("No devices found");
                return null;
            }
            //スキャン結果はdevicesに格納されている
            Console.WriteLine($"Found {devices.Count} devices");
            IService? service = null;
            foreach (var device in devices)
            {
                await adapter.ConnectToDeviceAsync(device);
                var services = await device.GetServicesAsync();
                var s = services.FirstOrDefault(x => x.Id == ServiceId);
                if (s == null)
                {
                    await adapter.DisconnectDeviceAsync(device);
                    continue;
                }
                ConnectedDevice = device;
                service = s;
                break;
            }
            return service;
        }

        private async void KeepAlive()
        {
            while (ConnectedDevice != null)
            {
                await Task.Delay(1000);
                var service = await ConnectedDevice.GetServiceAsync(BLESettings.ServiceId);
                var characteristic = await service.GetCharacteristicAsync(BLESettings.BleCommunicationCCharacteristic);
                await characteristic.ReadAsync();
            }
        }
    }
    public class ReceivedNotificationEventArgs : EventArgs
    {
        public byte[] Data { get; }
        public ReceivedNotificationEventArgs(byte[] data)
        {
            Data = data;
        }
    }
}
