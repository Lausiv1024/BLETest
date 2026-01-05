using BLETest.Common.ComModel;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Text;

namespace BLETest.Common
{
    public class Util
    {
        /// <summary>
        /// Tick(100ns)をミリ秒に変換
        /// </summary>
        /// <param name="ticks"></param>
        /// <returns></returns>
        public static int ToMilliseconds(long ticks)
        {
            return (int)TimeSpan.FromTicks(ticks).TotalMilliseconds;
        }
        public static string ExecutingDirectory()
        {
            return AppDomain.CurrentDomain.BaseDirectory;
        }

        public static CommandType GetCommandType(byte[] data)
        {
            if (data.Length < 1)
            {
                throw new ArgumentException("Data is too short to determine command type.");
            }
            var com = MessagePackSerializer.Deserialize<CommunicationBase>(data);
            return com.Command;
        }

        public static bool CanUnpack(byte[] data, CommandType command)
        {
            var com = MessagePackSerializer.Deserialize<CommunicationBase>(data);
            return com.Command == command;
        }

        public static byte[] PackComData<T>(T com)where T : CommunicationBase
        {
            return MessagePackSerializer.Serialize<T>(com);
        }

        public static T UnpackComData<T>(byte[] data) where T : CommunicationBase
        {
            return MessagePackSerializer.Deserialize<T>(data);
        }
    }
}
