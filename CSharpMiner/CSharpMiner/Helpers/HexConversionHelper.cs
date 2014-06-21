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

        public static string Swap(string hex)
        {
            StringBuilder sb = new StringBuilder(hex.Length);

            // Split into sections of 8 and then reverse the bytes in thoes sections
            for (int i = 0; i < hex.Length; i += 8)
            {
                for (int j = i + 6; j >= i; j -= 2)
                {
                    sb.Append(hex.Substring(j, 2));
                }
            }

            return sb.ToString();
        }

        public static string Reverse(string hex)
        {
            if ((hex.Length & 0x1) != 0)
            {
                throw new ArgumentException("Hex must have a length that is a multiple of two.");
            }

            StringBuilder sb = new StringBuilder(hex.Length);

            for (int i = hex.Length - 2; i >= 0; i -= 2)
            {
                sb.Append(hex.Substring(i, 2));
            }

            return sb.ToString();
        }
    }
}
