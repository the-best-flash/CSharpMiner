using CSharpMiner.Stratum;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace CSharpMiner.Configuration
{
    public interface IConfiguration
    {
        DataContractJsonSerializer Serializer { get; }
        Pool[] Pools { get; set; }

        void Load(System.IO.Stream stream);
        void Save(System.IO.Stream stream);
    }
}
