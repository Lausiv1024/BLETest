using MessagePack;
using System;
using System.Collections.Generic;
using System.Text;

namespace BLETest.Common.ComModel
{
    internal class DeviceNewData : CommunicationBase
    {
        [Key(3)]
        public Guid DeviceId { get; set; }
        [Key(4)]
        public string DeviceName { get; set; }
        = string.Empty;
        [Key(5)]
        public byte[] MPubKey { get; set; } //クライアントからのデータを検証するための署名検証用公開鍵
    }
}
