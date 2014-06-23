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

using CSharpMiner.Interfaces;
using CSharpMiner.ModuleLoading;
using MiningDevice;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Timers;

namespace DeviceLoader
{
    [DataContract]
    [MiningModule(Description = "This will be displayed as the description of the object when the user asks for help.")]
    public class TestDeviceLoader : IDeviceLoader
    {
        [DataMember(Name = "ports")]
        [MiningSetting(ExampleValue="[A value that will be inserted into the example JSON section.]", Optional=false, Description="A value that will be displayed as the description of the setting.")]
        public string[] Ports { get; set; }

        [DataMember(Name = "cores")]
        [MiningSetting(ExampleValue="42", Optional=true, Description="Because Optional=true this will have the word '(Optional)' appended after the property type.")]
        public int Cores { get; set; }

        public IEnumerable<IMiningDevice> LoadDevices()
        {
            List<IMiningDevice> devices = new List<IMiningDevice>();

            foreach(string str in Ports)
            {
                devices.Add(new TestDevice(str, Cores));
            }

            return devices;
        }
    }
}
