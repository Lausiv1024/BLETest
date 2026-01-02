using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using System.Diagnostics;
using Windows.Storage.Streams;
using BLETest.Common;
using BLETest.Common.ComModel;
using MessagePack;
using Org.BouncyCastle.Security;
namespace BLETest.Desktop;

public class BLECommunicationServer
{
    public const int BufferSize = 1024;

    public Guid ServiceId { get; }
    public Guid WriteCharacteristicId { get; }
    public Guid NotifyCharacteristicId { get; }
    public string ParamName { get; }
    private GattLocalCharacteristic writeCharacteristic;
    private GattLocalCharacteristic notifyCharacteristic;

    private GattLocalCharacteristic authenticationCharacteristicRead;
    private GattLocalCharacteristic authenticationCharacteristicWrite;

    private GattServiceProvider gattServiceProvider;

    public delegate void OnDataReceivedEventHandler(object sender, OnDataReceivedEventArgs e);
    public event OnDataReceivedEventHandler OnDataReceived;

    /// <summary>
    /// Write用とNotify用のキャラクタリスティックを分けてBLE通信を行う
    /// </summary>
    public BLECommunicationServer(Guid ServiceId, Guid WriteCharacteristicId, Guid NotifyCharacteristicId, string paramName)
    {
        this.ServiceId = ServiceId;
        this.WriteCharacteristicId = WriteCharacteristicId;
        this.NotifyCharacteristicId = NotifyCharacteristicId;
        this.ParamName = paramName;
    }

    /// <summary>
    /// 後方互換性のためのコンストラクタ（1つのキャラクタリスティックでWrite/Notify両方）
    /// </summary>
    [Obsolete("Use the constructor with separate Write and Notify characteristics")]
    public BLECommunicationServer(Guid ServiceId, Guid ParamId, string paramName)
        : this(ServiceId, ParamId, ParamId, paramName)
    {
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
        gattServiceProvider = gattSvcProvider;

        // Write用キャラクタリスティック（Read/Write/WriteWithoutResponse）
        var writeParam = new GattLocalCharacteristicParameters
        {
            CharacteristicProperties = GattCharacteristicProperties.Write
                | GattCharacteristicProperties.WriteWithoutResponse,
            WriteProtectionLevel = GattProtectionLevel.Plain,
            ReadProtectionLevel = GattProtectionLevel.Plain
        };
        var writeResult = await gattSvcProvider.Service.CreateCharacteristicAsync(
            WriteCharacteristicId, writeParam);
        writeCharacteristic = writeResult.Characteristic;

        writeCharacteristic.ReadRequested += async (sender, args) =>
        {
            var sw = new Stopwatch();
            var deferral = args.GetDeferral();
            sw.Start();
            var request = await args.GetRequestAsync();
            sw.Stop();
            Console.WriteLine("Write Characteristic ReadRequested - GetRequest Time : {0}ms", sw.ElapsedMilliseconds);
            byte[] buf = new byte[] { 0x20 };
            request.RespondWithValue(buf.AsBuffer());
            deferral.Complete();
        };

        writeCharacteristic.WriteRequested += async (sender, args) =>
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

        // Notify用キャラクタリスティック（Read/Notify）
        var notifyParam = new GattLocalCharacteristicParameters
        {
            CharacteristicProperties = GattCharacteristicProperties.Read | GattCharacteristicProperties.Notify,
            WriteProtectionLevel = GattProtectionLevel.Plain,
            ReadProtectionLevel = GattProtectionLevel.Plain
        };
        var notifyResult = await gattSvcProvider.Service.CreateCharacteristicAsync(
            NotifyCharacteristicId, notifyParam);
        notifyCharacteristic = notifyResult.Characteristic;

        notifyCharacteristic.ReadRequested += async (sender, args) =>
        {
            var deferral = args.GetDeferral();
            var request = await args.GetRequestAsync();
            Console.WriteLine("Notify Characteristic ReadRequested");
            byte[] buf = new byte[] { 0x21 };
            request.RespondWithValue(buf.AsBuffer());
            deferral.Complete();
        };

        var authParamRead = new GattLocalCharacteristicParameters
        {
            CharacteristicProperties = GattCharacteristicProperties.Read,
            ReadProtectionLevel = GattProtectionLevel.Plain
        };
        var authParamWrite = new GattLocalCharacteristicParameters
        {
            CharacteristicProperties = GattCharacteristicProperties.Write
                | GattCharacteristicProperties.Read,
            WriteProtectionLevel = GattProtectionLevel.Plain
        };

        var authReadResult =
            await gattServiceProvider.Service.CreateCharacteristicAsync(BLESettings.AuthCharacteristicRead, authParamRead);
        authenticationCharacteristicRead = authReadResult.Characteristic;

        authenticationCharacteristicRead.ReadRequested += AuthenticationCharacteristicRead_ReadRequested;

        var authWriteResult =
            await gattServiceProvider.Service.CreateCharacteristicAsync(BLESettings.AuthCharacteristicWrite, authParamWrite);
        authenticationCharacteristicWrite = authWriteResult.Characteristic;
        authenticationCharacteristicWrite.WriteRequested += AuthenticationCharacteristicWrite_WriteRequested;

        GattServiceProviderAdvertisingParameters advertisingParameters = new GattServiceProviderAdvertisingParameters
        {
            IsDiscoverable = true,
            IsConnectable = true
        };
        gattSvcProvider.StartAdvertising(advertisingParameters);
        
        await Task.Delay(int.MaxValue);
    }

    private async void AuthenticationCharacteristicWrite_WriteRequested(GattLocalCharacteristic sender, GattWriteRequestedEventArgs args)
    {
        var deferral = args.GetDeferral();
        var request = await args.GetRequestAsync();
        var b = request.Value.ToArray();

        var deviceNew = MessagePackSerializer.Deserialize<DeviceNewData>(b);

        var signer = SignerUtilities.GetSigner(Constants.ECDH_CURVE_ALGORITHM);
        signer.Init(false,CryptoUtil.ByteToPubKey( deviceNew.MPubKey));
        var deviceIdBytes = deviceNew.DeviceId.ToByteArray();
        signer.BlockUpdate(deviceIdBytes, 0,  deviceIdBytes.Length);
        var isValid = signer.VerifySignature(deviceNew.DeviceIdSig);

        if (!isValid)
        {
            Console.WriteLine("Invalid device authentication signature");
            deferral.Complete();
            return;
        }

        RegisteredDeviceManager.Default.SaveNew(Convert.ToBase64String(deviceNew.MPubKey));

        Console.WriteLine("Device authenticated: " + deviceNew.DeviceId);

        deferral.Complete();
    }

    private async void AuthenticationCharacteristicRead_ReadRequested(GattLocalCharacteristic sender, GattReadRequestedEventArgs args)
    {
        var deferral = args.GetDeferral();
        var request = await args.GetRequestAsync();
        deferral.Complete();
        //throw new NotImplementedException();
    }

    public async Task NotifyAsync(byte[] data)
    {
        await notifyCharacteristic?.NotifyValueAsync(data.AsBuffer());
    }

    public void Stop()
    {
        gattServiceProvider?.StopAdvertising();
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
