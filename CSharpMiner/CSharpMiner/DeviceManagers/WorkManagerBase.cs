using CSharpMiner;
using CSharpMiner.Helpers;
using CSharpMiner.Stratum;
using DeviceLoader;
using MiningDevice;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace DeviceManager
{
    [DataContract]
    public abstract class WorkManagerBase : IMiningDeviceManager
    {
        [DataMember(Name = "pools")]
        public Pool[] Pools { get; set; }

        [DataMember(Name = "devices")]
        public IMiningDevice[] MiningDevices { get; private set; }

        [IgnoreDataMember]
        public Pool ActivePool { get; private set; }

        [IgnoreDataMember]
        public int ActivePoolId { get; private set; }

        private bool working = false;
        private bool started = false;
        protected List<IMiningDevice> loadedDevices = null;
        protected List<IHotplugLoader> hotplugLoaders = null;

        protected PoolWork currentWork = null;
        protected PoolWork nextWork = null;

        private int deviceId = 0;

        protected abstract void StartWork(PoolWork work, int deviceId, bool restartAll, bool requested);
        protected abstract void NoWork(PoolWork oldWork, int deviceId, bool requested);
        protected abstract void SetUpDevice(IMiningDevice d);

        public void NewWork(object[] poolWorkData, int diff)
        {
            if (started && ActivePool != null)
            {
                PoolWork newWork = new PoolWork(poolWorkData, ActivePool.Extranonce1, "00000000", diff);

                // Pool asked us to toss out our old work
                if (poolWorkData[8].Equals(true))
                {
                    currentWork = newWork;
                    nextWork = newWork;

                    working = true;
                    StartWork(newWork, -1, true, false);
                }
                else // We can keep the old work
                {
                    if (!working)
                    {
                        currentWork = newWork;
                        nextWork = newWork;

                        working = true;
                        StartWork(newWork, -1, false, false);
                    }
                    else
                    {
                        nextWork = newWork;
                    }
                }
            }
        }

        public void SubmitWork(PoolWork work, string nonce, int deviceId)
        {
            if (started && this.ActivePool != null && currentWork != null && this.ActivePool.Connected)
            {
                Task.Factory.StartNew(() =>
                    {
                        this.ActivePool.SubmitWork(work.JobId, work.Extranonce2, work.Timestamp, nonce);
                    });

                StartWorkOnDevice(work, deviceId, false);
            }
            else if(this.ActivePool != null && !this.ActivePool.Connected && !this.ActivePool.Connecting)
            {
                //Attempt to connect to another pool
                this.AttemptPoolReconnect();
            }
        }

        public void StartWorkOnDevice(PoolWork work, int deviceId, bool requested)
        {
            if (nextWork.JobId != currentWork.JobId)
            {
                // Start working on the last thing the server sent us
                currentWork = nextWork;

                StartWork(nextWork, deviceId, false, requested);
            }
            else
            {
                working = false;
                NoWork(work, deviceId, requested);
            }
        }

        public void RequestWork(int deviceId)
        {
            LogHelper.ConsoleLogAsync(string.Format("Device {0} requested new work.", deviceId));
            StartWorkOnDevice(this.currentWork, deviceId, true);
        }

        public void Start()
        {
            // TODO: Make this throw an exception so that the system doesn't infinate loop
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
                    this.ActivePool.Start(this.NewWork, this.PoolDisconnected);
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
                    hotplugLoader.StartListening(this.AddNewDevice);
                    hotplugLoaders.Add(hotplugLoader);
                }
                else
                {
                    d.Id = deviceId;
                    deviceId++;

                    d.Load(this.SubmitWork, this.RequestWork);
                    loadedDevices.Add(d);

                    this.SetUpDevice(d);
                }
            }
        }

        private void AddNewDevice(IMiningDevice d)
        {
            Task.Factory.StartNew(() =>
                {
                    LoadDevice(d);
                });
        }

        public void Stop()
        {
            if (this.started)
            {
                this.started = false;

                if (this.hotplugLoaders != null)
                {
                    foreach (IHotplugLoader hotplugLoader in hotplugLoaders)
                    {
                        if (hotplugLoader != null)
                        {
                            hotplugLoader.StopListening();
                        }
                    }
                }

                if (this.loadedDevices != null)
                {
                    foreach (IMiningDevice d in this.loadedDevices)
                    {
                        if (d != null)
                        {
                            d.Unload();
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

        public void PoolDisconnected()
        {
            if (this.ActivePool != null)
            {
                LogHelper.ConsoleLogErrorAsync(string.Format("Disconnected from pool {0}", this.ActivePool.Url));
                LogHelper.LogErrorAsync(string.Format("Disconnected from pool {0}", this.ActivePool.Url));
            }

            AttemptPoolReconnect();
        }

        public void AttemptPoolReconnect()
        {
            // TODO: Handle when all pools are unable to be reached
            if (this.started && this.ActivePool != null && !this.ActivePool.Connecting)
            {
                this.ActivePool.Stop();
                this.ActivePool = null;

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
                this.ActivePool.Start(this.NewWork, this.PoolDisconnected);
            }
        }
    }
}
