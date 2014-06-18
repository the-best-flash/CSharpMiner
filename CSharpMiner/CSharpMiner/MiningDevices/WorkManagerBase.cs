using CSharpMiner.Stratum;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpMiner.MiningDevices
{
    public abstract class WorkManagerBase : IMiningDeviceManager
    {
        public string ExtraNonce1 { get; private set; }
        public SubmitWorkDelegate SubmitWorkDelegate { get; private set; }
        public IEnumerable<IMiningDevice> MiningDevices { get; private set; }

        public bool Started { get { return started; } }

        protected Stack workStack = Stack.Synchronized(new Stack());
        bool working = false;
        bool started = false;

        protected abstract void StartWork(PoolWork work);
        protected abstract void NoWork(PoolWork oldWork);

        public void NewWork(object[] poolWorkData)
        {
            if (started)
            {
                PoolWork newWork = new PoolWork(poolWorkData, this.ExtraNonce1, "00000000");

                // Pool asked us to toss out our old work
                if (poolWorkData[8].Equals(true))
                {
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
            if (started)
            {
                this.SubmitWorkDelegate(work.JobId, work.Extranonce2, work.Timestamp, nonce);

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

        public void Start(string extraNonce1, SubmitWorkDelegate submitWork)
        {
            if(string.IsNullOrEmpty(extraNonce1))
            {
                throw new ArgumentNullException("extraNonce1");
            }

            if(submitWork == null)
            {
                throw new ArgumentNullException("submitWork");
            }

            ExtraNonce1 = extraNonce1;
            SubmitWorkDelegate = submitWork;

            started = true;

            // TODO: Load devices
        }

        public void Stop()
        {
            started = false;

            // TODO: Unload devices
        }
    }
}
