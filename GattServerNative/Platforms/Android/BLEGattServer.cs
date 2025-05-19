using Android.Bluetooth;
using GattServerNative.GattServerImpl;
using GattServerNative.Platforms.Android;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[assembly : Dependency(typeof(BLEGattServer))]
namespace GattServerNative.Platforms.Android;

public class BLEGattServer : IBLEGattServer
{
    BluetoothManager manager;
    BluetoothGattServer gattServer;

    public void StartServer()
    {
        Console.WriteLine("Start Server");
    }
}
