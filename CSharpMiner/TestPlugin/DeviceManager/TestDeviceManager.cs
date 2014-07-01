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
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace DeviceManager
{
    [DataContract]
    [MiningModule(Description = "Since there wasn't any data member information specified the program attempts to auto generate it without descriptions.")]
    public class TestDeviceManager : WorkManagerBase
    {
        private bool started = false;

        [DataMember(Name = "pools")]
        public IPool[] PoolCollection { get; set; }

        [IgnoreDataMember]
        public override IPool[] Pools
        {
            get
            {
                return PoolCollection;
            }
        }

        [IgnoreDataMember]
        public string ExtraNonce1 { get; private set; }

        [IgnoreDataMember]
        public Action<string, string, string, string> SubmitWorkAction { get; private set; }

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

        public override void Start()
        {
            base.Start();

            started = true;
        }

        public override void Stop()
        {
            base.Stop();

            started = false;
        }

        public override void OnPoolDisconnected(IPool pool)
        {
            if (started && Pools.Length > 0)
            {
                LogHelper.ConsoleLog(string.Format("Pool {0} disconnected...", Pools[0].Url), LogVerbosity.Quiet);
            }

            base.OnPoolDisconnected(pool);
        }

        public void RequestWork(int deviceId)
        {
            // do nothing
        }

        protected override void StartWork(IPoolWork work, IMiningDevice device, bool restartAll, bool requested)
        {
            // do nothing
        }

        protected override void NoWork(IPoolWork oldWork, IMiningDevice device, bool requested)
        {
            // do nothing
        }

        protected override void SetUpDevice(IMiningDevice d)
        {
            // do nothing
        }

        protected override void OnWorkUpdateTimerExpired()
        {
            // do nothing
        }
    }
}
