using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Agreement;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;
using System;
using System.Collections.Generic;
using System.Text;

namespace BLETest.Common.Crypto
{
    public abstract class CryptoManager
    {
        private byte[] CommonKey;
        private AsymmetricKeyParameter myPrivateKey;
        private AsymmetricKeyParameter myPublicKey;
        private AsymmetricKeyParameter peerPublicKey;
        private BigInteger SecretKey;
        public CryptoManager()
        {

        }

        public abstract void Init();
        protected abstract void SendPubKey(AsymmetricKeyParameter pubKey);
        protected abstract AsymmetricKeyParameter ReceivePeerKey();
        protected AsymmetricCipherKeyPair generateECDHKeyPair(ECDomainParameters ecParams)
        {
            var ecKeygenParameters = 
                new ECKeyGenerationParameters(ecParams, new SecureRandom());
            var eCKeyPairGenerator= new ECKeyPairGenerator();
            eCKeyPairGenerator.Init(ecKeygenParameters);
            var ecKeyPair = eCKeyPairGenerator.GenerateKeyPair();
           
            return ecKeyPair;
        }

        protected BigInteger DoAgreement(AsymmetricKeyParameter privateKey, AsymmetricKeyParameter pubKey)
        {
            var keyAgreement = new ECDHBasicAgreement();
            keyAgreement.Init(privateKey);
            var secret = keyAgreement.CalculateAgreement(pubKey);
            return secret;
        }

        private void CreateKey(AsymmetricCipherKeyPair ecKeyPair)
        {
            
        }
    }
}
