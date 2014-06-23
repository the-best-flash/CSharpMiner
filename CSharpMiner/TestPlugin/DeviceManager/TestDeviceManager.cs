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

using CSharpMiner;
using CSharpMiner.Helpers;
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

namespace DeviceManager
{
    [DataContract]
    [MiningModule(Description = "Since there wasn't any data member information specified the program attempts to auto generate it without descriptions.")]
    public class TestDeviceManager : IMiningDeviceManager
    {
        [DataMember(Name = "pools")]
        public StratumPool[] Pools { get; set; }

        [IgnoreDataMember]
        public IMiningDevice[] MiningDevices { get; private set; }

        [IgnoreDataMember]
        public string ExtraNonce1 { get; private set; }

        [IgnoreDataMember]
        public Action<string, string, string, string> SubmitWorkAction { get; private set; }

        private bool started = false;
        [IgnoreDataMember]
        public bool Started { get { return started; } }

        public IEnumerable<IMiningDevice> LoadedDevices
        {
            get { return MiningDevices; }
        }

        public void NewWork(object[] poolWorkData, int diff)
        {
            StratumWork work = new StratumWork(poolWorkData, this.ExtraNonce1, "00000000", diff);

            LogHelper.ConsoleLog(new Object[] {
                string.Format("JobID: {0}", work.JobId),
                string.Format("Prev: {0}", work.PreviousHash),
                string.Format("coinb1: {0}", work.Coinbase1),
                string.Format("coinb2: {0}", work.Coinbase2),
                string.Format("merkle: {0}", work.MerkleRoot),
                string.Format("version: {0}", work.Version),
                string.Format("nbits: {0}", work.NetworkDiff),
                string.Format("ntime: {0}", work.Timestamp),
                string.Format("clean_jobs: {0}", poolWorkData[8])
            });
        }

        public void SubmitWork(StratumWork work, string nonce, int startWork)
        {
            // Do nothing
        }

        public void Start()
        {
            started = true;

            if(Pools.Length > 0)
            {
                Pools[0].Disconnected += this.PoolDisconnected;
                Pools[0].Start();
            }
        }

        public void Stop()
        {
            if (started && Pools.Length > 0)
            {
                Pools[0].Stop();
            }

            started = false;
        }


        public void PoolDisconnected(IPool pool)
        {
            if (started && Pools.Length > 0)
            {
                LogHelper.ConsoleLogAsync(string.Format("Pool {0} disconnected...", Pools[0].Url), LogVerbosity.Quiet);
            }
        }

        public void RequestWork(int deviceId)
        {
            // do nothing
        }

        public void AddNewPool(StratumPool pool)
        {
            throw new NotImplementedException();
        }

        public void AddNewDevice(IMiningDevice device)
        {
            throw new NotImplementedException();
        }

        public void RemovePool(int poolIndex)
        {
            throw new NotImplementedException();
        }

        public void RemoveDevice(int deviceIndex)
        {
            throw new NotImplementedException();
        }
    }
}
