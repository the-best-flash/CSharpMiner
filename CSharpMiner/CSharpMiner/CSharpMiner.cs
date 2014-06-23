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
using CSharpMiner.ModuleLoading;
using DeviceManager;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace CSharpMiner
{
    public class CSharpMiner
    {
        public const string VersionString = "0.0.1";

        private JsonConfiguration config = null;
        private bool started = false;

        private Object syncLock = new Object();

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
                    DataContractJsonSerializer jsonSerializer = new DataContractJsonSerializer(typeof(JsonConfiguration), ModuleLoader.GetKnownTypes());

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
        }

        public void Stop()
        {
            lock (syncLock) // We don't want to stop while we are starting
            {
                if (config != null)
                {
                    foreach (IMiningDeviceManager m in config.Managers)
                    {
                        m.Stop();
                    }

                    config = null;
                    started = false;
                }
            }
        }
    }
}
