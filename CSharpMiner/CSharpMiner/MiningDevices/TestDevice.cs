using CSharpMiner.Stratum;
using DeviceManager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace MiningDevice
{
    [DataContract]
    public class TestDevice : IMiningDevice
    {
        [DataMember(Name = "path")]
        public string Path { get; set; }

        [DataMember(Name = "cores")]
        public int Cores { get; set; }

        [IgnoreDataMember]
        public int Id { get; set; }

        [IgnoreDataMember]
        public int HashRate
        {
            get { return 1; }
        }

        [IgnoreDataMember]
        public int HardwareErrors
        {
            get { return 0; }
        }

        public TestDevice(string path, int cores)
        {
            Path = path;
            Cores = cores;
        }

        public void Load(Action<PoolWork, string, int> submitWork)
        {
            
            Console.WriteLine("Loading Miner {0}", Path);
        }

        public void Unload()
        {
            Console.WriteLine("Unloading Miner {0}", Path);
        }

        public void StartWork(PoolWork work)
        {
            Console.WriteLine("Miner {0} starting work {1} with:", Path, work.JobId);
            Console.WriteLine("\tExtranonce2: {0}", work.Extranonce2);
            Console.WriteLine("\tStartNonce:  {0}", work.StartingNonce);
        }

        public void Dispose()
        {
            this.Unload();
        }
    }
}
