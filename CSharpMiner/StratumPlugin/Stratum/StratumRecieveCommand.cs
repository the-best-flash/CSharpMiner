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
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace Stratum
{
    [DataContract]
    public class StratumRecieveCommand : StratumCommand
    {
        private static DataContractJsonSerializer _serializer = null;
        private static DataContractJsonSerializer JsonSerializer
        {
            get
            {
                if (_serializer == null)
                {
                    _serializer = new DataContractJsonSerializer(typeof(StratumRecieveCommand));
                }

                return _serializer;
            }
        }

        public override DataContractJsonSerializer Serializer
        {
            get
            {
                return JsonSerializer;
            }
        }

        public Object[] Params { get; private set; }

        public Object _paramValue = null;
        [DataMember(Name = "params")]
        public Object ParamValue
        {
            get
            {
                return _paramValue;
            }
            set
            {
                if (value is string)
                {
                    string str = value as string;

                    str = str.Replace("\\\"", "\"");
                    Params = JsonParsingHelper.ParseObjectArray(str).Item1;
                }
                else if (!value.GetType().IsArray)
                {
                    Params = new Object[] { value };
                }
                else
                {
                    Params = value as Object[];
                }

                _paramValue = value;
            }
        }

        public static StratumRecieveCommand Deserialize(Stream stream)
        {
            return JsonSerializer.ReadObject(stream) as StratumRecieveCommand;
        }

        public StratumRecieveCommand() : base()
        {
            _paramValue = null;
        }
    }
}
