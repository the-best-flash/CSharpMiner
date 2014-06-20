using MiningDevice;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeviceLoader
{
    public interface IHotplugLoader : IMiningDevice, IDisposable
    {
        void StartListening(Action<IMiningDevice> newMiningDevice);
        void StopListening();
    }
}
