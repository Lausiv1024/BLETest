using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;
using Org.BouncyCastle.Asn1.Pkcs;
using System;
using System.Collections.Generic;
using System.Text;

namespace BLETest.Settings
{
    public class CryptoUtil
    {
        public static AsymmetricCipherKeyPair GenerateECDHKeyPair()
        {
            var gen = new Org.BouncyCastle.Crypto.Generators.ECKeyPairGenerator();
            var secureRandom = new SecureRandom();
            var keyGenParam = new KeyGenerationParameters(secureRandom, 256);
            gen.Init(keyGenParam);
            var keyPair = gen.GenerateKeyPair();
            return keyPair;
        }

        public static byte[] PubKeyToByte(AsymmetricKeyParameter pubKey)
        {
            var pubKeyInfo = SubjectPublicKeyInfoFactory.CreateSubjectPublicKeyInfo(pubKey)
                .GetDerEncoded();
            return pubKeyInfo;
        }

        public static AsymmetricKeyParameter ByteToPubKey(byte[] pubKeyBytes)
        {
            var p = PublicKeyFactory.CreateKey(pubKeyBytes);
            return p;
        }

        public static byte[] PriKeyToByte(AsymmetricKeyParameter priKey)
        {
            var priKeyInfo = PrivateKeyInfoFactory.CreatePrivateKeyInfo(priKey)
                .GetDerEncoded();
            return priKeyInfo;
        }
        public static AsymmetricKeyParameter ByteToPriKey(byte[] priKeyBytes)
        {
            var p = PrivateKeyFactory.CreateKey(priKeyBytes);
            return p;
        }

        public static byte[] PriKeyToEncryptedByte(AsymmetricKeyParameter priKey, string password)
        {
            var random = new SecureRandom();
            var encryptedPriKeyInfo = PrivateKeyFactory.EncryptKey(
                PkcsObjectIdentifiers.PbeWithShaAnd3KeyTripleDesCbc,
                password.ToCharArray(),salt: random.GenerateSeed(16), iterationCount: 1024,
                priKey);
            return encryptedPriKeyInfo;
        }

        public static AsymmetricKeyParameter EncryptedByteToPriKey(byte[] encryptedPriKeyBytes, string password)
        {
            var p = PrivateKeyFactory.DecryptKey(
                password.ToCharArray(),
                encryptedPriKeyBytes);
            return p;
        }
    }
}
