using System;
using System.Collections.Generic;
using System.Text;
using MessagePack;
namespace BLETest.Common.ComModel
{
    [MessagePackObject]
    public class CommunicationBase
    {
        [Key(0)]
        public int Id { get; set; }
        [Key(1)]
        public long Timestamp { get; set; }
    }
}
