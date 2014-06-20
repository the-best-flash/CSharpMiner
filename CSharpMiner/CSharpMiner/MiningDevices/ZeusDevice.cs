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
        [DataMember(Name = "clock")]
        public int LtcClk { get; set; }

        [DataMember(Name = "cores")]
        public int Cores { get; set; }

        public ZeusDevice(string port, int clk, int cores)
        {
            UARTPort = port;
            LtcClk = clk;
            Cores = cores;
        }

        public override void StartWork(PoolWork work)
        {
            throw new NotImplementedException();
        }
    }
}
