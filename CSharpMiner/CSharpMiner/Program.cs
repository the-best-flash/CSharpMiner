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

        [Conditional("DEBUG")]
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
            if(args.Length != 1)
            {
                WriteUsage();
                return;
            }

            DataContractJsonSerializer jsonSerializer = new DataContractJsonSerializer(typeof(JsonConfiguration), GetKnownTypes());
            JsonConfiguration config = null;

            try
            {
                using (var inputFile = File.OpenRead(args[0]))
                {
                    config = jsonSerializer.ReadObject(inputFile) as JsonConfiguration;
                }
            }
            catch (FileNotFoundException)
            {
                Console.WriteLine("Configuration file not found. {0}", args[0]);
                return;
            }
            catch(SerializationException e)
            {
                Console.WriteLine("There was an error loading the configuration file:");
                Console.WriteLine(e.InnerException.Message);
                return;
            }

            foreach(IMiningDeviceManager m in config.Managers)
            {
                m.Start();
            }

            while(Console.ReadKey().Key != ConsoleKey.D)
            {
                // Wait for user to press D to disconnect from pool
            }

            foreach (IMiningDeviceManager m in config.Managers)
            {
                m.Stop();
            }
        }

        static void WriteUsage()
        {
            Console.WriteLine("CSharpMiner.exe <Configuration File Path>");
        }
    }
}
