using System;
using System.Collections.Generic;
using System.Text;
using MessagePack;
namespace BLETest.Common.ComModel
{
    internal class CommunicationBase
    {
        [Key(0)]
        public int Id { get; }
        [Key(1)]
        public long Timestamp { get; }
    }
}
