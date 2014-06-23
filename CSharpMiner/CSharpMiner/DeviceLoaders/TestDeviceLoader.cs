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
    [MiningModule(Description = "This will be displayed as the description of the object when the user asks for help.")]
    public class TestDeviceLoader : IDeviceLoader
    {
        [DataMember(Name = "ports")]
        [MiningSetting(ExampleValue="[A value that will be inserted into the example JSON section.]", Optional=false, Description="A value that will be displayed as the description of the setting.")]
        public string[] Ports { get; set; }

        [DataMember(Name = "cores")]
        [MiningSetting(ExampleValue="42", Optional=true, Description="Because Optional=true this will have the word '(Optional)' appended after the property type.")]
        public int Cores { get; set; }

        [IgnoreDataMember]
        public int Id { get; set; }

        [IgnoreDataMember]
        public Timer WorkRequestTimer
        {
            get { return new Timer(); }
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

        public void StartWork(IPoolWork work)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            
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
