using MessagePack;
using System;
using System.Collections.Generic;
using System.Text;

namespace BLETest.Common.ComModel
{
    [MessagePackObject]
    public class DeviceNewData : CommunicationBase
    {
        [Key(2)]
        public Guid DeviceId { get; set; }
        [Key(3)]
        public byte[] MPubKey { get; set; } //クライアントからのデータを検証するための署名検証用公開鍵
        [Key(4)]
        public byte[] DeviceIdSig { get; set; } //DeviceIdに対する署名
    }
}
