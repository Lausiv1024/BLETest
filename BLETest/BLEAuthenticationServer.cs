using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.GenericAttributeProfile;

namespace BLETest
{
    internal class BLEAuthenticationServer
    {
        public const int BufferSize = 1024;
        private NewDeviceContext newDeviceContext = null;
        private GattLocalCharacteristic authCharacteristic;
        public BLEAuthenticationServer() { }

        public void StartNewDeviceAcceptance()
        {
            // Implementation for starting new device acceptance
        }

        public void StopNewDeviceAcceptance()
        {
            // Implementation for stopping new device acceptance
        }
    }
    internal class NewDeviceContext
    {
        public string DeviceId { get; set; }
        public string PublicKey { get; set; }
        public string PrivateKey { get; set; }
    }
}
