using CSharpMiner.Configuration;
using System;
using System.Collections.Generic;
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

            TcpClient client = new TcpClient("", 0);
        }

        static void WriteUsage()
        {
            Console.WriteLine("CSharpMiner.exe <Configuration File Path>");
        }
    }
}
