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

using CSharpMiner.MiningDevice;
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
    class MultiGridseedDevice : MiningDeviceBase, IGridseedDeviceSettings
    {
        private const string defaultNameFormat = "Gridseeds({0})";

        [DataMember(Name = "chips", IsRequired = true)]
        [MiningSetting(ExampleValue = GridseedDevice.chipsExampleString, Optional = false, Description = GridseedDevice.chipsDescriptionString)]
        public int Chips { get; set; }

        [DataMember(Name = "freq", IsRequired = true)]
        [MiningSetting(ExampleValue = "850", Optional = false, Description = GridseedDevice.freqDescriptionString)]
        public int Frequency { get; set; }

        private int _lastDevices;
        private string _name;
        [IgnoreDataMember]
        public override string Name
        {
            get 
            {
                if (_name == null || _lastDevices != this.LoadedDevices.Count)
                    _name = string.Format(defaultNameFormat, LoadedDevices.Count);

                return _name;
            }
        }

        private List<GridseedDevice> _loadedDevices;
        private List<GridseedDevice> LoadedDevices
        {
            get
            {
                if (_loadedDevices == null)
                    _loadedDevices = new List<GridseedDevice>();

                return _loadedDevices;
            }
        }

        public override void WorkRejected(CSharpMiner.Interfaces.IPoolWork work)
        {
            throw new NotImplementedException();
        }

        public override void StartWork(CSharpMiner.Interfaces.IPoolWork work)
        {
            throw new NotImplementedException();
        }
    }
}
