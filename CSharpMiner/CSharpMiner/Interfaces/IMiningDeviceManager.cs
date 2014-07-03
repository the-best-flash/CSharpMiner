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

using System;
using System.Collections.Generic;

namespace CSharpMiner.Interfaces
{
    public interface IMiningDeviceManager
    {
        event Action<IPool, IPoolWork, IMiningDevice> WorkAccepted;
        event Action<IPool, IPoolWork, IMiningDevice, IShareResponse> WorkRejected;
        event Action<IPool, IPoolWork, IMiningDevice> WorkDiscarded; // Usually due to hardware error
        event Action<IPool, IPoolWork, bool> NewWorkRecieved;
        event Action<IMiningDeviceManager, IMiningDevice> DeviceConnected;
        event Action<IMiningDeviceManager, IMiningDevice> DeviceDisconnected;
        event Action<IPool> PoolConnected;
        event Action<IPool> PoolDisconnected;
        event Action<IMiningDeviceManager> Started;
        event Action<IMiningDeviceManager> Stopped;

        IEnumerable<IPool> Pools { get; }
        IEnumerable<IMiningDevice> Devices { get; }

        IEnumerable<IPool> ActivePools { get; }

        void AddNewPool(IPool pool);
        void AddNewDevice(IMiningDevice device);

        void RemovePool(IPool pool);
        void RemoveDevice(IMiningDevice device);

        void Start();
        void Stop();
    }
}
