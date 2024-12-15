using System;
using System.Collections.Generic;
using System.Text;

namespace BLETest.Settings
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
    }
}
