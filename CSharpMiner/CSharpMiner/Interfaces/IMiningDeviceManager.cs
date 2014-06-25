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

using System.Collections.Generic;

namespace CSharpMiner.Interfaces
{
    public interface IMiningDeviceManager
    {
        IEnumerable<IMiningDevice> LoadedDevices { get; }
        IPool[] Pools { get; }

        void AddNewPool(IPool pool);
        void AddNewDevice(IMiningDevice device);

        void RemovePool(IPool pool);
        void RemoveDevice(IMiningDevice device);

        void Start();
        void Stop();
    }
}
