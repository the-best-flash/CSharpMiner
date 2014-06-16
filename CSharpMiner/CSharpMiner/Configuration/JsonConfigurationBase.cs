using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using CSharpMiner.Stratum;

namespace CSharpMiner.Configuration
{
    [DataContract]
    public class JsonConfigurationBase : IConfiguration
    {
        private static DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(JsonConfigurationBase));

        [IgnoreDataMember]
        public virtual DataContractJsonSerializer Serializer { get { return JsonConfigurationBase.serializer; } }

        [DataMember(Name = "pools")]
        public Pool[] Pools { get; set; }

        public JsonConfigurationBase()
        {
            Pools = new Pool[0];
        }

        public JsonConfigurationBase(Pool[] pools)
        {
            Pools = pools;
        }

        public void Load(System.IO.Stream stream)
        {
            this.Load((IConfiguration)this.Serializer.ReadObject(stream));
        }

        public void Save(System.IO.Stream stream)
        {
            this.Serializer.WriteObject(stream, this);
        }

        protected virtual void Load(IConfiguration config)
        {
            Pools = config.Pools;
        }
    }
}
