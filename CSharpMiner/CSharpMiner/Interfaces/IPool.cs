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

namespace CSharpMiner.Interfaces
{
    public interface IPool : IDisposable
    {
        string Url { get; set; }
        string Username { get; set; }
        string Password { get; set; }

        int Accepted { get; set; }
        int Rejected { get; set; }
        int HardwareErrors { get; set; }

        int AcceptedWorkUnits { get; }
        int RejectedWorkUnits { get; }
        int DiscardedWorkUnits { get; set; }

        int NewBlocks { get; }
        int Diff { get; }

        bool Alive { get; }
        bool Connected { get; }
        bool Connecting { get; }

        event Action<IPool, IPoolWork, bool> NewWorkRecieved;
        event Action<IPool> Disconnected;

        event Action<IPool, IPoolWork, IMiningDevice> WorkAccepted;
        event Action<IPool, IPoolWork, IMiningDevice, string> WorkRejected;

        void Start();
        void Stop();
        void SubmitWork(IPoolWork work, Object workData);
    }
}
