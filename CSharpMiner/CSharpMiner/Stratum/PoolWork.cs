using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using CSharpMiner.Helpers;

namespace CSharpMiner.Stratum
{
    public class PoolWork
    {
        Object _lock = new Object();

        public Object[] CommandArray { get; private set; }

        public string JobId { get; private set; }
        public string PreviousHash { get; private set; }
        public string Coinbase1 { get; private set; }
        public string Coinbase2 { get; private set; }
        public string[] MerkelBranch { get; private set; }
        public string Version { get; private set; }
        public string NetworkDiff { get; private set; } // nbits
        public string Timestamp { get; private set; }
        public string Extranonce1 { get; private set; }
        public int StartingNonce { get; private set; }

        private string _extranonce2 = null;
        public string Extranonce2 
        { 
            get
            {
                return _extranonce2;
            }

            set
            {
                _merkelRoot = null; // Clear out the old value since it is invalid
                _extranonce2 = value;
            }
        }

        private string _merkelRoot = null;
        public string MerkelRoot
        {
            get
            {
                if(_merkelRoot == null)
                {
                    lock (_lock)
                    {
                        if (_merkelRoot == null) // Just in case a thread was waiting on the lock. No sense in recomputing the merkelRoot
                        {
                            _merkelRoot = ComputeMerkelRoot();
                        }
                    }
                }

                return _merkelRoot;
            }
        }

        private static SHA256 _sha256 = null;
        public static SHA256 SHA256Hash
        {
            get
            {
                if(_sha256 == null)
                {
                    _sha256 = SHA256.Create();
                }

                return _sha256;
            }
        }

        public PoolWork(Object[] serverCommandArray, string extranonce1, string extranonce2, int startingNonce = 0)
        {
            if(serverCommandArray.Length < 8)
            {
                throw new ArgumentException("Unrecognized work format from server. Work array length < 8.");
            }

            CommandArray = serverCommandArray;

            Extranonce1 = extranonce1;
            Extranonce2 = extranonce2;

            StartingNonce = startingNonce;

            JobId = serverCommandArray[0] as string;
            PreviousHash = serverCommandArray[1] as string;
            Coinbase1 = serverCommandArray[2] as string;
            Coinbase2 = serverCommandArray[3] as string;
            Version = serverCommandArray[5] as string;
            NetworkDiff = serverCommandArray[6] as string;
            Timestamp = serverCommandArray[7] as string;

            Object[] merkelTreeParts = serverCommandArray[4] as Object[];

            if(merkelTreeParts != null)
            {
                MerkelBranch = new string[merkelTreeParts.Length];

                for(int i = 0; i < merkelTreeParts.Length; i++)
                {
                    MerkelBranch[i] = merkelTreeParts[i] as string;
                }
            }
            else
            {
                throw new ArgumentException("Unrecognized work format from server. Merkel_Branch is not an array.");
            }
        }

        private string ComputeMerkelRoot()
        {
            string coinbase = string.Format("{0}{1}{2}{3}", Coinbase1, Extranonce1, Extranonce2, Coinbase2);
            byte[] coinbaseBinary = HexConversionHelper.ConvertFromHexString(coinbase);

            SHA256 sha256 = SHA256Hash;
            byte[] merkelRoot = sha256.ComputeHash(sha256.ComputeHash(coinbaseBinary));

            foreach (string str in MerkelBranch)
            {
                merkelRoot = sha256.ComputeHash(sha256.ComputeHash(merkelRoot.Concat(HexConversionHelper.ConvertFromHexString(str)).ToArray()));
            }

            return HexConversionHelper.ConvertToHexString(merkelRoot);
        }
    }
}
