using System;
using System.Collections.Generic;
using System.Text;
using System.Security;
using System.Security.Cryptography;
using Org.BouncyCastle.Crypto.Parameters;
namespace BLETest.Common.Crypto
{
    public class KeyAgreementSystem
    {
        ECPrivateKeyParameters myPrivateKey;
        ECPublicKeyParameters myPublicKey;
        ECPublicKeyParameters senderPublicKey;
        public KeyAgreementSystem()
        {

        }
    }
}
