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

            Console.WriteLine("Hash: {0}", Swap(ConvertToHexString(merkelRoot)));

            foreach (string str in MerkelBranch)
            {
                merkelRoot = sha256.ComputeHash(sha256.ComputeHash(merkelRoot.Concat(ConvertFromHexString(str)).ToArray()));
                Console.WriteLine("Root: {0}", Swap(ConvertToHexString(merkelRoot)));
            }

            return Swap(ConvertToHexString(merkelRoot));
        }

        static void Main(string[] args)
        {
            PreviousHash = "ffe0e3e6648893da746496964fb1cf15ad5ed89cbdf083ca16e185c8ac55b58f";
            Coinbase1 = "01000000010000000000000000000000000000000000000000000000000000000000000000ffffffff2702d923062f503253482f043514a55308";
            Coinbase2 = "0e2f434d757332313534393433332f000000000100e40b54020000001976a914e94614a433193d57b9d52cf4e37ab0fcde1514f188ac00000000";
            MerkelBranch = new string[0];
            Version = "00000002";
            NetworkDiff = "1b302fee";
            Timestamp = "53a51430";

            Extranonce1 = "7000f209"; // from earlier
            Extranonce2 = "01000000"; // can be anything, but 4 bytes 

            /*PreviousHash = "7cffb738392e95757bf0af9b91358a188083ef1fcce77d4cb3245cbd78463145";
            Coinbase1 = "01000000010000000000000000000000000000000000000000000000000000000000000000ffffffff230362ff08062f503253482f045f14a55308";
            Coinbase2 = "092f7374726174756d2f00000000014069212a010000001976a914c8f58075fdf2ba12619f34d15385567e5a1cb99488ac00000000";
            MerkelBranch = new string[]{
                "4de1ee1e9c77a9ab5ee0c87dbc8fd4ed51835acb6eafdf826c28028dd8de1866",
                "b992b340fb3bcc750355c3f123e21df0379badbe178c2f44b64b794c1aa29b73",
                "5c8977dc75f5cb482c02411e0f330c47d4686d37e550a6c52ac1086d3264926d",
                "07878c61e904097127ce2578354f715c28b151238e75ec1f3e19d1f7841463d9"
            };
            Version = "00000002";
            NetworkDiff = "1b056b20";
            Timestamp = "53a51444"; 

            Extranonce1 = "0817258e"; // from earlier
            Extranonce2 = "00000000"; // can be anything, but 4 bytes */

            Console.WriteLine("End: {0}", ComputeMerkelRoot());

            Console.WriteLine();
            Console.WriteLine(MakeHeader());

            Console.WriteLine();
            Console.WriteLine(Reverse(MakeHeader()));
        }

        private static string Swap(string hex)
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

        private static string Reverse(string hex)
        {
            if((hex.Length & 0x1) != 0)
            {
                throw new ArgumentException("Hex must have a length that is a multiple of two.");
            }

            StringBuilder sb = new StringBuilder(hex.Length);

            for(int i = hex.Length - 2; i >= 0; i -= 2)
            {
                sb.Append(hex.Substring(i, 2));
            }

            return sb.ToString();
        }

        private static string MakeHeader()
        {
            return string.Format("{0}{1}{2}{3}{4}", Version, PreviousHash, ComputeMerkelRoot(), Timestamp, NetworkDiff);
        }
    }
}
