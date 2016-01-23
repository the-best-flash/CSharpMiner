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
using System.Linq;
using System.Security.Cryptography;

namespace CSharpMiner.Helpers
{
    public static class HashHelper
    {
        private static SHA256 _sha256 = null;
        public static SHA256 SHA256
        {
            get
            {
                if (_sha256 == null)
                {
                    _sha256 = SHA256.Create();
                }

                return _sha256;
            }
        }

        public static string ComputeMerkleRoot(string root, string[] branches)
        {
            byte[] rootBinary = HexConversionHelper.ConvertFromHexString(root);

            SHA256 sha256 = HashHelper.SHA256;
            byte[] merkleRoot = sha256.ComputeHash(sha256.ComputeHash(rootBinary));

            foreach (string str in branches)
            {
                merkleRoot = sha256.ComputeHash(sha256.ComputeHash(merkleRoot.Concat(HexConversionHelper.ConvertFromHexString(str)).ToArray()));
            }

            return HexConversionHelper.Swap(HexConversionHelper.ConvertToHexString(merkleRoot));
        }

        // From Wikipedia sha256 pseudocode
        private static uint[] h = { 0x6a09e667, 0xbb67ae85, 
                                    0x3c6ef372, 0xa54ff53a,
                                    0x510e527f, 0x9b05688c, 
                                    0x1f83d9ab, 0x5be0cd19 };

        // From Wikipedia sha256 pseudocode
        private static uint[] k = { 0x428a2f98, 0x71374491, 0xb5c0fbcf, 0xe9b5dba5, 0x3956c25b, 0x59f111f1, 0x923f82a4, 0xab1c5ed5,
                                    0xd807aa98, 0x12835b01, 0x243185be, 0x550c7dc3, 0x72be5d74, 0x80deb1fe, 0x9bdc06a7, 0xc19bf174,
                                    0xe49b69c1, 0xefbe4786, 0x0fc19dc6, 0x240ca1cc, 0x2de92c6f, 0x4a7484aa, 0x5cb0a9dc, 0x76f988da,
                                    0x983e5152, 0xa831c66d, 0xb00327c8, 0xbf597fc7, 0xc6e00bf3, 0xd5a79147, 0x06ca6351, 0x14292967,
                                    0x27b70a85, 0x2e1b2138, 0x4d2c6dfc, 0x53380d13, 0x650a7354, 0x766a0abb, 0x81c2c92e, 0x92722c85,
                                    0xa2bfe8a1, 0xa81a664b, 0xc24b8b70, 0xc76c51a3, 0xd192e819, 0xd6990624, 0xf40e3585, 0x106aa070,
                                    0x19a4c116, 0x1e376c08, 0x2748774c, 0x34b0bcb5, 0x391c0cb3, 0x4ed8aa4a, 0x5b9cca4f, 0x682e6ff3,
                                    0x748f82ee, 0x78a5636f, 0x84c87814, 0x8cc70208, 0x90befffa, 0xa4506ceb, 0xbef9a3f7, 0xc67178f2 };

        private static uint RotateRight(uint i, int n)
        {
            return (i >> n) | (i << (32 - n));
        }

        public static byte[] ComputeMidstate(byte[] data)
        {
            if (data.Length != 64)
            {
                throw new ArgumentException("Data must have a length of 64.");
            }

            // From Wikipedia sha256 pseudocode
            uint[] W = new uint[64];
            uint[] Work = h.Clone() as uint[];

            int idx;
            uint tmp, tmp2, tmp3;
            uint temp1, temp2;
            uint s0;
            uint s1;
            uint ch;
            uint maj;
            int i;

            for (i = 0; i < 16; i++)
            {
                idx = i * 4;

                W[i] = ((uint)data[idx + 3] << 24) | ((uint)data[idx + 2] << 16) | ((uint)data[idx + 1] << 8) | ((uint)data[idx]);
            }

            for (i = 16; i < 64; i++)
            {
                tmp = W[i - 15];
                s0 = RotateRight(tmp, 7) ^ RotateRight(tmp, 18) ^ (tmp >> 3);

                tmp = W[i - 2];
                s1 = RotateRight(tmp, 17) ^ RotateRight(tmp, 19) ^ (tmp >> 10);

                W[i] = W[i - 16] + s0 + W[i - 7] + s1;
            }

            for (i = 0; i < 64; i++)
            {
                tmp = Work[4];
                s1 = RotateRight(tmp, 6) ^ RotateRight(tmp, 11) ^ RotateRight(tmp, 25);
                ch = (tmp & Work[5]) ^ ((~tmp) & Work[6]);
                temp1 = Work[7] + s1 + ch + k[i] + W[i];

                tmp = Work[0];
                s0 = RotateRight(tmp, 2) ^ RotateRight(tmp, 13) ^ RotateRight(tmp, 22);
                tmp2 = Work[1];
                tmp3 = Work[2];
                maj = (tmp & tmp2) ^ (tmp & tmp3) ^ (tmp2 & tmp3);
                temp2 = s0 + maj;

                Work[7] = Work[6];
                Work[6] = Work[5];
                Work[5] = Work[4];
                Work[4] = Work[3] + temp1;
                Work[3] = Work[2];
                Work[2] = Work[1];
                Work[1] = Work[0];
                Work[0] = temp1 + temp2;
            }

            byte[] midstate = new byte[32];

            for (i = 0; i < 8; i++)
            {
                tmp = h[i] + Work[i];

                idx = i * 4;

                midstate[idx] = (byte)(tmp);
                midstate[idx + 1] = (byte)(tmp >> 8);
                midstate[idx + 2] = (byte)(tmp >> 16);
                midstate[idx + 3] = (byte)(tmp >> 24);
            }

            return midstate;
        }

        public static string MegaHashDisplayString(double hashesPerSec)
        {
            if (!double.IsNaN(hashesPerSec))
            {
                double mHash = hashesPerSec / 1000000;

                return string.Format("{0:N2}Mh", mHash);
            }
            else
            {
                return "0.00Mh";
            }
        }
    }
}
