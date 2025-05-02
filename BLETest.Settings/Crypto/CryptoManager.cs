using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.Security;
using System;
using System.Collections.Generic;
using System.Text;

namespace BLETest.Common.Crypto
{
    public class CryptoManager
    {
        public CryptoManager()
        {

        }

        public static ECDomainParameters ecBuiltinBinaryDomainParameters()
        {
            var ecParams = ECNamedCurveTable.GetByName("K-283");
            return new ECDomainParameters(ecParams);
        }

        public void CreateEd25519Signer()
        {
            var ed25519KeyPairGenerator = new Ed25519KeyPairGenerator();
            var signer = new Ed25519Signer();
        }

        private AsymmetricCipherKeyPair generateECDHKeyPair(ECDomainParameters ecParams)
        {
            var ecKeygenParameters = 
                new ECKeyGenerationParameters(ecParams, new SecureRandom());
            var eCKeyPairGenerator= new ECKeyPairGenerator();
            eCKeyPairGenerator.Init(ecKeygenParameters);
            var ecKeyPair = eCKeyPairGenerator.GenerateKeyPair();
           
            return ecKeyPair;
        }

        private void CreateKey(AsymmetricCipherKeyPair ecKeyPair)
        {
            
        }
    }
}
