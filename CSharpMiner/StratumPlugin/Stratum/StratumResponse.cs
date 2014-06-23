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
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace Stratum
{
    [DataContract]
    public class StratumResponse : SerializedJsonObjectBase
    {
        public static DataContractJsonSerializer _serializer = null;

        public static DataContractJsonSerializer SerializerObject
        {
            get
            {
                if (_serializer == null)
                    _serializer = new DataContractJsonSerializer(typeof(StratumResponse));

                return _serializer;
            }
        }

        public override DataContractJsonSerializer Serializer
        {
            get
            {
                return StratumResponse.SerializerObject;
            }
        }

        public static StratumResponse Deserialize(Stream stream)
        {
            return (StratumResponse)StratumResponse.SerializerObject.ReadObject(stream);
        }

        [DataMember(Name = "id")]
        public int Id { get; set; }

        [DataMember(Name = "result")]
        public Object Data { get; set; }

        [DataMember(Name = "error")]
        public Object[] Error { get; set; }

        public StratumResponse()
        {
        }

        /// <summary>
        /// Fallback method for mono JSON parsing failure
        /// </summary>
        /// <param name="str">JSON string</param>
        public StratumResponse(string str)
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
