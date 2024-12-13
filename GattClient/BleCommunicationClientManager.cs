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
        ICharacteristic? CurrentCharacteristic;
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
            this.adapter.DeviceDiscovered += (s, e) => //デバイスの検出
            {
                Console.WriteLine($"Device Discovered: {e.Device.Name}");
                devices.Add(e.Device);
            };
            this.adapter.DeviceDisconnected += (s, e) => //デバイスの切断
            {
                Console.WriteLine("Device Disconnected");
                ConnectedDevice = null;
                CurrentCharacteristic = null;
                DeviceDisconnected?.Invoke(this, new EventArgs());
            };
            ble.StateChanged += (s, e) =>
            {
                Console.WriteLine($"BLE State Changed: {e.NewState}");
            };
            
        }


        public async Task<long> SendDataAsync(byte[] data)
        {
            if (ConnectedDevice == null || CurrentCharacteristic == null)
            {
                Console.WriteLine("Device is not connected");
                return 0;
            }
            if (IsConfiguring)
                return 0;
            long s = DateTime.Now.Ticks;
            await CurrentCharacteristic.WriteAsync(data);
            long e = DateTime.Now.Ticks;
            Console.WriteLine($"Write time: {e - s} ticks");
            return e - s;
        }
        private async Task DisconnectDevice()
        {
            if ( ConnectedDevice != null)
            {
                await adapter.DisconnectDeviceAsync(ConnectedDevice);
            }
        }

        /// <summary>
        /// 通信用キャラクタリスティックの設定
        /// </summary>
        /// <param name="ServiceId">サービスID</param>
        /// <param name="CharacteristicId">キャラクタ理スティックのID</param>
        /// <returns></returns>
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
            CurrentCharacteristic = c;
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
                Console.WriteLine($"Connected Device id : {ConnectedDevice.Id}");
                
                service = s;
                break;
            }
            return service;
        }

        private async void KeepAlive()
        {
            while (ConnectedDevice != null && CurrentCharacteristic != null)
            {
                await Task.Delay(1000);
                await CurrentCharacteristic.ReadAsync();
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
