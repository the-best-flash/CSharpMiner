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

using CSharpMiner;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

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
        public Nullable<int> Id
        {
            get
            {
                if(Identifier != null)
                {
                    if(Identifier is int)
                    {
                        return (int)Identifier;
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

/*        /// <summary>
        /// Fall back to manually parsing if the JSON parser fails in mono
        /// </summary>
        /// <param name="str"></param>
        public StratumCommand(string str)
        {
            int firstColon = str.IndexOf(':') + 1;
            string idStr = str.Substring(firstColon, str.IndexOf(',') - firstColon);

            if (!idStr.Contains("null"))
            {
                int i;
                if (!int.TryParse(idStr, out i))
                {
                    throw new InvalidDataException(string.Format("Error parsing command: {0}", str));
                }

                Id = i;
            }
            else
            {
                Id = null;
            }

            string searchStr = "\"method\"";
            int methodPos = str.IndexOf(searchStr) + searchStr.Length;
            string methodStr = str.Substring(methodPos, str.IndexOf("\"params\"") - methodPos);

            int firstQuote = methodStr.IndexOf('"') + 1;
            methodStr = methodStr.Substring(firstQuote, methodStr.LastIndexOf('"') - firstQuote);
            Method = methodStr;
            
            string [] paramToken = new []{"\"params\":"};

            string[] split = str.Split(paramToken, StringSplitOptions.None);

            if(split.Length < 2)
            {
                throw new InvalidDataException(string.Format("Error parsing {0}", str));
            }

            Params = ParseObjectArray(split[1]).Item1;
        } */

        public StratumCommand()
        {
            Identifier = null;
            Method = string.Empty;
        }

        public StratumCommand(int id, string method)
        {
            Identifier = id;
            Method = method;
        }
    }
}
