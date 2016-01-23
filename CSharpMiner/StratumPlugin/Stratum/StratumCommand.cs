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
using System.Runtime.Serialization;

namespace Stratum
{
    [DataContract]
    public abstract class StratumCommand : SerializedJsonObjectBase
    {
        public const string SubscribeCommandString = "mining.subscribe";
        public const string AuthorizationCommandString = "mining.authorize";
        public const string NotifyCommandString = "mining.notify";
        public const string SubmitCommandString = "mining.submit";
        public const string SetDifficlutyCommandString = "mining.set_difficulty";
        public const string ClientReconnectCommandString = "client.reconnect";
        public const string ClientGetVersionCommandString = "client.get_version";
        public const string ClientShowMessageCommandString = "client.show_message";

        [DataMember(Name = "id")]
        public Object Identifier { get; set; }

        [DataMember(Name = "method")]
        public string Method { get; set; }

        /// <summary>
        /// Fix for Mono JsonSerializer that doesn't support deserializing nullable objects. (Mono Fails if the object is null)
        /// So the object that mono is going to parse is Identifier, which works since it is an object, and the actual value will be exposed here
        /// </summary>
        [IgnoreDataMember]
        public Nullable<long> Id
        {
            get
            {
                if(Identifier != null)
                {
                    if (Identifier is long)
                    {
                        return (long)Identifier;
                    }
                    else
                    {
                        return null;
                    }
                }

                return null;
            }
            set
            {
                Identifier = value;
            }
        }

        public StratumCommand()
        {
            Identifier = null;
            Method = string.Empty;
        }

        public StratumCommand(long id, string method)
        {
            Identifier = id;
            Method = method;
        }
    }
}
