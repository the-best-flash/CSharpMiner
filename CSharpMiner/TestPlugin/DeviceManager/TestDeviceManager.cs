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
using CSharpMiner.ModuleLoading;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace DeviceManager
{
    [DataContract]
    [MiningModule(Description = "Since there wasn't any data member information specified the program attempts to auto generate it without descriptions.")]
    public class TestDeviceManager : IMiningDeviceManager
    {
        [DataMember(Name = "pools")]
        public IPool[] Pools { get; set; }

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
            List<Object> list = new List<object>();

            foreach(object obj in poolWorkData)
            {
                list.Add(string.Format("Work Param: {0}", obj));
            }

            list.Add(string.Format("Work Diff: {0}", diff));

            LogHelper.ConsoleLog(list.ToArray());
        }

        public void SubmitWork(IPoolWork work, string nonce, int startWork)
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

        public void AddNewPool(IPool pool)
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
