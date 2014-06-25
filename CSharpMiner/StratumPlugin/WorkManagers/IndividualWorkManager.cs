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

using CSharpMiner.DeviceManager;
using CSharpMiner.Helpers;
using CSharpMiner.Interfaces;
using CSharpMiner.ModuleLoading;
using Stratum;
using System;
using System.Runtime.Serialization;

namespace StratumManager
{
    [DataContract]
    [MiningModule(Description = "Uses the stratum protocol to generate a unique work item for each device it manages. It will allow the device to continue working on its work item until one of the following occurs: the device requests a new work item, the server forces a work restart, or the device submits a stale share.")]
    public class IndividualWorkManager : WorkManagerBase
    {
        private const int defaultWorkRestartTimeout = 180000;

        private static Random _random = null;
        private static Random Random
        {
            get
            {
                if(_random == null)
                {
                    _random = new Random();
                }

                return _random;
            }
        }

        [IgnoreDataMember]
        public override IPool[] Pools
        {
            get
            {
                return StratumPools;
            }
        }

        [DataMember(Name = "pools", IsRequired = true)]
        [MiningSetting(Description = "A collection of pools to connect to. This connects to the first pool and only uses the other pools if the first one fails. It does not automatically go back to the first pool if it becomes available again.", Optional = false)]
        public StratumPool[] StratumPools { get; set; }

        [DataMember(IsRequired=false, Name="forceRestart")]
        [MiningSetting(Description = "Restarts work on all devices even if the server indicates that a work restart is optional. [Default = true]", Optional = true, ExampleValue="false")]
        public bool AlwaysForceRestart { get; set; }

        private StratumWork currentWork = null;

        [OnDeserialized]
        private void OnDeserializing(StreamingContext context)
        {
            SetDefaultValues();
        }

        private void SetDefaultValues()
        {
            AlwaysForceRestart = true;
        }

        protected override void SetUpDevice(IMiningDevice d)
        {
            if (d.HashRate > 0)
            {
                double fullHashTimeSec = Int32.MaxValue / d.HashRate; // Hashes devided by Hashes per second yeilds seconds
                double safeWaitTime = fullHashTimeSec * 0.85 * 0.95; // Assume we lose 15% of our hash rate just in case then only wait until we've covered 95% of the hash space
                d.WorkRequestTimer.Interval = safeWaitTime;
            }
            else
            {
                d.WorkRequestTimer.Interval = defaultWorkRestartTimeout; // Unknown hashrate. Assume we need to restart the device every few minutes
            }
        }

        private int startingNonce = 0;

        protected override void StartWork(IPoolWork work, IMiningDevice device, bool restartAll, bool requested)
        {
            StratumWork stratumWork = work as StratumWork;

            if (stratumWork != null)
            {
                startingNonce = Random.Next();
                StartWorkOnDevice(stratumWork, device, (restartAll || (AlwaysForceRestart && (currentWork == null || currentWork.JobId != work.JobId))), requested);

                currentWork = stratumWork;
            }
        }

        protected override void NoWork(IPoolWork oldWork, IMiningDevice device, bool requested)
        {
            StratumWork stratumWork = oldWork as StratumWork;

            if (stratumWork != null)
            {
                StartWorkOnDevice(stratumWork, device, false, requested);
            }
        }

        protected override void OnWorkRejected(IPool pool, IPoolWork work, IMiningDevice device, IShareResponse response)
        {
            base.OnWorkRejected(pool, work, device, response);

            if(response.JobNotFound || response.RejectReason.Contains("job not found"))
            {
                LogHelper.ConsoleLog(string.Format("Device {0} submitted stale share. Restarting with new work.", device.Name), LogVerbosity.Verbose);
                this.RequestWork(device);
            }
        }

        private void StartWorkOnDevice(StratumWork work, IMiningDevice device, bool restartWork, bool requested)
        {
            if (!restartWork && device != null && requested)
            {
                StartWorkOnDevice(device, work.CommandArray, work.Extranonce1, work.ExtraNonce2Size, work.Diff);
            }
            else if(restartWork)
            {
                StartWorking(work.CommandArray, work.Extranonce1, work.ExtraNonce2Size, work.Diff);
            }
        }

        private void StartWorking(object[] param, string extranonce1, int extraNonce2Size, int diff)
        {
            foreach (IMiningDevice device in this.loadedDevices)
            {
                StartWorkOnDevice(device, param, extranonce1, extraNonce2Size, diff);
            }
        }

        private void StartWorkOnDevice(IMiningDevice device, object[] param, string extranonce1, int extraNonce2Size, int diff)
        {
            device.StartWork(new StratumWork(param, extranonce1, extraNonce2Size, string.Format("{0,8:X8}", startingNonce), diff));
            if (startingNonce != int.MaxValue)
            {
                startingNonce++;
            }
            else
            {
                startingNonce = 0;
            }
        }
    }
}
