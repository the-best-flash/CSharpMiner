using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpMiner.Stratum
{
    public class PoolWork
    {
        Object _lock = new Object();

        public string JobId { get; private set; }
        public string PreviousHash { get; private set; }
        public string Coinbase1 { get; private set; }
        public string Coinbase2 { get; private set; }
        public string[] MerkelBranch { get; private set; }
        public string Version { get; private set; }
        public string NetworkDiff { get; private set; } // nbits
        public string Timestamp { get; private set; }

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

        public PoolWork(Object[] serverCommandArray, string Extranonce1, string Extranonce2)
        {
            if(serverCommandArray.Length < 8)
            {
                throw new ArgumentException("Unrecognized work format from server. Work array length < 8.");
            }

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
            // TODO
            return null;
        }
    }
}
