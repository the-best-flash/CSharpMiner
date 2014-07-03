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

using CSharpMiner.Configuration;
using CSharpMiner.Helpers;
using CSharpMiner.Interfaces;
using CSharpMiner.ModuleLoading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace CSharpMiner
{
    public class Miner
    {
        public const string VersionString = "0.0.1";

        private JsonConfiguration config = null;
        private bool started = false;

        private Object syncLock = new Object();

        public event Action<IPool, IPoolWork, IMiningDevice> WorkAccepted;
        public event Action<IPool, IPoolWork, IMiningDevice, IShareResponse> WorkRejected;
        public event Action<IPool, IPoolWork, IMiningDevice> WorkDiscarded; // Usually due to hardware error
        public event Action<IPool, IPoolWork, bool> NewWorkRecieved;
        public event Action<IMiningDeviceManager, IMiningDevice> DeviceConnected;
        public event Action<IMiningDeviceManager, IMiningDevice> DeviceDisconnected;
        public event Action<IPool> PoolConnected;
        public event Action<IPool> PoolDisconnected;
        public event Action<Miner> Started;
        public event Action<Miner> Stopped;

        public IEnumerable<IMiningDeviceManager> MiningManagers
        {
            get
            {
                if(config != null)
                {
                    return config.Managers;
                }
                else
                {
                    return null;
                }
            }
        }

        public void Start(string configFilePath)
        {
            if (started)
                return;

            try
            {
                lock (syncLock)
                {
                    DataContractJsonSerializer jsonSerializer = new DataContractJsonSerializer(typeof(JsonConfiguration), ModuleLoader.KnownTypes);

                    try
                    {
                        using (var inputFile = File.OpenRead(configFilePath))
                        {
                            config = jsonSerializer.ReadObject(inputFile) as JsonConfiguration;
                        }
                    }
                    catch (FileNotFoundException e)
                    {
                        LogHelper.ConsoleLogError(string.Format("Configuration file not found. {0}", configFilePath));

                        throw new FileNotFoundException(string.Format("Configuration file not found. {0}", configFilePath), e);
                    }
                    catch (SerializationException e)
                    {
                        LogHelper.ConsoleLogError("There was an error loading the configuration file:");

                        if (e.InnerException != null)
                        {
                            LogHelper.ConsoleLog(e.InnerException.Message, LogVerbosity.Quiet);
                        }
                        else
                        {
                            LogHelper.ConsoleLog(e, LogVerbosity.Quiet);
                        }

                        throw new SerializationException("There was an error loading the configuration file:", e);
                    }

                    foreach (IMiningDeviceManager m in config.Managers)
                    {
                        m.WorkAccepted += OnWorkAccepted;
                        m.WorkRejected += OnWorkRejected;
                        m.WorkDiscarded += OnWorkDiscarded;
                        m.NewWorkRecieved += OnNewWorkRecieved;
                        m.DeviceConnected += OnDeviceConnected;
                        m.DeviceDisconnected += OnDeviceDisconnected;
                        m.PoolConnected += OnPoolConnected;
                        m.PoolDisconnected += PoolDisconnected;

                        m.Start();
                    }
                }
            }
            catch (Exception e)
            {
                LogHelper.LogError(e);

                LogHelper.ConsoleLogError(string.Format("There was an error. It has been logged to '{0}'", LogHelper.ErrorLogFilePath));
                LogHelper.ConsoleLog(e, LogVerbosity.Verbose);

                this.Stop();

                throw e;
            }

            started = true;

            if(this.Started != null)
            {
                this.Started(this);
            }
        }

        void OnPoolDisconnected(IPool obj)
        {
            if(this.PoolDisconnected != null)
                this.PoolDisconnected(obj);
        }

        void OnPoolConnected(IPool obj)
        {
            if (this.PoolConnected != null)
                this.PoolConnected(obj);
        }

        void OnDeviceDisconnected(IMiningDeviceManager arg1, IMiningDevice arg2)
        {
            if (this.DeviceDisconnected != null)
                this.DeviceDisconnected(arg1, arg2);
        }

        void OnDeviceConnected(IMiningDeviceManager arg1, IMiningDevice arg2)
        {
            if (this.DeviceConnected != null)
                this.DeviceConnected(arg1, arg2);
        }

        void OnNewWorkRecieved(IPool arg1, IPoolWork arg2, bool arg3)
        {
            if (this.NewWorkRecieved != null)
                this.NewWorkRecieved(arg1, arg2, arg3);
        }

        void OnWorkDiscarded(IPool arg1, IPoolWork arg2, IMiningDevice arg3)
        {
            if (this.WorkDiscarded != null)
                this.WorkDiscarded(arg1, arg2, arg3);
        }

        void OnWorkRejected(IPool arg1, IPoolWork arg2, IMiningDevice arg3, IShareResponse arg4)
        {
            if (this.WorkRejected != null)
                this.WorkRejected(arg1, arg2, arg3, arg4);
        }

        void OnWorkAccepted(IPool arg1, IPoolWork arg2, IMiningDevice arg3)
        {
            if (this.WorkAccepted != null)
                this.WorkAccepted(arg1, arg2, arg3);
        }

        public void Stop()
        {
            lock (syncLock) // We don't want to stop while we are starting
            {
                if (config != null)
                {
                    foreach (IMiningDeviceManager m in config.Managers)
                    {
                        m.WorkAccepted -= OnWorkAccepted;
                        m.WorkRejected -= OnWorkRejected;
                        m.WorkDiscarded -= OnWorkDiscarded;
                        m.NewWorkRecieved -= OnNewWorkRecieved;
                        m.DeviceConnected -= OnDeviceConnected;
                        m.DeviceDisconnected -= OnDeviceDisconnected;
                        m.PoolConnected -= OnPoolConnected;
                        m.PoolDisconnected -= PoolDisconnected;

                        m.Stop();
                    }

                    config = null;
                    started = false;
                }

                if(this.Stopped != null)
                {
                    this.Stopped(this);
                }
            }
        }
    }
}
