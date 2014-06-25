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

using CSharpMiner.DeviceLoader;
using CSharpMiner.Interfaces;
using CSharpMiner.ModuleLoading;
using System.Collections.Generic;
using System.Runtime.Serialization;
using ZeusMinerGen1Plugin;

namespace ZeusMiner
{
    [DataContract]
    [MiningModule(Description = "Configures many ZeusMiner(Gen1) or GAWMiner(A1) devices at once.")]
    public class ZeusDeviceLoader : USBDeviceLoader, IZeusDeviceSettings
    {
        [DataMember(Name = "cores")]
        [MiningSetting(ExampleValue = "6", Optional = false, Description = "Number of ZeusChips in the device.")]
        public int Cores { get; set; }

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
