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
using System.Collections.Generic;

namespace CSharpMiner.Helpers
{
    public static class MathHelper
    {
        private static byte[] _baseDifficluty = new byte[] { 0x00, 0x00, 0x00, 0x00, 0xFF, 0xFF, 0x00, 0x00, 
                                                             0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 
                                                             0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 
                                                             0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };

        public static byte[] ConvertDifficultyToTarget(int diff)
        {
            return Divide(_baseDifficluty, diff);
        }

        public static byte[] Divide(byte[] dividend, int divisor)
        {
            if ((dividend.Length & 4) != 0)
            {
                throw new ArgumentException("Dividend length must be a multiple of 4.");
            }

            if (divisor == 0)
            {
                throw new DivideByZeroException();
            }

            // Do long division in base 4294967296 (Pretend every 32 bits is a single digit)

            List<byte> result = new List<byte>();

            long remainder = 0;
            int iterations = dividend.Length / 4;

            for (int i = 0; i < iterations; i++)
            {
                int idx = i * 4;
                long number = (remainder << 32) | ((long)dividend[idx] << 24) | ((long)dividend[idx + 1] << 16) | ((long)dividend[idx + 2] << 8) | (long)dividend[idx + 3];

                long quotient = number / divisor; // This will be a 32 bit number
                remainder = number - (quotient * divisor);

                result.Add((byte)(quotient >> 24));
                result.Add((byte)(quotient >> 16));
                result.Add((byte)(quotient >> 8));
                result.Add((byte)quotient);
            }

            return result.ToArray();
        }
    }
}
