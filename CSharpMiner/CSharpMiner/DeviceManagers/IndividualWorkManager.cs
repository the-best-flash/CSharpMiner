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

        protected override void StartWork(PoolWork work)
        {
            startingNonce = Random.Next();
            StartWorking(work.CommandArray, work.Extranonce1);
        }

        protected override void NoWork(PoolWork oldWork)
        {
            StartWorking(oldWork.CommandArray, oldWork.Extranonce1);
        }

        private void StartWorking(object[] param, string extranonce1)
        {
            foreach (IMiningDevice device in this.loadedDevices)
            {
                device.StartWork(new PoolWork(param, extranonce1, string.Format("{0,8:X8}", startingNonce)));
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
}
