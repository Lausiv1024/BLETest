using BLETest.Common.Crypto;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.GenericAttributeProfile;

namespace BLETest.Crypto
{
    internal class GattServerCryptoManager : CryptoManager
    {
        GattLocalCharacteristic GattLocalCharacteristic { get; }
        public GattServerCryptoManager(GattLocalCharacteristic gattLocalCharacteristic) : base(true)
        {
            this.GattLocalCharacteristic = gattLocalCharacteristic;
            SendPubKey(myPublicKey);
        }
        public override void Init()
        {
            
        }
        public async Task HandleReceive(byte[] b)
        {
            if (b.Length < 2)
            {
                return;
            }
            if (b[0] == StartData[0] && b[1] == StartData[1])
            {
                // Start
                await GattLocalCharacteristic.NotifyValueAsync(OKData.AsBuffer());
            } else if (b[0] == OKData[0] && b[1] == OKData[1])
            {
                // OK
            }
        }
        protected override AsymmetricKeyParameter ReceivePeerKey()
        {
            throw new NotImplementedException();
        }

        protected async override Task SendPubKey(AsymmetricKeyParameter pubKey)
        {
            var pubKeyData = ((ECPublicKeyParameters)pubKey).Q.GetEncoded();
            Console.WriteLine("This is Public Key : {0}", pubKeyData);
            await GattLocalCharacteristic.NotifyValueAsync(pubKeyData.AsBuffer());
        }
    }
}
