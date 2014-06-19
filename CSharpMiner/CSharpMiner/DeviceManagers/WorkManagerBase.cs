using CSharpMiner;
using CSharpMiner.Stratum;
using MiningDevice;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace DeviceManager
{
    public delegate void SubmitMinerWorkDelegate(PoolWork work, string nonce);

    [DataContract]
    public abstract class WorkManagerBase : IMiningDeviceManager
    {
        [DataMember(Name = "pools")]
        public Pool[] Pools { get; set; }

        [DataMember(Name = "devices")]
        public IMiningDevice[] MiningDevices { get; private set; }

        [IgnoreDataMember]
        public Pool ActivePool { get; private set; }

        [IgnoreDataMember]
        public int ActivePoolId { get; private set; }

        protected Stack workStack = null;
        bool working = false;
        bool started = false;

        protected abstract void StartWork(PoolWork work);
        protected abstract void NoWork(PoolWork oldWork);

        public void NewWork(object[] poolWorkData)
        {
            if (started && ActivePool != null && workStack != null)
            {
                PoolWork newWork = new PoolWork(poolWorkData, ActivePool.Extranonce1, "00000000");

                // Pool asked us to toss out our old work
                if (poolWorkData[8].Equals(true))
                {
                    Program.DebugConsoleLog("New block!");

                    workStack.Clear();

                    working = true;
                    StartWork(newWork);
                }
                else // We can keep the old work
                {
                    if (!working)
                    {
                        working = true;
                        StartWork(newWork);
                    }
                    else
                    {
                        workStack.Push(newWork);
                    }
                }
            }
        }

        public void SubmitWork(PoolWork work, string nonce)
        {
            if (started && this.ActivePool != null && workStack != null)
            {
                this.ActivePool.SubmitWork(work.JobId, work.Extranonce2, work.Timestamp, nonce);

                if (workStack.Count != 0)
                {
                    // Start working on the last thing the server sent us
                    StartWork(workStack.Pop() as PoolWork);
                }
                else
                {
                    working = false;
                    NoWork(work);
                }
            }
        }

        public void Start()
        {
            workStack = Stack.Synchronized(new Stack());

            Task.Factory.StartNew(() =>
            {
                foreach(IMiningDevice d in this.MiningDevices)
                {
                    d.Load(this.SubmitWork);
                }

                if(Pools.Length > 0)
                {
                    this.ActivePool = Pools[0];
                    this.ActivePoolId = 0;
                    this.ActivePool.Start(this.NewWork, this.PoolDisconnected);
                }

                started = true;
            });
        }

        public void Stop()
        {
            started = false;

            foreach(IMiningDevice d in this.MiningDevices)
            {
                d.Unload();
            }

            if(this.ActivePool != null)
            {
                this.ActivePool.Stop();
                this.ActivePool.Thread.Join();

                this.ActivePool = null;
            }
        }

        public void PoolDisconnected()
        {
            // TODO: Handle when all pools are unable to be reached
            if(this.started && this.ActivePool != null)
            {
                this.ActivePool = null;

                if(this.ActivePoolId + 1 < this.Pools.Length)
                {
                    this.ActivePoolId++;
                }
                else
                {
                    this.ActivePoolId = 0;
                }

                this.ActivePool = this.Pools[this.ActivePoolId];
                this.ActivePool.Start(this.NewWork, this.PoolDisconnected);
            }
        }
    }
}
