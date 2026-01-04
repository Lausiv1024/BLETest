using MessagePack;
using System;
using System.Collections.Generic;
using System.Text;

namespace BLETest.Common.ComModel
{
    [MessagePackObject]
    public class ServerVerifyData
    {
        [Key(10)]
        public Guid ServerId;
        [Key(11)]
        public byte[] ServerIdSignature;
    }
}
