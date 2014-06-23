/*  Copyright (C) 2014 Colton Manville
    This file is part of CSharpMiner.

    CSharpMiner is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    CSharpMiner is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with CSharpMiner.  If not, see <http://www.gnu.org/licenses/>.*/

using CSharpMiner.ModuleLoading;
using CSharpMiner.Pools;
using MiningDevice;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace DeviceLoader
{
    [DataContract]
    public abstract class USBDeviceLoader : IDeviceLoader
    {
        [DataMember(Name = "cores")]
        [MiningSetting(ExampleValue = "6", Optional = false, Description = "Core count. The meaning of this setting is manufacturer specific.")]
        public int Cores { get; set; }

        [DataMember(Name = "ports", IsRequired=true)]
        [MiningSetting(ExampleValue = "[\"/dev/ttyUSB0\", \"/dev/ttyUSB1\", \"COM1\"]", Optional = false, Description = "List of ports devices are connected to. Linux /dev/tty* and Windows COM*")]
        public string[] Ports { get; set; }

        [DataMember(Name = "timeout")]
        [MiningSetting(ExampleValue = "60", Optional = true, Description = "Number of seconds to wait without response before restarting the device.")]
        public int WatchdogTimeout { get; set; }

        [DataMember(Name = "poll")]
        [MiningSetting(ExampleValue = "50", Optional = true, Description = "Milliseconds the thread waits before looking for incoming data. A larger value will decrease the processor usage but shares won't be submitted right away.")]
        public int PollFrequency { get; set; }

        [IgnoreDataMember]
        public int Id { get; set; }

        [IgnoreDataMember]
        public System.Timers.Timer WorkRequestTimer
        {
            get { throw new NotImplementedException(); }
        }

        [IgnoreDataMember]
        public int HashRate
        {
            get { throw new NotImplementedException(); }
        }

        [IgnoreDataMember]
        public int HardwareErrors
        {
            get { throw new NotImplementedException(); }
        }

        [IgnoreDataMember]
        public int Accepted
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        [IgnoreDataMember]
        public int Rejected
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        [IgnoreDataMember]
        public int AcceptedWorkUnits
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        [IgnoreDataMember]
        public int RejectedWorkUnits
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        [IgnoreDataMember]
        public int DiscardedWorkUnits
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public event Action<IMiningDevice, IPoolWork, string> ValidNonce;
        public event Action<IMiningDevice> WorkRequested;
        public event Action<IMiningDevice, IPoolWork> InvalidNonce;

        public abstract IEnumerable<IMiningDevice> LoadDevices();

        public void Unload()
        {
            throw new NotImplementedException();
        }

        public void StartWork(IPoolWork work)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            // Do Nothing
        }

        public void Load()
        {
            throw new NotImplementedException();
        }


        public void WorkRejected(IPoolWork work)
        {
            throw new NotImplementedException();
        }
    }
}
