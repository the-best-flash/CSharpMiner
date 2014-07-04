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
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Gridseed
{
    [DataContract]
    class MultiGridseedDevice : MiningDeviceBase, IGridseedDeviceSettings
    {
        private const string defaultNameFormat = "Gridseeds({0})";

        [DataMember(Name = "ports", IsRequired = true)]
        [MiningSetting(ExampleValue = "[\"/dev/ttyUSB0\", \"/dev/ttyUSB1\", \"COM1\"]", Optional = false, Description = "List of ports devices are connected to. Linux /dev/tty* and Windows COM*")]
        public string[] Ports { get; set; }

        [DataMember(Name = "poll")]
        [MiningSetting(ExampleValue = "50", Optional = true, Description = "Milliseconds the thread waits before looking for incoming data. A larger value will decrease the processor usage but shares won't be submitted right away.")]
        public int PollFrequency { get; set; }

        [DataMember(Name = "chips", IsRequired = true)]
        [MiningSetting(ExampleValue = GridseedDevice.chipsExampleString, Optional = false, Description = GridseedDevice.chipsDescriptionString)]
        public int Chips { get; set; }

        [DataMember(Name = "freq", IsRequired = true)]
        [MiningSetting(ExampleValue = "850", Optional = false, Description = GridseedDevice.freqDescriptionString)]
        public int Frequency { get; set; }

        private int _lastDeviceCount;
        private string _name;
        [IgnoreDataMember]
        public override string Name
        {
            get 
            {
                if (_name == null || _lastDeviceCount != this.LoadedDevices.Count)
                    _name = string.Format(defaultNameFormat, LoadedDevices.Count);

                _lastDeviceCount = this.LoadedDevices.Count;

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

        private bool _loaded;
        private int _totalChips;
        private Thread _dataPollThread;

        public override void Load()
        {
            _loaded = false;

            base.Load();

            _totalChips = 0;

            Parallel.ForEach(Ports, port =>
                {
                    GridseedDevice device = new GridseedDevice(port, Frequency, Chips, WatchdogTimeout, PollFrequency);

                    device.InvalidNonce += OnDeviceInvalidNonce;
                    device.Disconnected += OnDeviceDisconnected;
                    device.ValidNonce += OnDeviceValidNonce;
                    device.WorkRequested += OnDeviceWorkRequested;

                    device.Load(false);

                    LoadedDevices.Add(device);
                });

            foreach(GridseedDevice d in LoadedDevices)
            {
                this.HashRate += d.HashRate;
                _totalChips += d.Chips;
            }

            _loaded = true;

            if(_dataPollThread == null)
            {
                _dataPollThread = new Thread(new ThreadStart(this.CheckForData));
                _dataPollThread.Start();
            }

            this.OnConnected();
        }

        private void CheckForData()
        {
            while (_loaded)
            {
                Parallel.ForEach(LoadedDevices, device =>
                {
                    device.CheckForData();
                });

                Thread.Sleep(this.PollFrequency);
            }
        }

        void OnDeviceWorkRequested(IMiningDevice obj)
        {
            this.RequestWork();
        }

        void OnDeviceValidNonce(IMiningDevice arg1, IPoolWork arg2, string arg3)
        {
            this.OnValidNonce(arg2, arg3);
        }

        void OnDeviceDisconnected(IMiningDevice obj)
        {
            LogHelper.LogError(string.Format("Device {0} disconnected!", obj.Name));
        }

        void OnDeviceInvalidNonce(IMiningDevice arg1, IPoolWork arg2)
        {
            this.OnInvalidNonce(arg2);
        }

        public override void WorkRejected(IPoolWork work)
        {
            LogHelper.DebugConsoleLog(string.Format("Device {0} requesting new work since it had rejected work.", this.Name));
            this.RequestWork();
        }

        public override void StartWork(IPoolWork work)
        {
            DivideUpNonces(work, 0, 0xFFFFFFFF);
        }

        public override void StartWork(IPoolWork work, long startingNonce, long endingNonce)
        {
            DivideUpNonces(work, startingNonce, endingNonce);
        }

        private void DivideUpNonces(IPoolWork work, long startingNonce, long endingNonce)
        {
            double noncesPerChip = (endingNonce - startingNonce) / _totalChips;

            long start = 0;

            foreach (GridseedDevice d in LoadedDevices)
            {
                long end = (long)(start + d.Chips * noncesPerChip);

                d.StartWork(work, start, end);

                start = end + 1;
            }
        }

        public override void Reset()
        {
            Parallel.ForEach(LoadedDevices, device =>
                {
                    device.Reset();
                });
        }

        public override void Unload()
        {
            _loaded = false;
            _totalChips = 0;

            Parallel.ForEach(LoadedDevices, device =>
                {
                    device.Unload();
                });

            if(_dataPollThread != null)
            {
                _dataPollThread.Join(100);
                _dataPollThread.Abort();
                _dataPollThread = null;
            }

            base.Unload();
        }
    }
}
