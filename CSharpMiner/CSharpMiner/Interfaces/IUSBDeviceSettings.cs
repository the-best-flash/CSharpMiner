using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpMiner.Interfaces
{
    public interface IUSBDeviceSettings
    {
        int Cores { get; set; }
        string Port { get; set; }
        int WatchdogTimeout { get; set; }
        int PollFrequency { get; set; }
    }
}
