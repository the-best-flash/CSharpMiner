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
using CSharpMiner.Interfaces;
using CSharpMiner.ModuleLoading;
using Stratum;
using System;
using System.Runtime.Serialization;

namespace StratumManager
{
    [DataContract]
    [MiningModule(Description="This has not been implemented.")]
    public class DistributedWorkManager : WorkManagerBase
    {
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

        protected override void SetUpDevice(IMiningDevice d)
        {
            throw new NotImplementedException();
        }

        protected override void StartWork(IPoolWork work, IMiningDevice device, bool restartAll, bool requested)
        {
            throw new NotImplementedException();
        }

        protected override void NoWork(IPoolWork oldWork, IMiningDevice device, bool requested)
        {
            throw new NotImplementedException();
        }
    }
}
