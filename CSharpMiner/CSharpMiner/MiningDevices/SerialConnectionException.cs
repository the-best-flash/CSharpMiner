using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace MiningDevice
{
    class SerialConnectionException : Exception
    {
        public SerialConnectionException() : base()
        {

        }

        public SerialConnectionException(string message) : base(message)
        {

        }

        public SerialConnectionException(string message, Exception innerException) : base(message, innerException)
        {

        }

        public SerialConnectionException(SerializationInfo info, StreamingContext context) : base(info, context)
        {

        }
    }
}
