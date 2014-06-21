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
    public class IndividualWorkManager : WorkManagerBase
    {
        private static Random _random = null;
        private static Random Random
        {
            get
            {
                if(_random == null)
                {
                    _random = new Random();
                }

                return _random;
            }
        }

        private int startingNonce = 0;

        protected override void StartWork(PoolWork work, bool restartWork)
        {
            startingNonce = Random.Next();
            StartWorking(work.CommandArray, work.Extranonce1, work.Diff);
        }

        protected override void NoWork(PoolWork oldWork)
        {
            if (this._submissionQueue != null && this._submissionQueue.Count != 0)
            {
                StartWorkOnDevice(this.loadedDevices[(int)this._submissionQueue.Dequeue()], oldWork.CommandArray, oldWork.Extranonce1, oldWork.Diff);
            }
            else
            {
                StartWorking(oldWork.CommandArray, oldWork.Extranonce1, oldWork.Diff);
            }
        }

        private void StartWorking(object[] param, string extranonce1, int diff)
        {
            foreach (IMiningDevice device in this.loadedDevices)
            {
                StartWorkOnDevice(device, param, extranonce1, diff);
            }
        }

        private void StartWorkOnDevice(IMiningDevice device, object[] param, string extranonce1, int diff)
        {
            device.StartWork(new PoolWork(param, extranonce1, string.Format("{0,8:X8}", startingNonce), diff));
            if (startingNonce != int.MaxValue)
            {
                startingNonce++;
            }
            else
            {
                startingNonce = 0;
            }
        }
    }
}
