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
        [Key(2)]
        public CommandType Command { get; set; }
    }

    public enum CommandType
    {
        None = 0,
        DeviceNewData = 1,
        DeviceNewResult = 2,
        ServerVerifyData = 3,
    }
}
