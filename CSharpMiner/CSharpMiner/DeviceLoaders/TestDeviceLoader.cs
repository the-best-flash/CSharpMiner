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

using CSharpMiner.Stratum;
using MiningDevice;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace DeviceLoader
{
    [DataContract]
    public class TestDeviceLoader : IDeviceLoader
    {
        [DataMember(Name = "ports")]
        public string[] Ports { get; set; }

        [DataMember(Name = "cores")]
        public int Cores { get; set; }

        [IgnoreDataMember]
        public int Id { get; set; }

        [IgnoreDataMember]
        public Timer WorkRequestTimer
        {
            get { return new Timer(); }
        }

        public IEnumerable<MiningDevice.IMiningDevice> LoadDevices()
        {
            List<IMiningDevice> devices = new List<IMiningDevice>();

            foreach(string str in Ports)
            {
                devices.Add(new TestDevice(str, Cores));
            }

            return devices;
        }

        public int HashRate
        {
            get { throw new NotImplementedException(); }
        }

        public int HardwareErrors
        {
            get { throw new NotImplementedException(); }
        }

        public void Unload()
        {
            throw new NotImplementedException();
        }

        public void StartWork(CSharpMiner.Stratum.PoolWork work)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            
        }

        public void Load(Action<PoolWork, string, int> submitWork, Action<int> requestWork)
        {
            throw new NotImplementedException();
        }
    }
}
