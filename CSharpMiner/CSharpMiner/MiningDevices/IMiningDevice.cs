using CSharpMiner.Stratum;
using DeviceManager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace MiningDevice
{
    public interface IMiningDevice : IDisposable
    {
        int Id { get; set; }
        int Cores { get; }
        int HashRate { get; }
        int HardwareErrors { get; }
        Timer WorkRequestTimer { get; }

        void Load(Action<PoolWork, string, int> submitWork, Action<int> requestWork);
        void Unload();
        void StartWork(PoolWork work);
    }
}
