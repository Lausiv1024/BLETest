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
        private Queue<byte[]> sendingBytesQueue;
        private Guid? ConnectedDeviceId;

        public bool IsConfiguring { get; private set; }
        public bool IsConnected => ConnectedDevice != null;

        public delegate void DeviceDisconnectedEventHandler(object sender, EventArgs e);
        public event DeviceDisconnectedEventHandler? DeviceDisconnected;
        public delegate void ReceiveNotificationEventHandler(object sender, ReceivedNotificationEventArgs e);
        public event ReceiveNotificationEventHandler? OnReceive;

        public BleCommunicationClientManager(IBluetoothLE ble, IAdapter adapter)
        {
            devices = new List<IDevice>();
            sendingBytesQueue = new Queue<byte[]>();
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
                DeviceDisconnected?.Invoke(this, EventArgs.Empty);
            };
            ble.StateChanged += (s, e) =>
            {
                Console.WriteLine($"BLE State Changed: {e.NewState}");
            };
            Task.Run(ManagerMainLoop);
        }

        public delegate void DataSentEventHandler(object sender, DataSentEventArgs e);
        public event DataSentEventHandler? OnDataSent;
        private async Task ManagerMainLoop()
        {
            int c = 0;
            while (true)
            {
                if (IsConnected && CurrentCharacteristic != null)
                {
                    if (sendingBytesQueue.Count > 0)
                    {
                        var data = sendingBytesQueue.Dequeue();

                        long s = DateTime.Now.Ticks;
                        await CurrentCharacteristic.WriteAsync(data);//なぜかここで100万Tick以上の処理時間が生じる
                        long e = DateTime.Now.Ticks;

                        Console.WriteLine($"Sending time: {e - s}ticks");
                        OnDataSent?.Invoke(this, new DataSentEventArgs(e - s));
                    }
                    else if (c % 1000 == 0)
                    {
                        //await CurrentCharacteristic.ReadAsync();
                    }
                }

                await Task.Delay(1);
                c++;
            }
        }

        public async Task<long> SendDataAsync(byte[] data)
        {
            sendingBytesQueue.Enqueue(data);
            return 0;
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
            var c = await foundService.GetCharacteristicAsync(CharacteristicId);
            c.ValueUpdated += (s, e) =>
            {
                OnReceive?.Invoke(this, new ReceivedNotificationEventArgs(e.Characteristic.Value));
            };
            await c.StartUpdatesAsync();
            CurrentCharacteristic = c;
            IsConfiguring = false;
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
            await adapter.StartScanningForDevicesAsync();
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
    }
    public class ReceivedNotificationEventArgs : EventArgs
    {
        public byte[] Data { get; }
        public ReceivedNotificationEventArgs(byte[] data)
        {
            Data = data;
        }
    }

    public class DataSentEventArgs : EventArgs
    {
        public long SentTime { get; }
        public DataSentEventArgs(long sentTime)
        {
            SentTime = sentTime;
        }
    }
}
