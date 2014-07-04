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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Gridseed
{
    [DataContract]
    class GridseedDeviceLoader : USBDeviceLoader, IGridseedDeviceSettings
    {
        [DataMember(Name = "chips", IsRequired = true)]
        [MiningSetting(ExampleValue = GridseedDevice.chipsExampleString, Optional = false, Description = GridseedDevice.chipsDescriptionString)]
        public int Chips { get; set; }

        [DataMember(Name = "freq", IsRequired = true)]
        [MiningSetting(ExampleValue = "850", Optional = false, Description = GridseedDevice.freqDescriptionString)]
        public int Frequency { get; set; }

        public override IEnumerable<IMiningDevice> LoadDevices()
        {
            List<IMiningDevice> devices = new List<IMiningDevice>();

            foreach (string p in Ports)
            {
                devices.Add(new GridseedDevice(p, Frequency, Chips, WatchdogTimeout, PollFrequency));
            }

            return devices;
        }
    }
}
