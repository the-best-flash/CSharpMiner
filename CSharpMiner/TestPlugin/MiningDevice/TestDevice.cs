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
using System;
using System.Runtime.Serialization;
using System.Timers;

namespace MiningDevice
{
    [DataContract]
    public class TestDevice : MiningDeviceBase
    {
        [DataMember(Name = "path")]
        public string Path { get; set; }

        [IgnoreDataMember]
        public override string Name
        {
            get { return string.Format("TestDevice{0}", this.Id); }
        }

        public TestDevice(string path)
        {
            Path = path;
        }

        public override void Load()
        {
            base.Load();

            LogHelper.ConsoleLogAsync(string.Format("Loading Miner {0}", Path), LogVerbosity.Verbose);
        }

        public override void Unload()
        {
            base.Load();

            LogHelper.ConsoleLogAsync(string.Format("Unloading Miner {0}", Path), LogVerbosity.Verbose);
        }

        public override void StartWork(IPoolWork work)
        {
            LogHelper.ConsoleLogAsync(new object[]{
                    string.Format("Miner {0} starting work {1} with:", Path, work.JobId),
                    string.Format("\tDiff: {0}", work.Diff),
                    string.Format("\tHeader: {0}", work.Header)
                }, 
                LogVerbosity.Verbose);
        }

        public override void WorkRejected(IPoolWork work)
        {
            throw new NotImplementedException();
        }

        protected override void OnDeserializing()
        {
            // Do nothing
        }

        public override void StartWork(IPoolWork work, long startingNonce, long endingNonce)
        {
            LogHelper.ConsoleLogAsync(new object[]{
                    string.Format("Miner {0} starting work {1} with:", Path, work.JobId),
                    string.Format("\tDiff: {0}", work.Diff),
                    string.Format("\tHeader: {0}", work.Header),
                    string.Format("\tStartNonce: {0,8:X8}", startingNonce),
                    string.Format("\tEndNonce: {0,8:X8}", endingNonce)
                },
                LogVerbosity.Verbose);
        }
    }
}
