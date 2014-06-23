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

using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace Stratum
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
