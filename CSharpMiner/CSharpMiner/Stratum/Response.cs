using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace CSharpMiner.Stratum
{
    [DataContract]
    public class Response : SerializedJsonObjectBase
    {
        public static DataContractJsonSerializer _serializer = null;

        public static DataContractJsonSerializer SerializerObject
        {
            get
            {
                if (_serializer == null)
                    _serializer = new DataContractJsonSerializer(typeof(Response));

                return _serializer;
            }
        }

        public override DataContractJsonSerializer Serializer
        {
            get
            {
                return Response.SerializerObject;
            }
        }

        public static Response Deserialize(Stream stream)
        {
            return (Response)Response.SerializerObject.ReadObject(stream);
        }

        [DataMember(Name = "id")]
        public int Id { get; set; }

        [DataMember(Name = "result")]
        public Object Data { get; set; }

        [DataMember(Name = "error")]
        public Object[] Error { get; set; }

        public Response()
        {
        }
    }
}
