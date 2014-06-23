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
using System.Text;
using System.Threading.Tasks;

namespace CSharpMiner.Stratum
{
    [DataContract]
    public class StratumCommand : SerializedJsonObjectBase
    {
        public static DataContractJsonSerializer _serializer = null;

        public static DataContractJsonSerializer SerializerObject
        {
            get
            {
                if (_serializer == null)
                    _serializer = new DataContractJsonSerializer(typeof(StratumCommand));

                return _serializer;
            }
        }

        public override DataContractJsonSerializer Serializer
        {
            get
            {
                return StratumCommand.SerializerObject;
            }
        }

        public static StratumCommand Deserialize(Stream stream)
        {
            return (StratumCommand)StratumCommand.SerializerObject.ReadObject(stream);
        }

        [DataMember(Name = "id")]
        public Nullable<int> Id { get; set; }

        [DataMember(Name = "method")]
        public string Method { get; set; }

        [DataMember(Name = "params")]
        public Object[] Params { get; set; }

        public StratumCommand()
        {
        }

        /// <summary>
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
        }

        private Tuple<Object[], string> ParseObjectArray(string str)
        {
            List<Object> arr = new List<object>();

            str = str.Trim();

            if(!str.StartsWith("["))
            {
                throw new ArgumentException("Invalid object array string: {0}", str);
            }

            str = str.Substring(1);

            while (!string.IsNullOrEmpty(str))
            {
                string startString = str;

                if (str[0] == '[')
                {
                    Tuple<Object[], string> result = ParseObjectArray(str);
                    arr.Add(result.Item1);
                    str = result.Item2;
                }
                else if(str[0] == ']')
                {
                    if(str.Contains(','))
                    {
                        str = str.Substring(str.IndexOf(',') + 1);
                    }

                    return new Tuple<Object[], string>(arr.ToArray(), str);
                }
                else
                {
                    string item = null;
                    int splitIdx = 0;
                    bool isComma = false;

                    if (str.Contains(',') && str.IndexOf(',') < str.IndexOf(']'))
                    {
                        splitIdx = str.IndexOf(',');
                        isComma = true;
                    }
                    else
                    {
                        splitIdx = str.IndexOf(']');
                    }

                    item = str.Substring(0, splitIdx).Trim();
                    str = str.Substring(splitIdx + (isComma? 1 : 0)); // Keep the ']' but get rid of the ','

                    item = item.Trim();
          
                    if(item[0] == '"')
                    {
                        arr.Add(item.Replace("\"", ""));
                    }
                    else if(item.Contains("true"))
                    {
                        arr.Add(true);
                    }
                    else if (item.Contains("false"))
                    {
                        arr.Add(false);
                    }
                    else if(item.Contains("null"))
                    {
                        arr.Add(null);
                    }
                    else
                    {
                        int i;
                        if(!int.TryParse(item, out i))
                        {
                            throw new InvalidDataException(string.Format("Failed to parse {0} in {1}", item, str));
                        }

                        arr.Add(i);
                    }
                }

                if(str == startString)
                {
                    throw new InvalidDataException(string.Format("Infinate loop Error Parsing {0}", str));
                }
            }

            return null;
        }

        public StratumCommand(int id, string method, string[] paramArr)
        {
            Id = id;
            Method = method;
            Params = paramArr;
        }

        public const string SubscribeCommandString = "mining.subscribe";
        public const string AuthorizationCommandString = "mining.authorize";
        public const string NotifyCommandString = "mining.notify";
        public const string SubmitCommandString = "mining.submit";
        public const string SetDifficlutyCommandString = "mining.set_difficulty";
        public const string ClientReconnectCommandString = "client.reconnect";
        public const string ClientGetVersionCommandString = "client.get_version";
        public const string ClientShowMessageCommandString = "client.show_message";

        public static StratumCommand SubscribeCommand 
        {
            get
            {
                return new StratumCommand(1, SubscribeCommandString, StratumCommand.SubscribeParamArray);
            }
        }

        private static string[] _emptyParamArray = null;

        public static string[] EmptyParamArray
        {
            get
            {
                if (_emptyParamArray == null)
                    _emptyParamArray = new string[0];

                return _emptyParamArray;
            }
        }

        private static string[] _subscribeParamArray = null;

        public static string[] SubscribeParamArray
        {
            get
            {
                if (_subscribeParamArray == null)
                {
                    _subscribeParamArray = new string[1];
                    _subscribeParamArray[0] = string.Format("CSharpMiner_{0}", CSharpMiner.VersionString);
                }

                return _subscribeParamArray;
            }
        }
    }
}
