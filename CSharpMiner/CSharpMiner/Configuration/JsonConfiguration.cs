using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using CSharpMiner.Stratum;
using DeviceManager;

namespace CSharpMiner.Configuration
{
    [DataContract]
    public class JsonConfiguration
    {
        [DataMember(Name = "managers")]
        public IMiningDeviceManager[] Managers { get; set; }
    }
}
