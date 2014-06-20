using MiningDevice;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeviceLoader
{
    public interface IDeviceLoader : IMiningDevice
    {
        IEnumerable<IMiningDevice> LoadDevices();
    }
}
