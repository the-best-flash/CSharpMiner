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
    public abstract class SerializedJsonObjectBase
    {
        public const string NewLine = "\n";

        private static byte[] _newLineBytes = null;

        public static byte[] NewLineBytes
        {
            get
            {
                if(_newLineBytes == null)
                {
                    ASCIIEncoding asen = new ASCIIEncoding();
                    _newLineBytes = asen.GetBytes(NewLine);
                }

                return _newLineBytes;
            }
        }

        [IgnoreDataMember]
        public abstract DataContractJsonSerializer Serializer { get; }

        public SerializedJsonObjectBase()
        {
        }

        public void Serialize(Stream stream)
        {
            this.Serializer.WriteObject(stream, this);
            stream.Write(NewLineBytes, 0, NewLineBytes.Length);
        }
    }
}
