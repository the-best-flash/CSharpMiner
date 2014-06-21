using CSharpMiner.Stratum;
using MiningDevice;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace DeviceManager
{
    [DataContract]
    public class TestDeviceManager : IMiningDeviceManager
    {
        [DataMember(Name = "pools")]
        public Pool[] Pools { get; set; }

        [IgnoreDataMember]
        public IMiningDevice[] MiningDevices { get; private set; }

        [IgnoreDataMember]
        public string ExtraNonce1 { get; private set; }

        [IgnoreDataMember]
        public Action<string, string, string, string> SubmitWorkAction { get; private set; }

        private bool started = false;
        [IgnoreDataMember]
        public bool Started { get { return started; } }

        public void NewWork(object[] poolWorkData, int diff)
        {
            PoolWork work = new PoolWork(poolWorkData, this.ExtraNonce1, "00000000", diff);

            Console.WriteLine("JobID: {0}", work.JobId);
            Console.WriteLine("Prev: {0}", work.PreviousHash);
            Console.WriteLine("coinb1: {0}", work.Coinbase1);
            Console.WriteLine("coinb2: {0}", work.Coinbase2);
            Console.WriteLine("merkle: {0}", work.MerkleRoot);
            Console.WriteLine("version: {0}", work.Version);
            Console.WriteLine("nbits: {0}", work.NetworkDiff);
            Console.WriteLine("ntime: {0}", work.Timestamp);
            Console.WriteLine("clean_jobs: {0}", poolWorkData[8]);
        }

        public void SubmitWork(PoolWork work, string nonce)
        {
            // Do nothing
        }

        public void Start()
        {
            started = true;

            if(Pools.Length > 0)
            {
                Pools[0].Start(this.NewWork, this.PoolDisconnected);
            }
        }

        public void Stop()
        {
            if (started && Pools.Length > 0)
            {
                Pools[0].Stop();
            }

            started = false;
        }


        public void PoolDisconnected()
        {
            if (started && Pools.Length > 0)
            {
                Console.WriteLine("Pool {0} disconnected...", Pools[0].Url);
            }
        }
    }
}
