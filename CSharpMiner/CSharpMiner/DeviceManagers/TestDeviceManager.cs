using CSharpMiner;
using CSharpMiner.Helpers;
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

            LogHelper.ConsoleLog(new Object[] {
                string.Format("JobID: {0}", work.JobId),
                string.Format("Prev: {0}", work.PreviousHash),
                string.Format("coinb1: {0}", work.Coinbase1),
                string.Format("coinb2: {0}", work.Coinbase2),
                string.Format("merkle: {0}", work.MerkleRoot),
                string.Format("version: {0}", work.Version),
                string.Format("nbits: {0}", work.NetworkDiff),
                string.Format("ntime: {0}", work.Timestamp),
                string.Format("clean_jobs: {0}", poolWorkData[8])
            });
        }

        public void SubmitWork(PoolWork work, string nonce, int startWork)
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
                LogHelper.ConsoleLogAsync(string.Format("Pool {0} disconnected...", Pools[0].Url), LogVerbosity.Quiet);
            }
        }

        public void RequestWork(int deviceId)
        {
            // do nothing
        }
    }
}
