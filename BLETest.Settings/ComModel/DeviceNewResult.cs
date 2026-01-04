using MessagePack;
using System;
using System.Collections.Generic;
using System.Text;

namespace BLETest.Common.ComModel
{
    [MessagePackObject]
    public class DeviceNewResult: CommunicationBase
    {
        [Key(2)]
        public bool IsSuccess { get; set; }
    }
}
