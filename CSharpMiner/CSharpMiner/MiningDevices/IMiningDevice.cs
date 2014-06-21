using CSharpMiner.Stratum;
using DeviceManager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiningDevice
{
    public interface IMiningDevice : IDisposable
    {
        int Id { get; set; }
        int Cores { get; }
        int HashRate { get; }
        int HardwareErrors { get; }

        void Load(Action<PoolWork, string, int> submitWork);
        void Unload();
        void StartWork(PoolWork work);
    }
}
