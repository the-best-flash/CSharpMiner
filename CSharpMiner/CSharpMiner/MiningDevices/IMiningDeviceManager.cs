using CSharpMiner.Stratum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpMiner.MiningDevices
{
    public interface IMiningDeviceManager
    {
        string ExtraNonce1 { get; }
        SubmitWorkDelegate SubmitWorkDelegate { get; }
        bool Started { get; }
        IEnumerable<IMiningDevice> MiningDevices { get; }

        void NewWork(Object[] poolWorkData);
        void SubmitWork(PoolWork work, string nonce);
        void Start(string extraNonce1, SubmitWorkDelegate submitWork);
        void Stop();
    }
}
