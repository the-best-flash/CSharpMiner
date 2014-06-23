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
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace CSharpMiner.ModuleLoading
{
    public static class ModuleLoader
    {
        public static IEnumerable<Type> GetKnownTypes()
        {
            Assembly thisAssembly = Assembly.GetAssembly(typeof(ModuleLoader));

            // TODO: Get types from module assemblies

            return thisAssembly.GetTypes().Where(t => Attribute.IsDefined(t, typeof(DataContractAttribute)) && !t.IsAbstract);
        }

        public static void DisplayKnownTypes()
        {
            foreach (Type t in GetKnownTypes())
            {
                Console.ForegroundColor = ConsoleColor.DarkGreen;
                Console.Write("{0}", t.Name);
                Console.ResetColor();
                Console.WriteLine("#{0}", t.Namespace);
            }
        }

        public static void DisplayKnownTypeInfo(string typeName)
        {
            foreach (Type t in GetKnownTypes().Where(t => t.Name.ToLowerInvariant() == typeName.ToLowerInvariant()))
            {
                Console.WriteLine("{0}#{1}", t.Name, t.Namespace);
                Console.WriteLine();
                PropertyInfo descriptionProperty = t.GetProperty("UsageDescription", BindingFlags.Static | BindingFlags.Public);

                if (descriptionProperty != null && descriptionProperty.CanRead)
                {
                    Console.WriteLine(descriptionProperty.GetValue(null));
                }
                else
                {
                    foreach (PropertyInfo prop in t.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(prop => prop.CanRead && prop.CanWrite && !Attribute.IsDefined(prop, typeof(IgnoreDataMemberAttribute))))
                    {
                        if (Attribute.IsDefined(prop, typeof(DataMemberAttribute)))
                        {
                            DataMemberAttribute dataMemberAttrib = Attribute.GetCustomAttribute(prop, typeof(DataMemberAttribute)) as DataMemberAttribute;

                            if (dataMemberAttrib != null)
                            {
                                Console.WriteLine("\t{0} : {1}", dataMemberAttrib.Name, prop.PropertyType.Name);
                            }
                        }
                    }
                }

                PropertyInfo formatProperty = t.GetProperty("ExampleJSONFormat", BindingFlags.Static | BindingFlags.Public);

                if (formatProperty != null && formatProperty.CanRead)
                {
                    Console.WriteLine("Example JSON Format:");
                    Console.WriteLine(formatProperty.GetValue(null));
                }

                Console.WriteLine();
            }
        }
    }
}
