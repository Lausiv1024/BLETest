using System;
using System.Collections.Generic;
using System.Text;
using System.Security;
using System.Security.Cryptography;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto;
using BLETest.Settings;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Agreement;
namespace BLETest.Common.Crypto
{
    public class KeyAgreementSystem
    {
        AsymmetricKeyParameter myPrivateKey;
        AsymmetricKeyParameter myPublicKey;
        AsymmetricKeyParameter senderPublicKey;
        public KeyAgreementSystem()
        {
            var keyPair = CreateKeyPair();
            ECDHBasicAgreement keyAgreement = new ECDHBasicAgreement();
            keyAgreement.Init(keyPair.Private);
        }

        private AsymmetricCipherKeyPair CreateKeyPair()
        {
            var ecKeyGenParams =
                new ECKeyGenerationParameters(Util.K283DomainParameters(), new Org.BouncyCastle.Security.SecureRandom());
            var ecKeyPairGen = new ECKeyPairGenerator();
            ecKeyPairGen.Init(ecKeyGenParams);
            return ecKeyPairGen.GenerateKeyPair();
        }
    }
}
