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
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace CSharpMiner.MiningDevice
{
    [DataContract]
    public abstract class MiningDeviceBase : IMiningDevice
    {
        private const int defaultWatchdogTimeout = 60;
        private const int defaultWorkRestartTimeout = 300000;

        [DataMember(Name = "timeout")]
        [MiningSetting(ExampleValue = "60", Optional = true, Description = "Number of seconds to wait without response before restarting the device.")]
        public int WatchdogTimeout { get; set; }

        [IgnoreDataMember]
        public virtual long Id { get; set; }

        [IgnoreDataMember]
        public virtual long HashRate { get; protected set; }

        [IgnoreDataMember]
        public virtual long HardwareErrors { get; set; }

        [IgnoreDataMember]
        public virtual long Accepted { get; set; }

        [IgnoreDataMember]
        public virtual long Rejected { get; set; }

        [IgnoreDataMember]
        public virtual long AcceptedWorkUnits { get; set; }

        [IgnoreDataMember]
        public virtual long RejectedWorkUnits { get; set; }

        [IgnoreDataMember]
        public virtual long DiscardedWorkUnits { get; set; }

        [IgnoreDataMember]
        public abstract string Name { get; }

        [IgnoreDataMember]
        public virtual double AcceptedHashRate
        {
            get
            {
                return ComputeHashRate(AcceptedWorkUnits);
            }
        }

        [IgnoreDataMember]
        public virtual double RejectedHashRate
        {
            get
            {
                return ComputeHashRate(RejectedWorkUnits);
            }
        }

        [IgnoreDataMember]
        public virtual double DiscardedHashRate
        {
            get
            {
                return ComputeHashRate(DiscardedWorkUnits);
            }
        }

        [IgnoreDataMember]
        public Timer WorkRequestTimer { get; private set; }

        public event Action<IMiningDevice, IPoolWork, string> ValidNonce;
        public event Action<IMiningDevice> WorkRequested;
        public event Action<IMiningDevice, IPoolWork> InvalidNonce;
        public event Action<IMiningDevice> Disconnected;

        private DateTime start;
        private Timer watchdogTimer = null;

        public abstract void WorkRejected(IPoolWork work);
        public abstract void StartWork(IPoolWork work);

        [OnDeserializing]
        protected virtual void SetValuesOnDeserializing(StreamingContext context)
        {
            WatchdogTimeout = defaultWatchdogTimeout;
        }

        public virtual void Load()
        {
            start = DateTime.Now;

            Accepted = 0;
            Rejected = 0;
            HardwareErrors = 0;

            AcceptedWorkUnits = 0;
            RejectedWorkUnits = 0;
            DiscardedWorkUnits = 0;

            if (WatchdogTimeout <= 0)
            {
                WatchdogTimeout = defaultWatchdogTimeout; // Default if not set
            }

            DestoryWorkRequestTimer();

            this.WorkRequestTimer = new Timer(defaultWorkRestartTimeout);
            this.WorkRequestTimer.Elapsed += this.WorkRequestTimerExpired;
            this.WorkRequestTimer.AutoReset = false;

            DestoryWatchdogTimer();

            watchdogTimer = new System.Timers.Timer(WatchdogTimeout * 1000);
            watchdogTimer.Elapsed += this.WatchdogExpired;
            watchdogTimer.AutoReset = true;
        }

        public virtual void Unload()
        {
            DestoryWorkRequestTimer();
            DestoryWatchdogTimer();
        }

        public void Restart()
        {
            this.Unload();
            this.Load();
        }

        private void DestoryWatchdogTimer()
        {
            if (this.watchdogTimer != null)
            {
                this.watchdogTimer.Stop();
                this.watchdogTimer.Elapsed -= this.WatchdogExpired;
                this.watchdogTimer = null;
            }
        }

        private void DestoryWorkRequestTimer()
        {
            if (this.WorkRequestTimer != null)
            {
                this.WorkRequestTimer.Stop();
                this.WorkRequestTimer.Elapsed -= this.WorkRequestTimerExpired;
                this.WorkRequestTimer = null;
            }
        }

        protected void SubmitWork(IPoolWork work, string nonce)
        {
            this.OnValidNonce(work, nonce);
        }

        protected void RequestWork()
        {
            if (this.WorkRequested != null)
            {
                Task.Factory.StartNew(() =>
                {
                    this.WorkRequested(this);
                });
            }
        }

        protected void OnValidNonce(IPoolWork work, string nonce)
        {
            this.RestartWatchdogTimer();

            if (this.ValidNonce != null)
            {
                Task.Factory.StartNew(() =>
                    {
                        this.ValidNonce(this, work, nonce);
                    });
            }
        }

        protected void OnInvalidNonce(IPoolWork work)
        {
            if (this.InvalidNonce != null)
            {
                Task.Factory.StartNew(() =>
                    {
                        this.InvalidNonce(this, work);
                    });
            }
        }

        protected void OnDisconnected()
        {
            if (this.Disconnected != null)
            {
                Task.Factory.StartNew(() =>
                    {
                        this.Disconnected(this);
                    });
            }
        }

        private void WatchdogExpired(object sender, System.Timers.ElapsedEventArgs e)
        {
            LogHelper.ConsoleLogErrorAsync(string.Format("Device {0} hasn't responded for {1} sec. Restarting.", this.Name, (double)WatchdogTimeout));
            RequestWork();
        }

        private void WorkRequestTimerExpired(object sender, System.Timers.ElapsedEventArgs e)
        {
            LogHelper.DebugConsoleLog(string.Format("Work request timer timed out with interval = {0} for device {1}", this.WorkRequestTimer.Interval, this.Name));
            RequestWork();
        }

        protected void RestartWatchdogTimer()
        {
            if (watchdogTimer != null)
            {
                watchdogTimer.Stop();
                watchdogTimer.Start();
            }
        }

        protected void StopWatchdogTimer()
        {
            if(watchdogTimer != null)
            {
                watchdogTimer.Stop();
            }
        }

        protected void StartWatchdogTimer()
        {
            if(watchdogTimer != null)
            {
                watchdogTimer.Start();
            }
        }

        protected void RestartWorkRequestTimer()
        {
            if (WorkRequestTimer != null)
            {
                WorkRequestTimer.Stop();
                WorkRequestTimer.Start();
            }
        }

        private double ComputeHashRate(long workUnits)
        {
            return 65535.0 * workUnits / DateTime.Now.Subtract(start).TotalSeconds; // Expected hashes per work unit * work units / sec = hashes per sec
        }

        public virtual void Dispose()
        {
            this.Unload();
        }
    }
}
