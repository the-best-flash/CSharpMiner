using CSharpMiner.Stratum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpMiner.MiningDevices
{
    public class TestDeviceManager : IMiningDeviceManager
    {
        public string ExtraNonce1 { get; private set; }
        public SubmitWorkDelegate SubmitWorkDelegate { get; private set; }
        public IEnumerable<IMiningDevice> MiningDevices { get; private set; }

        private bool started = false;
        public bool Started { get { return started; } }

        public void NewWork(object[] poolWorkData)
        {
            PoolWork work = new PoolWork(poolWorkData, this.ExtraNonce1, "00000000");

            Console.WriteLine("JobID: {0}", work.JobId);
            Console.WriteLine("Prev: {0}", work.PreviousHash);
            Console.WriteLine("coinb1: {0}", work.Coinbase1);
            Console.WriteLine("coinb2: {0}", work.Coinbase2);
            Console.WriteLine("merkel: {0}", work.MerkelRoot);
            Console.WriteLine("version: {0}", work.Version);
            Console.WriteLine("nbits: {0}", work.NetworkDiff);
            Console.WriteLine("ntime: {0}", work.Timestamp);
            Console.WriteLine("clean_jobs: {0}", poolWorkData[8]);
        }

        public void SubmitWork(PoolWork work, string nonce)
        {
            // Do nothing
        }

        public void Start(string extraNonce1, SubmitWorkDelegate submitWork)
        {
            started = true;
        }

        public void Stop()
        {
            started = false;
        }
    }
}
