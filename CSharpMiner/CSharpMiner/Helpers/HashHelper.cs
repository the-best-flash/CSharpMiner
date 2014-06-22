using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

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
    }
}
