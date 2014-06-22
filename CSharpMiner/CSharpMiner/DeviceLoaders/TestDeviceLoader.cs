using CSharpMiner.Stratum;
using MiningDevice;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace DeviceLoader
{
    [DataContract]
    public class TestDeviceLoader : IDeviceLoader
    {
        [DataMember(Name = "ports")]
        public string[] Ports { get; set; }

        [DataMember(Name = "cores")]
        public int Cores { get; set; }

        [IgnoreDataMember]
        public int Id { get; set; }

        public IEnumerable<MiningDevice.IMiningDevice> LoadDevices()
        {
            List<IMiningDevice> devices = new List<IMiningDevice>();

            foreach(string str in Ports)
            {
                devices.Add(new TestDevice(str, Cores));
            }

            return devices;
        }

        public int HashRate
        {
            get { throw new NotImplementedException(); }
        }

        public int HardwareErrors
        {
            get { throw new NotImplementedException(); }
        }

        public void Unload()
        {
            throw new NotImplementedException();
        }

        public void StartWork(CSharpMiner.Stratum.PoolWork work)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            
        }

        public void Load(Action<PoolWork, string, int> submitWork, Action<int> requestWork)
        {
            throw new NotImplementedException();
        }
    }
}
