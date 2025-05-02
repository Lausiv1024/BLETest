using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto.Parameters;
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

        public static ECDomainParameters K283DomainParameters()
        {
            var ecParams = ECNamedCurveTable.GetByName("K-283");
            return new ECDomainParameters(ecParams);
        }
    }
}
