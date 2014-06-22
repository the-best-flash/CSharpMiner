/*  Copyright (C) 2014 Colton Manville
    This file is part of CSharpMiner.

    CSharpMiner is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    CSharpMiner is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with CSharpMiner.  If not, see <http://www.gnu.org/licenses/>.*/

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
