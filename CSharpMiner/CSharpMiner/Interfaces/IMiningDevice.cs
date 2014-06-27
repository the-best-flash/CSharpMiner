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
using System.Timers;

namespace CSharpMiner.Interfaces
{
    public interface IMiningDevice : IMiningDeviceObject, IDisposable
    {
        long Id { get; set; }

        long HashRate { get; }

        long Accepted { get; set; }
        long Rejected { get; set; }
        long HardwareErrors { get; set; }

        long AcceptedWorkUnits { get; set; }
        long RejectedWorkUnits { get; set; }
        long DiscardedWorkUnits { get; set; }

        double AcceptedHashRate { get; }
        double RejectedHashRate { get; }
        double DiscardedHashRate { get; }

        Timer WorkRequestTimer { get; }

        string Name { get; }

        event Action<IMiningDevice, IPoolWork, string> ValidNonce;
        event Action<IMiningDevice> WorkRequested;
        event Action<IMiningDevice, IPoolWork> InvalidNonce;
        event Action<IMiningDevice> Disconnected;
        event Action<IMiningDevice> Connected;

        void Load();
        void Unload();
        void Restart();
        void StartWork(IPoolWork work);
        void WorkRejected(IPoolWork work);
    }
}
