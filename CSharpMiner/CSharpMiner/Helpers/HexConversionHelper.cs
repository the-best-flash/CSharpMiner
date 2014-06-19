using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpMiner.Helpers
{
    public static class HexConversionHelper
    {
        public static byte[] ConvertFromHexString(string hex)
        {
            if ((hex.Length & 0x1) != 0)
            {
                throw new ArgumentException("Input hex string must have even length.");
            }

            int finalLength = hex.Length / 2;
            byte[] result = new byte[finalLength];

            for (int i = 0; i < hex.Length; i += 2)
            {
                result[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            }

            return result;
        }

        public static string ConvertToHexString(byte[] binary)
        {
            StringBuilder sb = new StringBuilder(binary.Length * 2);

            foreach (byte b in binary)
            {
                string hex = Convert.ToString(b, 16);

                if (hex.Length < 2)
                    sb.Append("0");

                sb.Append(hex);
            }

            return sb.ToString();
        }
    }
}
