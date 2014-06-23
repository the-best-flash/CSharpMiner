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
    public interface IMiningDevice : IDisposable
    {
        int Id { get; set; }
        int Cores { get; }

        int HashRate { get; }

        int Accepted { get; set; }
        int Rejected { get; set; }
        int HardwareErrors { get; set; }

        int AcceptedWorkUnits { get; set; }
        int RejectedWorkUnits { get; set; }
        int DiscardedWorkUnits { get; set; }

        Timer WorkRequestTimer { get; }

        string Name { get; }

        event Action<IMiningDevice, IPoolWork, string> ValidNonce;
        event Action<IMiningDevice> WorkRequested;
        event Action<IMiningDevice, IPoolWork> InvalidNonce;

        void Load();
        void Unload();
        void StartWork(IPoolWork work);
        void WorkRejected(IPoolWork work);
    }
}
