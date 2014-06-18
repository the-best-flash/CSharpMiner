using CSharpMiner.Stratum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpMiner.MiningDevices
{
    public class IndividualWorkManager : WorkManagerBase
    {
        protected override void StartWork(PoolWork work)
        {
            throw new NotImplementedException();
        }

        protected override void NoWork(PoolWork oldWork)
        {
            throw new NotImplementedException();
        }
    }
}
