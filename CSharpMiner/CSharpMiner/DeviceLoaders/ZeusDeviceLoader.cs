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
using CSharpMiner.Stratum;
using DeviceManager;
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
    [MiningModule(Description = "Configures a collection ZeusMiner Gen1 or GAWMiner A1 devices using the same settings.")]
    public class ZeusDeviceLoader : USBDeviceLoader
    {
        [DataMember(Name = "clock")]
        [MiningSetting(ExampleValue="328", Description="The clockspeed of the miner. Max = 382", Optional=true)]
        public int LtcClk { get; set; }

        public override IEnumerable<IMiningDevice> LoadDevices()
        {
            List<IMiningDevice> devices = new List<IMiningDevice>();

            foreach (string p in Ports)
            {
                devices.Add(new ZeusDevice(p, LtcClk, Cores, WatchdogTimeout, PollFrequency));
            }

            return devices;
        }
    }
}
