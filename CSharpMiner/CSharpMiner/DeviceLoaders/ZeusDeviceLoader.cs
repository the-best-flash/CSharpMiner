using CSharpMiner.Stratum;
using DeviceManager;
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
    public class ZeusDeviceLoader : IDeviceLoader
    {
        [DataMember(Name = "cores")]
        public int Cores { get; set; }

        [DataMember(Name = "ports")]
        public string[] Ports { get; set; }

        [DataMember(Name = "clock")]
        public int LtcClk { get; set; }

        [IgnoreDataMember]
        public int Id { get; set; }

        [IgnoreDataMember]
        public int HashRate
        {
            get { throw new NotImplementedException(); }
        }

        [IgnoreDataMember]
        public int HardwareErrors
        {
            get { throw new NotImplementedException(); }
        }

        public IEnumerable<IMiningDevice> LoadDevices()
        {
            List<IMiningDevice> devices = new List<IMiningDevice>();

            foreach(string p in Ports)
            {
                devices.Add(new ZeusDevice(p, LtcClk, Cores));
            }

            return devices;
        }

        public void Unload()
        {
            throw new NotImplementedException();
        }

        public void StartWork(PoolWork work)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            // Do Nothing
        }

        public void Load(Action<PoolWork, string, int> submitWork, Action<int> requestWork)
        {
            throw new NotImplementedException();
        }
    }
}
