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

using CSharpMiner.Helpers;
using CSharpMiner.Interfaces;
using System;
using System.Linq;

namespace Stratum
{
    public class StratumWork : IPoolWork
    {
        private const string WorkLogFile = "work.log";

        public Object[] CommandArray { get; private set; }

        public string JobId { get; private set; }
        public string PreviousHash { get; private set; }
        public string Coinbase1 { get; private set; }
        public string Coinbase2 { get; private set; }
        public string[] MerkleBranch { get; private set; }
        public string Version { get; private set; }
        public string NetworkDiff { get; private set; } // nbits

        private int _diff;
        public int Diff
        {
            get
            {
                return _diff;
            }

            set
            {
                _diff = value;
                _target = null;
            }
        }

        private string _timestamp;
        public string Timestamp 
        { 
            get
            {
                return _timestamp;
            }

            private set
            {
                _timestamp = value;
                this.ClearHeaderData();
            }
        }

        private string _extranonce1;
        public string Extranonce1
        {
            get
            {
                return _extranonce1;
            }

            set
            {
                this.ClearHeaderData();
                _extranonce1 = value;
            }
        }

        private string _extranonce2 = null;
        public string Extranonce2
        {
            get
            {
                return _extranonce2;
            }

            set
            {
                // Clear out the old value since it is invalid
                this.ClearHeaderData();
                _extranonce2 = value;
            }
        }

        public int ExtraNonce2Size { get; private set; }

        private string _merkleRoot = null;
        public string MerkleRoot
        {
            get
            {
                if (_merkleRoot == null)
                {
                    _merkleRoot = ComputeMerkleRoot();
                }

                return _merkleRoot;
            }
        }

        private byte[] _midState = null;
        public byte[] Midstate
        {
            get
            {
                if(_midState == null)
                {
                    _midState = ComputeMidstate();
                }

                return _midState;
            }
        }

        private byte[] _target = null;
        public byte[] Target
        {
            get
            {
                if(_target == null)
                    _target = MathHelper.ConvertDifficultyToTarget(this.Diff);

                return _target;
            }
        }

        private byte[] _header = null;
        public byte[] Header
        {
            get
            {
                if (_header == null)
                {
                    _header = HexConversionHelper.ConvertFromHexString(MakeHeader());
                }

                return _header;
            }
        }

        private void ClearHeaderData()
        {
            _merkleRoot = null;
            _header = null;
        }

        public StratumWork(Object[] serverCommandArray, string extranonce1, int extranonce2Size, string extranonce2, int diff)
        {
            if (serverCommandArray.Length < 8)
            {
                Exception e = new ArgumentException("Unrecognized work format from server. Work array length < 8.");
                LogHelper.LogErrorSecondary(e);
                throw e;
            }

            CommandArray = serverCommandArray.Clone() as object[];

            Extranonce1 = extranonce1;
            ExtraNonce2Size = extranonce2Size;

            // Make sure extranonce 2 is the right length
            if (extranonce2.Length != extranonce2Size * 2)
            {
                int expectedSize = extranonce2Size * 2;
                if (expectedSize > extranonce2.Length)
                {
                    int neededZeros = expectedSize - extranonce2.Length;
                    string fix = extranonce2;

                    for(int i = 0; i < neededZeros; i++)
                    {
                        fix = "0" + fix;
                    }

                    Extranonce2 = fix;
                }
                else
                {
                    Extranonce2 = extranonce2.Substring(extranonce2.Length - expectedSize, expectedSize);
                }
            }
            else
            {
                Extranonce2 = extranonce2;
            }

            Diff = diff;

            JobId = serverCommandArray[0] as string;
            PreviousHash = serverCommandArray[1] as string;
            Coinbase1 = serverCommandArray[2] as string;
            Coinbase2 = serverCommandArray[3] as string;
            Version = serverCommandArray[5] as string;
            NetworkDiff = serverCommandArray[6] as string;
            Timestamp = serverCommandArray[7] as string;

            Object[] merkleTreeParts = serverCommandArray[4] as Object[];

            if (merkleTreeParts != null)
            {
                MerkleBranch = new string[merkleTreeParts.Length];

                for (int i = 0; i < merkleTreeParts.Length; i++)
                {
                    MerkleBranch[i] = merkleTreeParts[i] as string;
                }
            }
            else
            {
                Exception e = new ArgumentException("Unrecognized work format from server. Merkle_Branch is not an array.");
                LogHelper.LogErrorSecondary(e);
                throw e;
            }

            LogHelper.DebugLogToFileAsync(new Object[] {
                "Work:",
                string.Format("  nonce1: {0}", Extranonce1),
                string.Format("  nonce2: {0}", Extranonce2),
                string.Format("  nonce2_size: {0}", ExtraNonce2Size),
                string.Format("  diff: {0}", Diff),
                string.Format("  id: {0}", JobId),
                string.Format("  prevHash: {0}", PreviousHash),
                string.Format("  coinb1: {0}", Coinbase1),
                string.Format("  coinb2: {0}", Coinbase2),
                string.Format("  version: {0}", Version),
                string.Format("  nbits: {0}", NetworkDiff),
                string.Format("  ntime: {0}", Timestamp)
            }, WorkLogFile);
        }

        private byte[] ComputeMidstate()
        {
            byte[] headerBytes = this.Header;
            return HashHelper.ComputeMidstate(headerBytes.Take(64).ToArray());
        }

        private string ComputeMerkleRoot()
        {
            string coinbase = string.Format("{0}{1}{2}{3}", Coinbase1, Extranonce1, Extranonce2, Coinbase2);
            return HashHelper.ComputeMerkleRoot(coinbase, MerkleBranch);
        }

        private string MakeHeader()
        {
            return string.Format("{0}{1}{2}{3}{4}", Version, PreviousHash, MerkleRoot, Timestamp, NetworkDiff);
        }

        public void SetTimestamp(uint timestamp)
        {
            string value = string.Format("{0:X8}", timestamp);
            this.CommandArray[7] = value;
            this.Timestamp = value;
        }

        public object Clone()
        {
            return new StratumWork(this.CommandArray, this.Extranonce1, this.ExtraNonce2Size, this.Extranonce2, this.Diff);
        }
    }
}
