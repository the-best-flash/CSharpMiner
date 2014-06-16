using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace CSharpMiner.Stratum
{
    public class StratumConnectionFailureException : Exception
    {
        public StratumConnectionFailureException() : base()
        {
        }

        public StratumConnectionFailureException(string message) : base(message)
        {
        }

        public StratumConnectionFailureException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public StratumConnectionFailureException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public StratumConnectionFailureException(SocketException e) : base(StratumConnectionFailureException.GetMessageFromSocketException(e), e)
        {
        }

        private static string GetMessageFromSocketException(SocketException e)
        {
            switch(e.SocketErrorCode)
            {
                case SocketError.AccessDenied:
                    return "Socket Access Denied";

                case SocketError.InProgress:
                    return "A blocking operation was in progress.";

                case SocketError.Interrupted:
                    return "Socket call interrupted.";

                case SocketError.IsConnected:
                    return "This socket is already connected.";

                case SocketError.ProcessLimit:
                    return "Too many processes are using the underlying socket provider.";

                case SocketError.TryAgain:
                    return "The name of the host could not be resolved. Try again later.";

                default:
                    return string.Format("Socket Error: {0}", e.SocketErrorCode);
            }
        }
    }
}
