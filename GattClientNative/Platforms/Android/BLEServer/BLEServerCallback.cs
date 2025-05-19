using Android.Bluetooth;
using Android.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GattClientNative.BLEServer
{
    public class BleEventArgs : EventArgs
    {
        public BluetoothDevice Device { get; set; }
        public GattStatus GattStatus { get; set; }
        public BluetoothGattCharacteristic Characteristic { get; set; }
        public byte[] Value { get; set; }
        public int RequestId { get; set; }
        public int Offset { get; set; }
    }
    internal class BLEServerCallback : BluetoothGattServerCallback
    {
        public event EventHandler<BleEventArgs> NotificationSent;
        public event EventHandler<BleEventArgs> CharacteristicReadRequest;
        public event EventHandler<BleEventArgs> CharacteristicWriteRequest;

        public BLEServerCallback()
        {

        }

        public override void OnCharacteristicReadRequest(BluetoothDevice? device, int requestId, int offset, BluetoothGattCharacteristic? characteristic)
        {
            base.OnCharacteristicReadRequest(device, requestId, offset, characteristic);
            CharacteristicReadRequest?.Invoke(this,
                new BleEventArgs() { Device = device, RequestId = requestId, Offset = offset, });
        }

        public override void OnCharacteristicWriteRequest(BluetoothDevice? device, int requestId, BluetoothGattCharacteristic? characteristic, bool preparedWrite, bool responseNeeded, int offset, byte[]? value)
        {
            base.OnCharacteristicWriteRequest(device, requestId, characteristic, preparedWrite, responseNeeded, offset, value);
            CharacteristicWriteRequest?.Invoke(this,
                new BleEventArgs() { Device = device, RequestId = requestId, Offset = offset, });
        }

        public override void OnConnectionStateChange(BluetoothDevice? device, [GeneratedEnum] ProfileState status, [GeneratedEnum] ProfileState newState)
        {
            base.OnConnectionStateChange(device, status, newState);
        }

        public override void OnNotificationSent(BluetoothDevice? device, [GeneratedEnum] GattStatus status)
        {
            base.OnNotificationSent(device, status);
            NotificationSent?.Invoke(this, new BleEventArgs() { Device = device });
        }
    }
}
