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
using System.Threading.Tasks;


namespace BLETest.Common.Crypto
{
    public abstract class CryptoManager
    {
        private byte[] CommonKey;
        protected AsymmetricKeyParameter myPrivateKey;
        protected AsymmetricKeyParameter myPublicKey;
        protected AsymmetricKeyParameter peerPublicKey;
        private BigInteger SecretKey;
        private bool IsServerSide;
        public static readonly byte[] StartData = new byte[] {0x20, 0x40 };
        public static readonly byte[] OKData = new byte[] { 0x24, 0x41 };
        public CryptoManager(bool IsServerSide)
        {
            this.IsServerSide = IsServerSide;
            var keyPair = generateECDHKeyPair(Util.K283DomainParameters());
            myPrivateKey = keyPair.Private;
            myPublicKey = keyPair.Public;
        }

        public abstract void Init();
        protected abstract Task SendPubKey(AsymmetricKeyParameter pubKey);
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

        protected BigInteger ComputeSharedSecret(AsymmetricKeyParameter privateKey, AsymmetricKeyParameter pubKey)
        {
            var keyAgreement = new ECDHBasicAgreement();
            keyAgreement.Init(privateKey);
            var secret = keyAgreement.CalculateAgreement(pubKey);
            return secret;
        }
    }
}
