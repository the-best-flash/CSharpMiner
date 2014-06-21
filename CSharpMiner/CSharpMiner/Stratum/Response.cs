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

        /// <summary>
        /// Fallback method for mono JSON parsing failure
        /// </summary>
        /// <param name="str">JSON string</param>
        public Response(string str)
        {
            int idParam = str.IndexOf("id");
            string idParamStr = str.Substring(idParam, str.IndexOf(',', idParam) - idParam);
            string[] split = idParamStr.Split(':');

            int i;
            if(split.Length < 2 || !int.TryParse(split[1], out i))
            {
                throw new InvalidDataException(string.Format("Error Parsing {0}", str));
            }

            Id = i;

            string[] separator = new string[]{"\"error\":"};
            split = str.Split(separator, StringSplitOptions.None);

            if(split.Length < 2)
            {
                throw new InvalidDataException(string.Format("Error Parsing {0}", str));
            }

            string[] errorParts = split[1].Replace("(", "").Replace(")", "").Split(',');
            Error = errorParts;

            int firstComma = str.IndexOf(",");
            string resultParam = str.Substring(str.IndexOf(","), str.IndexOf("\"error\":") - firstComma);

            string lowerCase = resultParam.ToLowerInvariant();
            if(lowerCase.Contains("true"))
            {
                this.Data = true;
            }
            else if (lowerCase.Contains("null") || lowerCase.Contains("false"))
            {
                this.Data = false;
            }
            else
            {
                resultParam = resultParam.Substring(0, resultParam.LastIndexOf(']'));
                split = resultParam.Split(',');
                Object[] array = new Object[3];
                Data = array;

                if(split.Length < 2 || !int.TryParse(split[split.Length - 1], out i))
                {
                    throw new InvalidDataException(string.Format("Error Parsing {0}", str));
                }

                array[2] = i;
                array[1] = split[split.Length - 2].Replace("\"", "").Trim();
            }
        }
    }
}
