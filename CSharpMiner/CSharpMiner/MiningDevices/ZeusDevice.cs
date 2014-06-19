using CSharpMiner.Stratum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace MiningDevice
{
    [DataContract]
    class ZeusDevice : UsbMinerBase
    {
        public override void StartWork(PoolWork work)
        {
            throw new NotImplementedException();
        }
    }
}
