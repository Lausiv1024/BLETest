using System;

namespace BLETest.Settings
{
    public class BLESettings
    {
        public static readonly Guid ServiceId = Guid.Parse("000c61ed-9516-40b5-88ca-6658629826d3");
        public static readonly Guid BleCommunicationCCharacteristic = Guid.Parse("fa78938a-adf8-414e-b42d-0b195f6b368f");
        public static readonly Guid ServiceIdEsp = Guid.Parse("b4d9bf8b-7751-4914-b0fd-71b63a32e266");
        public static readonly Guid BleCommunicationCCharacteristicEsp = Guid.Parse("a3fc6c40-b765-4945-883e-917003099b5e");
    }
}
