using BLETest.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using System.Diagnostics;
using Windows.Storage.Streams;
namespace BLETest
{
    public class BLECommunicationServer
    {
        public const int BufferSize = 1024;

        public Guid ServiceId { get; }
        public Guid ParamId { get; }
        public string ParamName { get; }
        private GattLocalCharacteristic localCharasteristic;
        public delegate void OnDataReceivedEventHandler(object sender, OnDataReceivedEventArgs e);
        public event OnDataReceivedEventHandler OnDataReceived;
        /// <summary>
        /// 1つのGattCharastricを用いて、BLE通信を行う
        /// </summary>
        public BLECommunicationServer(Guid ServiceId, Guid ParamId, string paramName)
        {
            this.ServiceId = ServiceId;
            this.ParamId = ParamId;
            this.ParamName = paramName;
        }
        public async Task BLEInitializeAsync()
        {
            var gattSvcProviderRes = await GattServiceProvider.CreateAsync(ServiceId);
            if (gattSvcProviderRes.Error != Windows.Devices.Bluetooth.BluetoothError.Success)
            {
                Console.WriteLine("Failed to create GattServiceProvider::" + gattSvcProviderRes.Error);
                return;
            }
            var gattSvcProvider = gattSvcProviderRes.ServiceProvider;
            var cReadWriteParam = new GattLocalCharacteristicParameters
            {
                CharacteristicProperties = GattCharacteristicProperties.Read | GattCharacteristicProperties.Write | GattCharacteristicProperties.Notify,
                WriteProtectionLevel = GattProtectionLevel.Plain,
                ReadProtectionLevel = GattProtectionLevel.Plain
            };
            var cReadWrite = await gattSvcProvider.Service.CreateCharacteristicAsync(
                ParamId, cReadWriteParam);
            localCharasteristic = cReadWrite.Characteristic;
            localCharasteristic.ReadRequested += async (sender, args) =>
            {
                var sw = new Stopwatch();
                var deferral = args.GetDeferral();
                sw.Start();
                var request = await args.GetRequestAsync();
                sw.Stop();
                Console.WriteLine("GetRequest Time : {0}ms", sw.ElapsedMilliseconds);
                byte[] buf = new byte[] { 0x20};
                request.RespondWithValue(buf.AsBuffer());
                deferral.Complete();
            };
            localCharasteristic.WriteRequested += async (sender, args) =>
            {
                var deferral = args.GetDeferral();

                var request = await args.GetRequestAsync();
                var buf = request.Value.ToArray();
                Console.WriteLine("WriteRequested: " + BitConverter.ToString(buf));

                OnDataReceived?.Invoke(this, new OnDataReceivedEventArgs(buf, args.Session.DeviceId.Id));

                if (request.Option == GattWriteOption.WriteWithResponse)
                {
                    request.Respond();
                    Console.WriteLine("Respond to write Request");
                }
                
                deferral.Complete();

            };

            GattServiceProviderAdvertisingParameters advertisingParameters = new GattServiceProviderAdvertisingParameters
            {
                IsDiscoverable = true,
                IsConnectable = true
            };
            gattSvcProvider.StartAdvertising(advertisingParameters);
            await Task.Delay(int.MaxValue);
        }

        public async Task NotifyAsync(byte[] data)
        {
            await localCharasteristic?.NotifyValueAsync(data.AsBuffer());
        }
    }

    public class OnDataReceivedEventArgs : EventArgs
    {
        public byte[] Data { get; }
        public string DeviceId { get; }
        public OnDataReceivedEventArgs(byte[] data, string deviceId)
        {
            Data = data;
            DeviceId = deviceId;
        }
    }
}
