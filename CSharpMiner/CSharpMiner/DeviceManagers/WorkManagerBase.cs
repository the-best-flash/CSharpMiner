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
using System.Threading.Tasks;

namespace CSharpMiner.DeviceManager
{
    [DataContract]
    public abstract class WorkManagerBase : IMiningDeviceManager
    {
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
        public IMiningDevice[] MiningDevices { get; set; }

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

        private int deviceId = 0;

        protected abstract void StartWork(IPoolWork work, IMiningDevice device, bool restartAll, bool requested);
        protected abstract void NoWork(IPoolWork oldWork, IMiningDevice device, bool requested);
        protected abstract void SetUpDevice(IMiningDevice d);

        bool boundPools = false;

        Object deviceListLock = new Object();
        Object hotplugListLock = new Object();
        private Object reconnectLock = new Object();

        public void NewWork(IPool pool, IPoolWork newWork, bool forceStart)
        {
            if (started && ActivePool != null)
            {
                if (newWork != null)
                {
                    // Pool asked us to toss out our old work or we don't have any work yet
                    if (forceStart || currentWork == null)
                    {
                        currentWork = newWork;
                        nextWork = newWork;

                        working = true;
                        StartWork(newWork, null, true, false);
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

        public virtual void SubmitWork(IMiningDevice device, IPoolWork work, string nonce)
        {
            if (started && this.ActivePool != null && currentWork != null && this.ActivePool.Connected)
            {
                Task.Factory.StartNew(() =>
                    {
                        this.ActivePool.SubmitWork(work, new Object[] {device, nonce});
                    });

                StartWorkOnDevice(work, device, false);
            }
            else if(this.ActivePool != null && !this.ActivePool.Connected && !this.ActivePool.Connecting)
            {
                //Attempt to connect to another pool
                this.AttemptPoolReconnect();
            }
        }

        public void StartWorkOnDevice(IPoolWork work, IMiningDevice device, bool requested)
        {
            if (nextWork.JobId != currentWork.JobId)
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

        public void RequestWork(IMiningDevice device)
        {
            LogHelper.DebugConsoleLogAsync(string.Format("Device {0} requested new work.", device.Name));
            StartWorkOnDevice(this.currentWork, device, true);
        }

        public virtual void Start()
        {
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
                    pool.Disconnected += this.PoolDisconnected;
                    pool.NewWorkRecieved += this.NewWork;
                    pool.WorkAccepted += this.OnWorkAccepted;
                    pool.WorkRejected += this.OnWorkRejected;
                }

                boundPools = true;
            }

            Task.Factory.StartNew(() =>
            {
                loadedDevices = new List<IMiningDevice>();
                hotplugLoaders = new List<IHotplugLoader>();

                foreach(IMiningDevice d in this.MiningDevices)
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
            });
        }

        private void LoadDevice(IMiningDevice d)
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
                    lock (deviceListLock)
                    {
                        d.Id = deviceId;
                        deviceId++;

                        loadedDevices.Add(d);
                    }

                    d.ValidNonce += this.SubmitWork;
                    d.WorkRequested += this.RequestWork;
                    d.InvalidNonce += this.InvalidNonce;

                    d.Load();

                    this.SetUpDevice(d);
                }
            }
        }

        public void AddNewDevice(IMiningDevice d)
        {
            Task.Factory.StartNew(() =>
                {
                    LoadDevice(d);
                });
        }

        protected virtual void OnWorkRejected(IPool pool, IPoolWork work, IMiningDevice device, string reason)
        {
            if (!reason.Contains("low difficulty"))
            {
                device.Rejected++;
                device.RejectedWorkUnits += work.Diff;
            }
            else
            {
                device.HardwareErrors++;
                device.DiscardedWorkUnits += work.Diff;
            }

            DisplayDeviceStats(device);
        }

        protected virtual void OnWorkAccepted(IPool pool, IPoolWork work, IMiningDevice device)
        {
            device.Accepted++;
            device.AcceptedWorkUnits += work.Diff;

            DisplayDeviceStats(device);
        }

        private void DisplayDeviceStats(IMiningDevice d)
        {
            LogHelper.ConsoleLogAsync(new Object[] {
                    new Object[] {string.Format("Device {0} ", d.Name), false},
                    new Object[] {" ( ", false },
                    new Object[] {d.Accepted, ConsoleColor.Green, false},
                    new Object[] {" : ", false},
                    new Object[] {d.Rejected, ConsoleColor.Red, false},
                    new Object[] {" : ", false},
                    new Object[] {d.HardwareErrors, ConsoleColor.Magenta, false},
                    new Object[] {" ) ", true}
                },
                LogVerbosity.Verbose);
        }

        public virtual void Stop()
        {
            if(boundPools)
            {
                foreach(IPool pool in this.Pools)
                {
                    pool.Disconnected -= this.PoolDisconnected;
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
        }

        public virtual void InvalidNonce(IMiningDevice device, IPoolWork work)
        {
            throw new NotImplementedException();
        }

        public void PoolDisconnected(IPool pool)
        {
            if (pool != null)
            {
                LogHelper.ConsoleLogErrorAsync(string.Format("Disconnected from pool {0}", pool.Url));
                LogHelper.LogErrorAsync(string.Format("Disconnected from pool {0}", pool.Url));
            }

            if (pool == this.ActivePool)
                AttemptPoolReconnect();
        }

        public void AttemptPoolReconnect()
        {
            lock (reconnectLock)
            {
                if (this.ActivePool == null || (!this.ActivePool.Connected && !this.ActivePool.Connected))
                {
                    if (this.ActivePool != null)
                    {
                        this.ActivePool.Stop();
                        this.ActivePool = null;
                    }

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

                        this.ActivePool = this.Pools[this.ActivePoolId];
                        LogHelper.ConsoleLog(string.Format("Attempting to connect to pool {0}", this.ActivePool.Url));
                        this.ActivePool.Start();
                    }
                }
            }
        }

        public void AddNewPool(IPool pool)
        {
            throw new NotImplementedException();
        }

        public void RemovePool(int poolIndex)
        {
            throw new NotImplementedException();
        }

        public void RemoveDevice(int deviceIndex)
        {
            throw new NotImplementedException();
        }
    }
}
