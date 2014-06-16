using CSharpMiner.Configuration;
using CSharpMiner.Stratum;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.Serialization;
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

        static void Main(string[] args)
        {
            if(args.Length != 1)
            {
                WriteUsage();
                return;
            }

            JsonConfigurationBase config = new JsonConfigurationBase();

            try
            {
                using (var inputFile = File.OpenRead(args[0]))
                {
                    config.Load(inputFile);
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

            foreach(Pool p in config.Pools)
            {
                Console.WriteLine("Pool: {0}", p.Url);

                try
                {
                    p.Start();
                }
                catch (StratumConnectionFailureException e)
                {
                    Console.WriteLine("Error connecting to pool {0}", p.Url);
                    Console.WriteLine("Connection Error: {0}", e.Message);
                }
            }

            while(Console.ReadKey().Key != ConsoleKey.D)
            {
                // Wait for user to press D to disconnect from pool
            }

            foreach(Pool p in config.Pools)
            {
                DebugConsoleLog(string.Format("Disconnecting from pool {0}", p.Url));

                p.Stop();
                p.Thread.Join();
            }
        }

        static void WriteUsage()
        {
            Console.WriteLine("CSharpMiner.exe <Configuration File Path>");
        }
    }
}
