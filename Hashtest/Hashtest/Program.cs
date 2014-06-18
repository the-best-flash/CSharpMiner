using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Hashtest
{
    class Program
    {
        private static byte[] ConvertFromHexString(string hex)
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

        private static string ConvertToHexString(byte[] binary)
        {
            StringBuilder sb = new StringBuilder(binary.Length * 2);

            foreach(byte b in binary)
            {
                string hex = Convert.ToString(b, 16);

                if (hex.Length < 2)
                    sb.Append("0");

                sb.Append(hex);
            }

            return sb.ToString();
        }

        public static string JobId { get; private set; }
        public static string PreviousHash { get; private set; }
        public static string Coinbase1 { get; private set; }
        public static string Coinbase2 { get; private set; }
        public static string[] MerkelBranch { get; private set; }
        public static string Version { get; private set; }
        public static string NetworkDiff { get; private set; } // nbits
        public static string Timestamp { get; private set; }
        public static string Extranonce1 { get; private set; }
        public static string Extranonce2 { get; private set; }

        private static SHA256 _sha256 = null;
        public static SHA256 SHA256Hash
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

        private static string ComputeMerkelRoot()
        {
            string coinbase = string.Format("{0}{1}{2}{3}", Coinbase1, Extranonce1, Extranonce2, Coinbase2);
            byte[] coinbaseBinary = ConvertFromHexString(coinbase);

            SHA256 sha256 = SHA256Hash;
            byte[] merkelRoot = sha256.ComputeHash(sha256.ComputeHash(coinbaseBinary));

            Console.WriteLine("Hash: {0}", ConvertToHexString(merkelRoot));

            foreach (string str in MerkelBranch)
            {
                merkelRoot = sha256.ComputeHash(sha256.ComputeHash(merkelRoot.Concat(ConvertFromHexString(str)).ToArray()));
                Console.WriteLine("Root: {0}", ConvertToHexString(merkelRoot));
            }

            return ConvertToHexString(merkelRoot);
        }

        static void Main(string[] args)
        {
            PreviousHash = "7dcf1304b04e79024066cd9481aa464e2fe17966e19edf6f33970e1fe0b60277";
            Coinbase1 = "01000000010000000000000000000000000000000000000000000000000000000000000000ffffffff270362f401062f503253482f049b8f175308";
            Coinbase2 = "0d2f7374726174756d506f6f6c2f000000000100868591052100001976a91431482118f1d7504daf1c001cbfaf91ad580d176d88ac00000000";
            MerkelBranch = new string[]{ 
                "57351e8569cb9d036187a79fd1844fd930c1309efcd16c46af9bb9713b6ee734", 
                "936ab9c33420f187acae660fcdb07ffdffa081273674f0f41e6ecc1347451d23"
                                      };
            Version = "00000002";
            NetworkDiff = "1b44dfdb";
            Timestamp = "53178f9b";

            Extranonce1 = "f8002c90"; // from earlier
            Extranonce2 = "00000002"; // can be anything, but 4 bytes

            Console.WriteLine("End: {0}", ComputeMerkelRoot());
        }
    }
}
