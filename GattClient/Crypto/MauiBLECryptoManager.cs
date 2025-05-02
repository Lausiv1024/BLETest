using BLETest.Common.Crypto;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Plugin.BLE.Abstractions.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GattClient.Crypto
{
    internal class MauiBLECryptoManager : CryptoManager
    {
        ICharacteristic Characteristic { get; }
        public MauiBLECryptoManager(ICharacteristic characteristic) : base(false)
        {
            Characteristic = characteristic;
        }

        public override async void Init()
        {
            await Characteristic.WriteAsync(StartData);
        }

        protected override AsymmetricKeyParameter ReceivePeerKey()
        {
            throw new NotImplementedException();
        }

        protected override async Task SendPubKey(AsymmetricKeyParameter pubKey)
        {
            var pubKeyData = ((ECPublicKeyParameters)pubKey).Q.GetEncoded();
            await Characteristic.WriteAsync(pubKeyData);
        }
    }
}
