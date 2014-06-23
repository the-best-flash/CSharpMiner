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
                Console.WriteLine(":#{0}", t.Namespace);
            }
        }

        public static void DisplayKnownTypeInfo(string typeName)
        {
            foreach (Type t in GetKnownTypes().Where(t => t.Name.ToLowerInvariant() == typeName.ToLowerInvariant()))
            {
                Console.WriteLine("{0}:#{1}", t.Name, t.Namespace);
                Console.WriteLine();
                
                if (Attribute.IsDefined(t, typeof(MiningModuleAttribute)))
                {
                    MiningModuleAttribute attrib = Attribute.GetCustomAttribute(t, typeof(MiningModuleAttribute)) as MiningModuleAttribute;

                    if(attrib != null)
                    {
                        Console.WriteLine(attrib.Description);
                    }
                }

                IEnumerable<PropertyInfo> serializableProperties = t.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(prop => prop.CanRead && prop.CanWrite && !Attribute.IsDefined(prop, typeof(IgnoreDataMemberAttribute)));

                foreach (PropertyInfo prop in serializableProperties)
                {
                    if (Attribute.IsDefined(prop, typeof(DataMemberAttribute)))
                    {
                        DataMemberAttribute dataMemberAttrib = Attribute.GetCustomAttribute(prop, typeof(DataMemberAttribute)) as DataMemberAttribute;

                        if (dataMemberAttrib != null)
                        {
                            Console.WriteLine("\t{0} : {1}", dataMemberAttrib.Name, prop.PropertyType.Name);
                        }
                    }
                    else
                    {
                        Console.WriteLine("\t{0} : {1}", prop.Name, prop.PropertyType.Name);
                    }

                    if(Attribute.IsDefined(prop, typeof(MiningSettingAttribute)))
                    {
                        MiningSettingAttribute attrib = Attribute.GetCustomAttribute(prop, typeof(MiningSettingAttribute)) as MiningSettingAttribute;
 
                        if(attrib != null)
                        {
                            Console.WriteLine("\t\t{1}{0}", (attrib.Optional ? "(Optional) " : ""), attrib.Description);
                        }
                    }
                }

                Console.WriteLine("Example JSON Format:");
                Console.WriteLine(GetJSONFormatExample(t, serializableProperties));

                Console.WriteLine();
            }
        }

        private static string GetJSONFormatExample(Type t, IEnumerable<PropertyInfo> serializableProperties, string append = "")
        {
            if(!t.IsAbstract && Attribute.IsDefined(t, typeof(DataContractAttribute)))
            {
                StringBuilder sb = new StringBuilder();

                sb.AppendLine(append + "{");

                sb.AppendLine(string.Format("{0}    \"__type\" : \"{1}:#{2}\",", append, t.Name, t.Namespace));

                foreach(PropertyInfo info in serializableProperties)
                {
                    string name = null;

                    if (Attribute.IsDefined(info, typeof(DataMemberAttribute)))
                    {
                        DataMemberAttribute dataMemberAttrib = Attribute.GetCustomAttribute(info, typeof(DataMemberAttribute)) as DataMemberAttribute;

                        if (dataMemberAttrib != null)
                        {
                            name = dataMemberAttrib.Name;
                        }
                    }
                    
                    if(string.IsNullOrEmpty(name))
                    {
                        name = info.Name;
                    }

                    sb.AppendLine(string.Format("{0}    \"{1}\" : {2},", append, info.Name, GetExampleValue(info)));
                }

                sb.AppendLine(append + "}");
                return sb.ToString();
            }

            return string.Empty;
        }

        private static string GetExampleValue(PropertyInfo prop)
        {
            Type propertyType = prop.PropertyType;

            string exampleValue = null;

            if(Attribute.IsDefined(prop, typeof(MiningSettingAttribute)))
            {
                MiningSettingAttribute attrib = Attribute.GetCustomAttribute(prop, typeof(MiningSettingAttribute)) as MiningSettingAttribute;

                if(attrib != null)
                {
                    exampleValue = attrib.ExampleValue;
                }
            }

            if(string.IsNullOrEmpty(exampleValue))
            {
                if (propertyType == typeof(string))
                {
                    if (string.IsNullOrEmpty(exampleValue))
                    {
                        exampleValue = "A string";
                    }
                }
                else if (propertyType.IsArray)
                {
                    if (string.IsNullOrEmpty(exampleValue))
                    {
                        exampleValue = "[]";
                    }
                }
                else if (Attribute.IsDefined(propertyType, typeof(DataContractAttribute)) && !propertyType.IsAbstract)
                {
                    if (string.IsNullOrEmpty(exampleValue))
                    {
                        exampleValue = GetJSONFormatExample(propertyType);
                    }
                }
                else
                {
                    exampleValue = "\"Unknown type. Disregard this example.\"";
                }
            }

            if(propertyType == typeof(string))
            {
                return string.Format("\"{0}\"", exampleValue);
            }
            else
            {
                return exampleValue;
            }
        }

        public static string GetJSONFormatExample(Type t)
        {
            return GetJSONFormatExample(t, t.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(prop => prop.CanRead && prop.CanWrite && !Attribute.IsDefined(prop, typeof(IgnoreDataMemberAttribute))));
        }
    }
}
