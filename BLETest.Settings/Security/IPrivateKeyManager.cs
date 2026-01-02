using System;
using System.Collections.Generic;
using System.Text;

namespace BLETest.Settings.Security
{
    public interface IPrivateKeyManager
    {
        string GetPrivateKey(Guid guid);
        void AddPrivateKey(Guid guid, string privateKey);
    }
}
