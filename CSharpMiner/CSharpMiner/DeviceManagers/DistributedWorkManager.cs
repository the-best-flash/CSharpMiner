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
        protected override void StartWork(PoolWork work, bool restartWork)
        {
            throw new NotImplementedException();
        }

        protected override void NoWork(PoolWork oldWork, int deviceId)
        {
            throw new NotImplementedException();
        }
    }
}
