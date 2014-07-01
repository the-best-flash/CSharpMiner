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
using System.Threading;
using System.Threading.Tasks;

namespace CSharpMiner.DeviceManager
{
    [DataContract]
    public abstract class WorkManagerBase : IMiningDeviceManager
    {
        private const int longWaitTime = 600000;
        private const int shortWaitTime = 30000;
        private const int longWaitDisplayTime = 10;
        private const int shortWaitDisplayTime = 30;
        private const int longWaitThreshold = 10;
        private const int defaultWorkUpdateInterval = 60;

        public abstract IPool[] Pools { get; }

        [DataMember(Name = "devices")]
        [MiningSetting(Description="A collection of MiningDevice or DeviceLoader JSON objects.", Optional=false, 
           ExampleValue=@"[
    {
		    '__type' : 'ZeusDeviceLoader:#DeviceLoader',
		    'ports' : ['/dev/ttyUSB0', '/dev/ttyUSB1', '/dev/ttyUSB2', '/dev/ttyUSB3', '/dev/ttyUSB4', '/dev/ttyUSB5', 'COM1' ],
		    'cores' : 6,
		    'clock' : 328
    },
    {
		    '__type' : 'ZeusDevice:#MiningDevice',
		    'port' : '/dev/ttyUSB9',
		    'cores' : 6,
		    'clock' : 382
    }
]")]
        public IMiningDeviceObject[] MiningDevices { get; set; }

        private int _workUpdateTimerInterval;

        [DataMember(Name = "workUpdate")]
        [MiningSetting(Description="Interval in seconds before forcing devices to start a new work with a new ntime. Can often be left alone. [0 = disabled. Default 60]", ExampleValue="60", Optional=true)]
        public int WorkUpdateTimerInterval 
        { 
            get
            {
                if (_workUpdateTimerInterval > 0)
                {
                    return _workUpdateTimerInterval;
                }
                else
                {
                    return defaultWorkUpdateInterval;
                }
            }

            set
            {
                if (value > 0)
                {
                    _workUpdateTimerInterval = value;

                    if (_workUpdateTimer != null)
                        _workUpdateTimer.Interval = value * 1000;
                }
                else
                {
                    _workUpdateTimerInterval = defaultWorkUpdateInterval;

                    if (_workUpdateTimer != null)
                        _workUpdateTimer.Interval = defaultWorkUpdateInterval * 1000;
                }
            }
        }

        [IgnoreDataMember]
        public IPool ActivePool { get; private set; }

        [IgnoreDataMember]
        public int ActivePoolId { get; private set; }

        [IgnoreDataMember]
        public IEnumerable<IMiningDevice> LoadedDevices
        {
            get { return loadedDevices; }
        }

        private bool working = false;
        private bool started = false;
        protected List<IMiningDevice> loadedDevices = null;
        protected List<IHotplugLoader> hotplugLoaders = null;

        protected IPoolWork currentWork = null;
        protected IPoolWork nextWork = null;

        private long deviceId = 0;

        bool boundPools = false;

        Object deviceListLock = new Object();
        Object hotplugListLock = new Object();
        private Object reconnectLock = new Object();

        private int reconnectionAttempts = 0;
        private int waitAttempts = 0;
        private bool longWait = false;
        private bool waitingToReconnect = false;
        private IPool poolReconnectingTo = null;

        private System.Timers.Timer _workUpdateTimer;

        protected abstract void StartWork(IPoolWork work, IMiningDevice device, bool restartAll, bool requested);
        protected abstract void NoWork(IPoolWork oldWork, IMiningDevice device, bool requested);
        protected abstract void SetUpDevice(IMiningDevice d);
        protected abstract void OnWorkUpdateTimerExpired();

        public event Action<IPool, IPoolWork, IMiningDevice> WorkAccepted;
        public event Action<IPool, IPoolWork, IMiningDevice, IShareResponse> WorkRejected;
        public event Action<IPool, IPoolWork, IMiningDevice> WorkDiscarded;
        public event Action<IPool, IPoolWork, bool> NewWorkRecieved;
        public event Action<IMiningDeviceManager, IMiningDevice> DeviceConnected;
        public event Action<IMiningDeviceManager, IMiningDevice> DeviceDisconnected;
        public event Action<IPool> PoolConnected;
        public event Action<IPool> PoolDisconnected;
        public event Action<IMiningDeviceManager> Started;
        public event Action<IMiningDeviceManager> Stopped;

        [OnDeserialized]
        private void Deserialized(StreamingContext context)
        {
            this.OnDeserialized();
        }

        [OnDeserializing]
        private void Deserializing(StreamingContext context)
        {
            this.OnDeserializing();
        }

        public WorkManagerBase()
        {
            SetDefaultValues();
        }

        protected virtual void OnDeserialized()
        {

        }

        protected virtual void OnDeserializing()
        {
            SetDefaultValues();
        }

        private void WorkUpdateTimerExipred(object sender, System.Timers.ElapsedEventArgs e)
        {
            if(currentWork != nextWork)
            {
                currentWork = nextWork;
            }

            this.OnWorkUpdateTimerExpired();
        }

        protected void RestartWorkUpdateTimer()
        {
            if (this.WorkUpdateTimerInterval > 0 && _workUpdateTimer != null)
            {
                _workUpdateTimer.Stop();
                _workUpdateTimer.Start();
            }
        }

        protected void StopWorkUpdateTimer()
        {
            if (_workUpdateTimer != null)
            {
                _workUpdateTimer.Stop();
            }
        }

        private void SetDefaultValues()
        {
            _workUpdateTimer = new System.Timers.Timer(defaultWorkUpdateInterval * 1000);
            _workUpdateTimer.Stop();
            _workUpdateTimer.Elapsed += this.WorkUpdateTimerExipred;

            WorkUpdateTimerInterval = defaultWorkUpdateInterval;
        }

        public void NewWork(IPool pool, IPoolWork newWork, bool forceStart)
        {
            if (this.NewWorkRecieved != null)
            {
                Task.Factory.StartNew(() =>
                    {
                        this.NewWorkRecieved(pool, newWork, forceStart);
                    });
            }

            longWait = false;
            reconnectionAttempts = 0; // We know that we've connected to something now
            poolReconnectingTo = null;

            if (started && ActivePool != null)
            {
                if (newWork != null)
                {
                    RestartWorkUpdateTimer();

                    OnNewWork(pool, newWork, forceStart);

                    // Pool asked us to toss out our old work or we don't have any work yet
                    if (forceStart || currentWork == null)
                    {
                        StartNewWork(newWork);
                    }
                    else // We can keep the old work
                    {
                        if (!working)
                        {
                            currentWork = newWork;
                            nextWork = newWork;

                            working = true;
                            StartWork(newWork, null, false, false);
                        }
                        else
                        {
                            nextWork = newWork;
                        }
                    }
                }
            }
        }

        protected virtual void OnNewWork(IPool pool, IPoolWork newWork, bool forceStart)
        {
            // Do nothing
        }

        public virtual void SubmitWork(IMiningDevice device, IPoolWork work, string nonce)
        {
            if (started && this.ActivePool != null && currentWork != null && this.ActivePool.IsConnected)
            {
                this.ActivePool.SubmitWork(work, device, nonce);

                StartWorkOnDevice(work, device, false);
            }
            else if(this.ActivePool != null && !this.ActivePool.IsConnected && !this.ActivePool.IsConnecting)
            {
                //Attempt to connect to another pool
                this.AttemptPoolReconnect();
            }
        }

        public void StartWorkOnDevice(IPoolWork work, IMiningDevice device, bool requested)
        {
            if (nextWork != null && currentWork != null)
            {
                if (nextWork.JobId != currentWork.JobId || nextWork.Diff != currentWork.Diff)
                {
                    // Start working on the last thing the server sent us
                    currentWork = nextWork;

                    StartWork(nextWork, device, false, requested);
                }
                else
                {
                    working = false;
                    NoWork(work, device, requested);
                }
            }
        }

        public void RequestWork(IMiningDevice device)
        {
            LogHelper.DebugConsoleLogAsync(string.Format("Device {0} requested new work.", device.Name));
            StartWorkOnDevice(this.currentWork, device, true);
        }

        protected virtual void OnPoolConnected(IPool pool)
        {
            if(this.PoolConnected != null)
            {
                this.PoolConnected(pool);
            }
        }

        public virtual void Start()
        {
            _workUpdateTimer = new System.Timers.Timer(this.WorkUpdateTimerInterval * 1000);
            _workUpdateTimer.Stop();
            _workUpdateTimer.Elapsed += this.WorkUpdateTimerExipred;

            reconnectionAttempts = 0;
            waitAttempts = 0;
            longWait = false;
            waitingToReconnect = false;
            poolReconnectingTo = null;

            if (deviceListLock == null)
            {
                deviceListLock = new Object();
            }

            if(hotplugListLock == null)
            {
                hotplugListLock = new Object();
            }

            if(reconnectLock == null)
            {
                reconnectLock = new Object();
            }

            if(!boundPools)
            {
                foreach(IPool pool in this.Pools)
                {
                    pool.Connected += OnPoolConnected;
                    pool.Disconnected += this.OnPoolDisconnected;
                    pool.NewWorkRecieved += this.NewWork;
                    pool.WorkAccepted += this.OnWorkAccepted;
                    pool.WorkRejected += this.OnWorkRejected;
                }

                boundPools = true;
            }

            loadedDevices = new List<IMiningDevice>();
            hotplugLoaders = new List<IHotplugLoader>();

            foreach (IMiningDeviceObject d in this.MiningDevices)
            {
                LoadDevice(d);
            }

            if(Pools.Length > 0)
            {
                this.ActivePool = Pools[0];
                this.ActivePoolId = 0;
                this.ActivePool.Start();
            }

            started = true;

            if(this.Started != null)
            {
                this.Started(this);
            }
        }

        private void LoadDevice(IMiningDeviceObject d)
        {
            IDeviceLoader loader = d as IDeviceLoader;

            if (loader != null)
            {
                foreach (IMiningDevice device in loader.LoadDevices())
                {
                    LoadDevice(device);
                }
            }
            else
            {
                IHotplugLoader hotplugLoader = d as IHotplugLoader;

                if (hotplugLoader != null)
                {
                    hotplugLoader.DeviceFound += this.AddNewDevice;
                    hotplugLoader.StartListening();

                    lock (hotplugListLock)
                    {
                        hotplugLoaders.Add(hotplugLoader);
                    }
                }
                else
                {
                    IMiningDevice device = d as IMiningDevice;

                    if (device != null)
                    {
                        lock (deviceListLock)
                        {
                            device.Id = deviceId;
                            deviceId++;

                            loadedDevices.Add(device);
                        }

                        device.ValidNonce += this.SubmitWork;
                        device.WorkRequested += this.RequestWork;
                        device.InvalidNonce += this.InvalidNonce;

                        device.Connected += OnDeviceConnected;
                        device.Disconnected += OnDeviceDisconnected;

                        device.Load();

                        this.SetUpDevice(device);
                    }
                }
            }
        }

        protected virtual void OnDeviceConnected(IMiningDevice device)
        {
            if(this.DeviceConnected != null)
            {
                this.DeviceConnected(this, device);
            }
        }

        protected virtual void OnDeviceDisconnected(IMiningDevice device)
        {
            if(this.DeviceDisconnected != null)
            {
                this.DeviceDisconnected(this, device);
            }
        }

        public void AddNewDevice(IMiningDevice d)
        {
            Task.Factory.StartNew(() =>
                {
                    LoadDevice(d);
                });
        }

        protected void StartNewWork(IPoolWork work)
        {
            currentWork = work;
            nextWork = work;
            working = true;
            StartWork(work, null, true, false);
        }

        protected virtual void OnWorkRejected(IPool pool, IPoolWork work, IMiningDevice device, IShareResponse response)
        {
            if (!response.IsLowDifficlutyShare && !response.RejectReason.Contains("low difficulty") && !response.RejectReason.Contains("above target"))
            {
                device.Rejected++;
                device.RejectedWorkUnits += work.Diff;

                if(this.WorkRejected != null)
                {
                    this.WorkRejected(pool, work, device, response);
                }
            }
            else
            {
                // Fix for bug where some pools will change the difficluty in the middle of a job and expect shares at the new difficluty
                if (pool.Diff > work.Diff)
                {
                    LogHelper.DebugLogErrorAsync(string.Format("Submitted share with low difficluty while pool diff was different than work diff. P: {0} W: {0}", pool.Diff, work.Diff));

                    LogHelper.DebugConsoleLog("Restarting work on all to attempt to synchronize difficluty.", ConsoleColor.Red, LogVerbosity.Quiet);

                    IPoolWork newWork = work.Clone() as IPoolWork;
                    newWork.Diff = pool.Diff;
                    StartNewWork(newWork);
                }

                device.HardwareErrors++;
                device.DiscardedWorkUnits += work.Diff;

                if(this.ActivePool != null)
                {
                    this.ActivePool.Rejected--;
                    this.ActivePool.RejectedWorkUnits -= work.Diff;

                    this.ActivePool.HardwareErrors++;
                    this.ActivePool.DiscardedWorkUnits += work.Diff;
                }
            }

            DisplayDeviceStats(device);

            if(this.WorkDiscarded != null)
            {
                this.WorkDiscarded(pool, work, device);
            }
        }

        protected virtual void OnWorkAccepted(IPool pool, IPoolWork work, IMiningDevice device)
        {
            device.Accepted++;
            device.AcceptedWorkUnits += work.Diff;

            DisplayDeviceStats(device);

            if(this.WorkAccepted != null)
            {
                this.WorkAccepted(pool, work, device);
            }
        }

        private void DisplayDeviceStats(IMiningDevice d)
        {
            LogHelper.ConsoleLogAsync(new Object[] {
                    new Object[] {string.Format("Device {0} ", d.Name), false},
                    new Object[] {" ( ", false},
                    new Object[] {d.Accepted, ConsoleColor.Green, false},
                    new Object[] {" : ", false},
                    new Object[] {d.Rejected, ConsoleColor.Red, false},
                    new Object[] {" : ", false},
                    new Object[] {d.HardwareErrors, ConsoleColor.Magenta, false},
                    new Object[] {" ) ", false},
                    new Object[] {" ( ", false},
                    new Object[] {MegaHashDisplayString(d.AcceptedHashRate), ConsoleColor.Green, false},
                    new Object[] {" : ", false},
                    new Object[] {MegaHashDisplayString(d.RejectedHashRate), ConsoleColor.Red, false},
                    new Object[] {" : ", false},
                    new Object[] {MegaHashDisplayString(d.DiscardedHashRate), ConsoleColor.Magenta, false},
                    new Object[] {" )", true}
                },
                LogVerbosity.Verbose);
        }

        private string MegaHashDisplayString(double hashesPerSec)
        {
            double mHash = hashesPerSec / 1000000;

            return string.Format("{0:N2}Mh", mHash);
        }

        public virtual void Stop()
        {
            if (_workUpdateTimer != null)
            {
                _workUpdateTimer.Stop();
                _workUpdateTimer.Elapsed -= this.WorkUpdateTimerExipred;
                _workUpdateTimer = null;
            }

            if(boundPools)
            {
                foreach(IPool pool in this.Pools)
                {
                    pool.Connected -= this.OnPoolConnected;
                    pool.Disconnected -= this.OnPoolDisconnected;
                    pool.NewWorkRecieved -= this.NewWork;
                    pool.WorkRejected -= this.OnWorkRejected;
                    pool.WorkAccepted -= this.OnWorkAccepted;
                }

                boundPools = false;
            }

            if (this.started)
            {
                this.started = false;

                if (this.hotplugLoaders != null)
                {
                    lock (hotplugListLock)
                    {
                        foreach (IHotplugLoader hotplugLoader in hotplugLoaders)
                        {
                            if (hotplugLoader != null)
                            {
                                hotplugLoader.StopListening();
                                hotplugLoader.DeviceFound -= this.AddNewDevice;
                            }
                        }
                    }
                }

                if (this.loadedDevices != null)
                {
                    lock (deviceListLock)
                    {
                        foreach (IMiningDevice d in this.loadedDevices)
                        {
                            if (d != null)
                            {
                                d.WorkRequested -= this.RequestWork;
                                d.ValidNonce -= this.SubmitWork;
                                d.InvalidNonce -= this.InvalidNonce;

                                d.Connected -= this.OnDeviceConnected;
                                d.Disconnected -= this.OnDeviceDisconnected;

                                d.Unload();
                            }
                        }
                    }
                }

                if (this.ActivePool != null)
                {
                    this.ActivePool.Stop();
                    this.ActivePool = null;
                }
            }

            if(this.Stopped != null)
            {
                this.Stopped(this);
            }
        }

        public virtual void InvalidNonce(IMiningDevice device, IPoolWork work)
        {
            if(this.ActivePool != null)
            {
                this.ActivePool.HardwareErrors++;
                this.ActivePool.DiscardedWorkUnits += work.Diff;
            }
        }

        public virtual void OnPoolDisconnected(IPool pool)
        {
            if(this.PoolDisconnected != null)
            {
                Task.Factory.StartNew(() =>
                    {
                        this.PoolDisconnected(pool);
                    });
            }

            if (pool != null)
            {
                LogHelper.ConsoleLogErrorAsync(string.Format("Disconnected from pool {0}", pool.Url));
                LogHelper.LogErrorAsync(string.Format("Disconnected from pool {0}", pool.Url));
            }

            if (poolReconnectingTo == null || pool == poolReconnectingTo)
            {
                poolReconnectingTo = null;

                if (pool == this.ActivePool)
                    AttemptPoolReconnect();
            }
        }

        public void AttemptPoolReconnect()
        {
            if (!waitingToReconnect && poolReconnectingTo == null)
            {
                Task.Factory.StartNew(() =>
                    {
                        lock (reconnectLock)
                        {
                            if (poolReconnectingTo == null && (this.ActivePool == null || (!this.ActivePool.IsConnected && !this.ActivePool.IsConnected)))
                            {
                                // TODO: Handle when all pools are unable to be reached
                                if (this.started)
                                {
                                    if (this.ActivePoolId + 1 < this.Pools.Length)
                                    {
                                        this.ActivePoolId++;
                                    }
                                    else
                                    {
                                        this.ActivePoolId = 0;
                                    }

                                    poolReconnectingTo = this.Pools[this.ActivePoolId];

                                    if (reconnectionAttempts == this.Pools.Length)
                                    {
                                        if (waitAttempts > longWaitThreshold)
                                            longWait = true;

                                        LogHelper.ConsoleLogErrorAsync(string.Format("Could not connect to any pools. Waiting {0} {1} before trying again.", (longWait ? longWaitDisplayTime : shortWaitDisplayTime), (longWait ? "minutes" : "sec")));

                                        waitAttempts++;

                                        waitingToReconnect = true;

                                        Thread.Sleep((longWait ? longWaitTime : shortWaitTime));

                                        reconnectionAttempts = 0;
                                    }
                                    else
                                    {
                                        reconnectionAttempts++;
                                    }

                                    if (this.ActivePool != null)
                                    {
                                        this.ActivePool.Stop();
                                        this.ActivePool = null;
                                    }

                                    this.ActivePool = poolReconnectingTo;
                                    LogHelper.ConsoleLogAsync(string.Format("Attempting to connect to pool {0}", this.ActivePool.Url));
                                    this.ActivePool.Start();

                                    waitingToReconnect = false;
                                }
                            }
                        }
                    });
            }
        }

        public void AddNewPool(IPool pool)
        {
            throw new NotImplementedException();
        }

        public void RemovePool(IPool pool)
        {
            throw new NotImplementedException();
        }

        public void RemoveDevice(IMiningDevice device)
        {
            throw new NotImplementedException();
        }
    }
}
