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

using CSharpMiner.Helpers;
using CSharpMiner.Interfaces;
using CSharpMiner.MiningDevice;
using CSharpMiner.ModuleLoading;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Gridseed
{
    [DataContract]
    public class GridseedDevice : UsbMinerBase, IGridseedDeviceSettings
    {
        internal const int defaultChips = 5;
        internal const int defaultFreq = 700;
        internal const int gBladeChips = 80;
        internal const string chipsExampleString = "5";
        internal const string chipsDescriptionString = "Number of Gridseed chips in the device. [Default = 5, GBlade = 80]";
        internal const string freqDescriptionString = "Core Clock in Mhz. [Default = 700, Recommended Max = 900]";

        private int _chips;
        [DataMember(Name = "chips", IsRequired = true)]
        [MiningSetting(ExampleValue = chipsExampleString, Optional = false, Description = chipsDescriptionString)]
        public int Chips
        {
            get
            {
                return _chips;
            }

            set
            {
                _chips = value;

                HashRate = this.GetExpectedHashrate();
            }
        }

        private int _freq;
        [DataMember(Name = "freq", IsRequired = true)]
        [MiningSetting(ExampleValue = "850", Optional = false, Description = freqDescriptionString)]
        public int Frequency
        {
            get
            {
                return _freq;
            }

            set
            {
                _freq = value;

                HashRate = this.GetExpectedHashrate();
            }
        }

        [IgnoreDataMember]
        public override int BaudRate
        {
            get
            {
                return 115200;
            }
        }

        private string _lastPort;
        private string _name;
        [IgnoreDataMember]
        public override string Name
        {
            get
            {
                if (_name == null || _lastPort == null || _lastPort != this.Port)
                    _name = string.Format("{0}-{1}", (this.Chips != gBladeChips ? "GC3355" : "GBlade"), this.Port);

                _lastPort = this.Port;

                return _name;
            }
        }

        public GridseedDevice(string port, int freq = defaultFreq, int chips = defaultChips, int watchdogTimeout = defaultWatchdogTimeout, int pollFrequency = defaultPollTime)
            : base(port, watchdogTimeout, pollFrequency)
        {
            this.Frequency = freq;
            this.Chips = chips;
        }

        private long GetExpectedHashrate()
        {
            return _chips;
        }

        protected override void DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            throw new NotImplementedException();
        }

        public override void WorkRejected(IPoolWork work)
        {
            LogHelper.DebugConsoleLog(string.Format("Device {0} requesting new work since it had rejected work.", this.Name));
            this.RequestWork();
        }

        public override void StartWork(IPoolWork work)
        {
            throw new NotImplementedException();
        }
    }
}
