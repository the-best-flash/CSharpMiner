using CSharpMiner.Configuration;
using CSharpMiner.Helpers;
using CSharpMiner.Stratum;
using DeviceManager;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace CSharpMiner
{
    class Program
    {
        public const string VersionString = "0.0.1";

        private static IEnumerable<Type> GetKnownTypes()
        {
            Assembly thisAssembly = Assembly.GetAssembly(typeof(Program));

            // TODO: Get types from module assemblies

            return thisAssembly.GetTypes().Where(t => Attribute.IsDefined(t, typeof(DataContractAttribute)));
        }

        static void Main(string[] args)
        {
            if(args.Length < 1 || args.Length > 3)
            {
                WriteUsage();
                return;
            }

            bool loop = true;

            if (args.Length == 2)
            {
                loop = (args[1].ToLower().Trim() != "false");

                if (args[1].ToLower().Trim() != "false" && args[1].ToLower().Trim() != "true")
                {
                    LogHelper.ErrorLogFilePath = args[1];
                }
            }
            else
            {
                loop = (args[2].ToLower().Trim() != "false");

                LogHelper.ErrorLogFilePath = args[1];
            }

            do
            {
                JsonConfiguration config = null;

                try
                {
                    DataContractJsonSerializer jsonSerializer = new DataContractJsonSerializer(typeof(JsonConfiguration), GetKnownTypes());

                    try
                    {
                        using (var inputFile = File.OpenRead(args[0]))
                        {
                            config = jsonSerializer.ReadObject(inputFile) as JsonConfiguration;
                        }
                    }
                    catch (FileNotFoundException e)
                    {
                        LogHelper.ConsoleLogError(string.Format("Configuration file not found. {0}", args[0]));

                        loop = false; // We cannot recover

                        throw new FileNotFoundException(string.Format("Configuration file not found. {0}", args[0]), e);
                    }
                    catch (SerializationException e)
                    {
                        LogHelper.ConsoleLogError("There was an error loading the configuration file:");

                        if (e.InnerException != null)
                        {
                            LogHelper.ConsoleLog(e.InnerException.Message);
                        }
                        else
                        {
                            LogHelper.ConsoleLog(e);
                        }

                        loop = false; // We cannot recover

                        throw new SerializationException("There was an error loading the configuration file:", e);
                    }

                    foreach (IMiningDeviceManager m in config.Managers)
                    {
                        m.Start();
                    }

                    while (Console.ReadKey().Key != ConsoleKey.Q)
                    {
                        // Wait for user to press Q to quit
                    }

                    loop = false;

                    using (StreamWriter log = new StreamWriter(File.Open("log.log", FileMode.Append)))
                    {
                        log.WriteLine("Exiting normally at {0}", DateTime.Now);
                    }
                }
                catch (Exception e)
                {
                    LogHelper.LogError(e);

                    LogHelper.ConsoleLogError("There was an error. It has been logged to 'log.err'. More details below:");
                    LogHelper.ConsoleLog(e);
                }
                finally
                {
                    if (config != null)
                    {
                        foreach (IMiningDeviceManager m in config.Managers)
                        {
                            m.Stop();
                        }
                    }
                }
            }while(loop);
        }

        static void WriteUsage()
        {
            LogHelper.ConsoleLog("CSharpMiner.exe <Configuration File Path> [LogFilePath] [true|false]");
        }
    }
}
