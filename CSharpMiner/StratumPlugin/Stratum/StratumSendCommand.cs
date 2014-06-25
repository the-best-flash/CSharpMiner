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

namespace Stratum
{
    [DataContract]
    public class StratumSendCommand : StratumCommand
    {
        private static DataContractJsonSerializer _serializer = null;
        public override DataContractJsonSerializer Serializer
        {
            get
            {
                if (_serializer == null)
                {
                    _serializer = new DataContractJsonSerializer(typeof(StratumSendCommand));
                }

                return _serializer;
            }
        }

        public static StratumSendCommand SubscribeCommand
        {
            get
            {
                return new StratumSendCommand(1, SubscribeCommandString, StratumSendCommand.SubscribeParamArray);
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

        public static Object[] SubscribeParamArray
        {
            get
            {
                if (_subscribeParamArray == null)
                {
                    _subscribeParamArray = new string[1];
                    _subscribeParamArray[0] = string.Format("CSharpMiner_{0}", Miner.VersionString);
                }

                return _subscribeParamArray;
            }
        }

        public Object[] _params = null;
        [DataMember(Name = "params")]
        public Object[] Params { get; set; }

        public StratumSendCommand(long id, string method, Object[] param)
            : base(id, method)
        {
            Params = param;
        }
    }
}
