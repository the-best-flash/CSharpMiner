﻿/*  Copyright (C) 2014 Colton Manville
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

        long Accepted { get; set; }
        long Rejected { get; set; }
        long HardwareErrors { get; set; }

        double AcceptedHashRate { get; }
        double RejectedHashRate { get; }
        double DiscardedHashRate { get; }

        long AcceptedWorkUnits { get; }
        long RejectedWorkUnits { get; set; }
        long DiscardedWorkUnits { get; set; }

        long NewBlocks { get; }
        int Diff { get; }

        bool IsAlive { get; }
        bool IsConnected { get; }
        bool IsConnecting { get; }

        event Action<IPool, IPoolWork, bool> NewWorkRecieved;
        event Action<IPool> Disconnected;
        event Action<IPool> Connected;

        event Action<IPool, IPoolWork, IMiningDevice> WorkAccepted;
        event Action<IPool, IPoolWork, IMiningDevice, IShareResponse> WorkRejected;

        void Start();
        void Stop();
        void SubmitWork(IPoolWork work, IMiningDevice device, string nonce);
    }
}
