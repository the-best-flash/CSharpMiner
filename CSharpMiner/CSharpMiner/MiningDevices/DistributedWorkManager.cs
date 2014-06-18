using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpMiner.MiningDevices
{
    public class DistributedWorkManager : WorkManagerBase
    {
        protected override void StartWork(Stratum.PoolWork work)
        {
            throw new NotImplementedException();
        }

        protected override void NoWork(Stratum.PoolWork oldWork)
        {
            throw new NotImplementedException();
        }
    }
}
