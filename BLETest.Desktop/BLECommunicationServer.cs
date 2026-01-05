using BLETest.Common;
using BLETest.Common.ComModel;
using MessagePack;
using Org.BouncyCastle.Asn1.Ocsp;
using Org.BouncyCastle.Security;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Storage.Streams;
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
    public event OnDataReceivedEventHandler? OnDataReceived;

    public delegate void OnDebugMessageEventHandler(object sender, OnDebugMessageEventArgs e);
    public event OnDebugMessageEventHandler? OnDebugMessage;

    public delegate void AuthenticationResultEventHandler(object sender, AuthenticationResultEventArgs e);
    public event AuthenticationResultEventHandler? OnAuthenticationResult;

    private CommunicationBase? AuthNextRead;

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

    /// <summary>
    /// BLEの初期化とアドバタイズ開始
    /// </summary>
    /// <returns></returns>
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
        var cmd = Util.GetCommandType(b);
        if (cmd == CommandType.DeviceNewData)
        {
            HandleDeviceNewData(b, request);
        }

        deferral.Complete();
    }

    private void HandleDeviceNewData(byte[]b, GattWriteRequest req)
    {
        // Implementation for handling DeviceNewData
        var deviceNew = MessagePackSerializer.Deserialize<DeviceNewData>(b);
        // Signature検証
        var signer = SignerUtilities.GetSigner(Constants.ECDH_CURVE_ALGORITHM);
        signer.Init(false, CryptoUtil.ByteToPubKey(deviceNew.MPubKey)); //signerを検証モードとして初期化します
        var deviceIdBytes = deviceNew.DeviceId.ToByteArray();
        signer.BlockUpdate(deviceIdBytes, 0, deviceIdBytes.Length);
        var isValid = signer.VerifySignature(deviceNew.DeviceIdSig);
        if (req.Option == GattWriteOption.WriteWithResponse)
        {
            req.Respond();
        }
        if (!isValid)
        {
            Console.WriteLine("Invalid device authentication signature");
            //PostDebugMessage("Invalid device authentication signature");
            SendInitAuthenticationResult(false, "Invalid device authentication signature", req);
            return;
        }

        RegisteredDeviceManager.Default.SaveNew(Convert.ToBase64String(deviceNew.MPubKey));

        Console.WriteLine("Device authenticated: " + deviceNew.DeviceId);
        //PostDebugMessage("Device authenticated: " + deviceNew.DeviceId);
        SendInitAuthenticationResult(true, "Device authenticated", req);
    }

    /// <summary>
    /// 初回認証結果をクライアントに送信します
    /// </summary>
    /// <param name="isSuccess"></param>
    /// <param name="message"></param>
    /// <param name="req"></param>
    private void SendInitAuthenticationResult(bool isSuccess, string message, GattWriteRequest req)
    {
        OnAuthenticationResult?.Invoke(this, new AuthenticationResultEventArgs(isSuccess, message));

        AuthNextRead = new DeviceNewResult
        {
            IsSuccess = isSuccess,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),Id=1, Command = CommandType.DeviceNewResult,
        };
    }

    private async void AuthenticationCharacteristicRead_ReadRequested(GattLocalCharacteristic sender, GattReadRequestedEventArgs args)
    {
        var deferral = args.GetDeferral();
        var request = await args.GetRequestAsync();
        Console.WriteLine("AuthenticationCharacteristic ReadRequested");
        // Respond with the next AuthNextRead data if available
        if (AuthNextRead != null)
        {
            var bytes = MessagePackSerializer.Serialize((DeviceNewResult)AuthNextRead);
            request.RespondWithValue(bytes.AsBuffer());
            AuthNextRead = null;
        }
        else
        {
            byte[] buf = "0"u8.ToArray();
            request.RespondWithValue(buf.AsBuffer());
        }
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

    public void PostDebugMessage(string message)
    {
        OnDebugMessage?.Invoke(this, new OnDebugMessageEventArgs(message));
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

public class OnDebugMessageEventArgs : EventArgs
{
    public string Message { get; }
    public OnDebugMessageEventArgs(string message)
    {
        Message = message;
    }
}
public class AuthenticationResultEventArgs : EventArgs
{
    public bool IsSuccess { get; }
    public string Message { get; }
    public AuthenticationResultEventArgs(bool isSuccess, string message)
    {
        IsSuccess = isSuccess;
        Message = message;
    }
}