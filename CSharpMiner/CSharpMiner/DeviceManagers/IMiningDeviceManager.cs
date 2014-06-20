using CSharpMiner.Stratum;
using MiningDevice;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeviceManager
{
    public interface IMiningDeviceManager
    {
        IMiningDevice[] MiningDevices { get; }
        Pool[] Pools { get; }

        void NewWork(Object[] poolWorkData, int diff);
        void SubmitWork(PoolWork work, string nonce);
        void PoolDisconnected();
        void Start();
        void Stop();
    }
}
