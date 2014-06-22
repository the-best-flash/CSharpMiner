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

namespace DeviceManager
{
    [DataContract]
    public class IndividualWorkManager : WorkManagerBase
    {
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

        protected override void SetUpDevice(IMiningDevice d)
        {
            double fullHashTimeSec = Int32.MaxValue / d.HashRate; // Hashes devided by Hashes per second yeilds seconds
            double safeWaitTime = fullHashTimeSec * 0.85 * 0.95; // Assume we lose 15% of our hash rate just in case then only wait until we've covered 95% of the hash space
            d.WorkRequestTimer.Interval = safeWaitTime;
        }

        private int startingNonce = 0;

        protected override void StartWork(PoolWork work, int deviceId, bool restartWork, bool requested)
        {
            startingNonce = Random.Next();
            StartWorkOnDevice(work, deviceId, restartWork, requested);
        }

        private void StartWorkOnDevice(PoolWork work, int deviceId, bool restartWork, bool requested)
        {
            if (!restartWork && deviceId >= 0 && deviceId < this.loadedDevices.Count && requested)
            {
                StartWorkOnDevice(this.loadedDevices[deviceId], work.CommandArray, work.Extranonce1, work.Diff);
            }
            else if(restartWork)
            {
                StartWorking(work.CommandArray, work.Extranonce1, work.Diff);
            }
        }

        protected override void NoWork(PoolWork oldWork, int deviceId, bool requested)
        {
            StartWorkOnDevice(oldWork, deviceId, false, requested);
        }

        private void StartWorking(object[] param, string extranonce1, int diff)
        {
            foreach (IMiningDevice device in this.loadedDevices)
            {
                StartWorkOnDevice(device, param, extranonce1, diff);
            }
        }

        private void StartWorkOnDevice(IMiningDevice device, object[] param, string extranonce1, int diff)
        {
            device.StartWork(new PoolWork(param, extranonce1, string.Format("{0,8:X8}", startingNonce), diff));
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
