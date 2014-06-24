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
using System.Text;
using System.Threading.Tasks;

namespace Stratum
{
    /// <summary>
    /// Note: This class is to help work arround lack of support for deserializing arrays of generic objects from JSON in the Mono runtime.
    /// This makes assumptions that are true for the types of commands sent in the stratum protocol. As a result, this won't parse all kinds of JSON.
    /// </summary>
    public static class JsonParsingHelper
    {
        public static Tuple<Object[], string> ParseObjectArray(string str)
        {
            List<Object> arr = new List<object>();

            str = str.Trim();

            if (!str.StartsWith("["))
            {
                throw new ArgumentException("Invalid object array string: {0}", str);
            }

            str = str.Substring(1).Trim();

            while (!string.IsNullOrEmpty(str))
            {
                str = str.Trim();
                string startString = str;

                if (str[0] == '[')
                {
                    Tuple<Object[], string> result = ParseObjectArray(str);
                    arr.Add(result.Item1);
                    str = result.Item2;
                }
                else if (str[0] == ']')
                {
                    if (str.Contains(','))
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
                    str = str.Substring(splitIdx + (isComma ? 1 : 0)); // Keep the ']' but get rid of the ','

                    item = item.Trim();

                    if (item[0] == '"')
                    {
                        arr.Add(item.Replace("\"", ""));
                    }
                    else if (item.Contains("true"))
                    {
                        arr.Add(true);
                    }
                    else if (item.Contains("false"))
                    {
                        arr.Add(false);
                    }
                    else if (item.Contains("null"))
                    {
                        arr.Add(null);
                    }
                    else
                    {
                        int i;
                        if (!int.TryParse(item, out i))
                        {
                            throw new InvalidDataException(string.Format("Failed to parse {0} in {1}", item, str));
                        }

                        arr.Add(i);
                    }
                }

                if (str == startString)
                {
                    throw new InvalidDataException(string.Format("Infinate loop Error Parsing {0}", str));
                }
            }

            return null;
        }
    }
}
