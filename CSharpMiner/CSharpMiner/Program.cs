using CSharpMiner.Configuration;
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

        public static void DebugConsoleLog(Object thing)
        {
            Console.WriteLine(thing);
        }

        private static IEnumerable<Type> GetKnownTypes()
        {
            Assembly thisAssembly = Assembly.GetAssembly(typeof(Program));

            // TODO: Get types from module assemblies

            return thisAssembly.GetTypes().Where(t => Attribute.IsDefined(t, typeof(DataContractAttribute)));
        }

        static void Main(string[] args)
        {
            if(args.Length < 1 || args.Length > 2)
            {
                WriteUsage();
                return;
            }

            bool loop = (args.Length != 2 || args[2].ToLower().Trim() != "false");

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
                        Console.WriteLine("Configuration file not found. {0}", args[0]);
                        loop = false; // We cannot recover
                        throw new FileNotFoundException(string.Format("Configuration file not found. {0}", args[0]), e);
                    }
                    catch (SerializationException e)
                    {
                        Console.WriteLine("There was an error loading the configuration file:");
                        if (e.InnerException != null)
                        {
                            Console.WriteLine(e.InnerException.Message);
                        }
                        else
                        {
                            Console.WriteLine(e);
                        }
                        loop = false; // We cannot recover
                        throw new SerializationException("There was an error loading the configuration file:", e);
                    }

                    foreach (IMiningDeviceManager m in config.Managers)
                    {
                        m.Start();
                    }

                    while (Console.ReadKey().Key != ConsoleKey.D)
                    {
                        // Wait for user to press D to disconnect from pool
                        loop = false;
                    }
                }
                catch (Exception e)
                {
                    using(StreamWriter errLog = new StreamWriter(File.Open("log.err", FileMode.Append)))
                    {
                        errLog.WriteLine("Exception caught at {0}.", DateTime.Now);
                        errLog.WriteLine(e);
                        errLog.WriteLine();
                    }

                    ConsoleColor defaultColor = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("There was an error. It has been logged to 'log.err'. More details below:");
                    Console.ForegroundColor = defaultColor;
                    Console.WriteLine(e);
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
            Console.WriteLine("CSharpMiner.exe <Configuration File Path> [true|false]");
        }
    }
}
