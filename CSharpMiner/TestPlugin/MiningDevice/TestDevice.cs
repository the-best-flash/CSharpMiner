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
using System;
using System.Runtime.Serialization;
using System.Timers;

namespace MiningDevice
{
    [DataContract]
    public class TestDevice : IMiningDevice
    {
        [DataMember(Name = "path")]
        public string Path { get; set; }

        [DataMember(Name = "cores")]
        public int Cores { get; set; }

        [IgnoreDataMember]
        public int Id { get; set; }

        [IgnoreDataMember]
        public int HashRate
        {
            get { return 1; }
        }

        [IgnoreDataMember]
        public int HardwareErrors { get; set; }

        [IgnoreDataMember]
        public Timer WorkRequestTimer
        {
            get { return new Timer(); }
        }

        [IgnoreDataMember]
        public int Accepted { get; set; }

        [IgnoreDataMember]
        public int Rejected { get; set; }

        [IgnoreDataMember]
        public int AcceptedWorkUnits { get; set; }

        [IgnoreDataMember]
        public int RejectedWorkUnits { get; set; }

        [IgnoreDataMember]
        public int DiscardedWorkUnits { get; set; }

        public double AcceptedHashRate
        {
            get { return 0.0; }
        }

        public double RejectedHashRate
        {
            get { return 0.0; }
        }

        public double DiscardedHashRate
        {
            get { return 0.0; }
        }

        [IgnoreDataMember]
        public string Name
        {
            get { return string.Format("TestDevice{0}", this.Id); }
        }

        public event Action<IMiningDevice, IPoolWork, string> ValidNonce;
        public event Action<IMiningDevice> WorkRequested;
        public event Action<IMiningDevice, IPoolWork> InvalidNonce;

        public TestDevice(string path, int cores)
        {
            Path = path;
            Cores = cores;
        }

        public void Load()
        {
            LogHelper.ConsoleLogAsync(string.Format("Loading Miner {0}", Path), LogVerbosity.Verbose);
        }

        public void Unload()
        {
            LogHelper.ConsoleLogAsync(string.Format("Unloading Miner {0}", Path), LogVerbosity.Verbose);
        }

        public void StartWork(IPoolWork work)
        {
            LogHelper.ConsoleLogAsync(new object[]{
                    string.Format("Miner {0} starting work {1} with:", Path, work.JobId),
                    string.Format("\tStartNonce:  {0}", work.StartingNonce),
                    string.Format("\tDiff: {0}", work.Diff),
                    string.Format("\tHeader: {0}", work.Header)
                }, 
                LogVerbosity.Verbose);
        }

        public void Dispose()
        {
            this.Unload();
        }

        public void WorkRejected(IPoolWork work)
        {
            throw new NotImplementedException();
        }
    }
}
