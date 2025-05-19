using Android.Bluetooth;
using Android.Content;
using GattClientNative.BLEServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GattServerNative.GattServerService;

public partial class GattServerService
{
    BluetoothManager _manager;
    BluetoothGattServer _gattServer;
    BluetoothAdapter _adapter;
    BluetoothGattCharacteristic _characteristic;
    BLEServerCallback BLEServerCallback;

    public partial void StartServer()
    {
        _manager = (BluetoothManager)Android.App.Application.Context.GetSystemService(Context.BluetoothService);
        _adapter = _manager.Adapter;

        BLEServerCallback = new BLEServerCallback();

        //var service = new BluetoothGattService()
    }
}
