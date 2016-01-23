/*  Copyright (C) 2014 Colton Manville
    This file is part of CSharpMiner.

    CSharpMiner is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    CSharpMiner is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with CSharpMiner.  If not, see <http://www.gnu.org/licenses/>.*/

using System;
using System.Text;

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
                sb.Append(b.ToString("x2"));
            }

            return sb.ToString();
        }

        public static string ConvertToHexString(uint[] binary)
        {
            StringBuilder sb = new StringBuilder(binary.Length * 8);

            foreach (uint b in binary)
            {
                sb.Append(b.ToString("x8"));
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
