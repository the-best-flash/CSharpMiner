using CSharpMiner.Stratum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace DeviceManager
{
    [DataContract]
    public class DistributedWorkManager : WorkManagerBase
    {
        protected override void StartWork(PoolWork work, int deviceId, bool restartWork, bool requested)
        {
            throw new NotImplementedException();
        }

        protected override void NoWork(PoolWork oldWork, int deviceId, bool requested)
        {
            throw new NotImplementedException();
        }

        protected override void SetUpDevice(MiningDevice.IMiningDevice d)
        {
            throw new NotImplementedException();
        }
    }
}
