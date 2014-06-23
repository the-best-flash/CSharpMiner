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

using CSharpMiner.Helpers;
using CSharpMiner.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;

namespace CSharpMiner.ModuleLoading
{
    public static class ModuleLoader
    {
        private static string _moduleFolder = "bin";
        public static string ModuleFolder
        {
            get
            {
                return _moduleFolder;
            }
            
            set
            {
                _moduleFolder = value;
            }
        }

        private static IEnumerable<Type> _knownTypes = null;
        public static IEnumerable<Type> KnownTypes
        {
            get
            {
                if (_knownTypes == null)
                {
                    if (Directory.Exists(ModuleFolder))
                    {
                        LogHelper.DebugConsoleLog(string.Format("Loading modules from {0}...", ModuleFolder));
                        foreach (string filename in Directory.EnumerateFiles(ModuleFolder))
                        {
                            LogHelper.DebugConsoleLog(string.Format("Attempting to load assembly {0}", filename));

                            Assembly assembly = null;

                            try
                            {
                                assembly = Assembly.LoadFrom(filename);
                                LogHelper.DebugConsoleLog(string.Format("Successfully loaded assembly {0}", filename));
                            }
                            catch (BadImageFormatException)
                            {
                                LogHelper.DebugConsoleLog(string.Format("{0} is not an assembly...", filename));
                            }

                            if(assembly != null)
                            {
                                if(_knownTypes == null)
                                {
                                    _knownTypes = GetKnownTypesFromAssembly(assembly);
                                }
                                else
                                {
                                    _knownTypes = _knownTypes.Concat(GetKnownTypesFromAssembly(assembly));
                                }
                            }
                        }
                    }
                    else
                    {
                        LogHelper.LogError(string.Format("Could not find module directory {0}", ModuleFolder));
                    }

                    if (_knownTypes == null)
                    {
                        _knownTypes = GetKnownTypesFromAssembly(Assembly.GetAssembly(typeof(ModuleLoader)));
                    }
                    else
                    {
                        _knownTypes = _knownTypes.Concat(GetKnownTypesFromAssembly(Assembly.GetAssembly(typeof(ModuleLoader))));
                    }
                }

                return _knownTypes;
            }
        }

        private static IEnumerable<Type> GetKnownTypesFromAssembly(Assembly assembly)
        {
            return assembly.GetTypes().Where(t => Attribute.IsDefined(t, typeof(DataContractAttribute)) && !t.IsAbstract);
        }

        public static void DisplayKnownTypes()
        {
            IEnumerable<Type> knownTypes = KnownTypes;

            IEnumerable<Type> hotplugLoaders = knownTypes.Where(t => t.GetInterfaces().Contains(typeof(IHotplugLoader)));
            IEnumerable<Type> deviceLoaders = knownTypes.Where(t => t.GetInterfaces().Contains(typeof(IDeviceLoader)));
            IEnumerable<Type> devices = knownTypes.Where(t => t.GetInterfaces().Contains(typeof(IMiningDevice)));
            IEnumerable<Type> deviceManagers = knownTypes.Where(t => t.GetInterfaces().Contains(typeof(IMiningDeviceManager)));
            IEnumerable<Type> pools = knownTypes.Where(t => t.GetInterfaces().Contains(typeof(IPool)));
            IEnumerable<Type> others = knownTypes.Except(deviceLoaders).Except(deviceManagers).Except(devices).Except(pools);

            if (devices.Count() > 0)
            {
                Console.WriteLine("Devices:");
                Console.WriteLine();
                OutputTypeInfoList(devices, "\t");
            }

            if (deviceLoaders.Count() > 0)
            {
                Console.WriteLine("Device Loaders:");
                Console.WriteLine();
                OutputTypeInfoList(deviceLoaders, "\t");
            }

            if(hotplugLoaders.Count() > 0)
            {
                Console.WriteLine("Hotplug Loaders:");
                Console.WriteLine();
                OutputTypeInfoList(hotplugLoaders, "\t");
            }

            if (deviceManagers.Count() > 0)
            {
                Console.WriteLine("Device Managers:");
                Console.WriteLine();
                OutputTypeInfoList(deviceManagers, "\t");
            }

            if (pools.Count() > 0)
            {
                Console.WriteLine("Pool Managers:");
                Console.WriteLine();
                OutputTypeInfoList(pools, "\t");
            }

            if (others.Count() > 0)
            {
                Console.WriteLine("Others:");
                Console.WriteLine();
                OutputTypeInfoList(others, "\t");
            }
        }

        private static void OutputTypeInfoList(IEnumerable<Type> types, string append = "")
        {
            foreach (Type t in types)
            {
                OuputJSONTypeString(t, append);
            }

            Console.WriteLine();
        }

        private static void OuputJSONTypeString(Type t, string append = "")
        {
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.Write("{0}{1}", append, t.Name);
            Console.ResetColor();
            Console.WriteLine(":#{0}", t.Namespace);
        }

        public static void DisplayKnownTypeInfo(string typeName)
        {
            foreach (Type t in KnownTypes.Where(t => t.Name.ToLowerInvariant() == typeName.ToLowerInvariant()))
            {
                Console.WriteLine();
                Console.WriteLine();
                OuputJSONTypeString(t);
                Console.WriteLine();
                
                if (Attribute.IsDefined(t, typeof(MiningModuleAttribute)))
                {
                    MiningModuleAttribute attrib = Attribute.GetCustomAttribute(t, typeof(MiningModuleAttribute)) as MiningModuleAttribute;

                    if(attrib != null)
                    {
                        Console.WriteLine("    Description: ");
                        Console.WriteLine();
                        Console.WriteLine("        {0}", attrib.Description);
                    }
                }

                IEnumerable<PropertyInfo> serializableProperties = t.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(prop => prop.CanRead && prop.CanWrite && !Attribute.IsDefined(prop, typeof(IgnoreDataMemberAttribute)));

                foreach (PropertyInfo prop in serializableProperties)
                {
                    Console.WriteLine();

                    string name = "";

                    if (Attribute.IsDefined(prop, typeof(DataMemberAttribute)))
                    {
                        DataMemberAttribute dataMemberAttrib = Attribute.GetCustomAttribute(prop, typeof(DataMemberAttribute)) as DataMemberAttribute;

                        if (dataMemberAttrib != null)
                        {
                            name = dataMemberAttrib.Name;
                        }
                    }
                    else
                    {
                        name = prop.Name;
                    }

                    Console.ForegroundColor = ConsoleColor.DarkGreen;
                    Console.Write("    {0}", name);
                    Console.ResetColor();
                    Console.Write(" : ");
                    Console.ForegroundColor = ConsoleColor.DarkCyan;
                    Console.Write(prop.PropertyType.Name);
                    Console.ResetColor();

                    if(Attribute.IsDefined(prop, typeof(MiningSettingAttribute)))
                    {
                        MiningSettingAttribute attrib = Attribute.GetCustomAttribute(prop, typeof(MiningSettingAttribute)) as MiningSettingAttribute;
 
                        if(attrib != null)
                        {
                            Console.ForegroundColor = ConsoleColor.DarkMagenta;
                            Console.WriteLine((attrib.Optional ? " (Optional)" : ""));
                            Console.ResetColor();
                            Console.WriteLine();
                            Console.WriteLine("        {0}", attrib.Description);
                        }
                    }
                    else
                    {
                        Console.WriteLine();
                    }
                }

                Console.WriteLine();
                Console.WriteLine();
                Console.WriteLine("Example JSON Format:");
                Console.WriteLine();
                Console.WriteLine(GetJSONFormatExample(t, serializableProperties, "    "));

                Console.WriteLine();
            }
        }

        private static string GetJSONFormatExample(Type t, IEnumerable<PropertyInfo> serializableProperties, string append = "", bool showType = true, int recursion = 0)
        {
            if(recursion > 20) // We're probably infinate looping on a type that contains a type that contains the first type. Or this is an extremely complex JSON object. Either way we can be done now.
            {
                return string.Empty;
            }

            if(!t.IsAbstract && Attribute.IsDefined(t, typeof(DataContractAttribute)))
            {
                StringBuilder sb = new StringBuilder();

                sb.AppendLine(append + "{");

                if (showType)
                {
                    sb.Append(string.Format("{0}    \"__type\" : \"{1}:#{2}\"", append, t.Name, t.Namespace));
                }

                bool first = true;

                foreach(PropertyInfo info in serializableProperties)
                {
                    if (showType || !first)
                    {
                        sb.AppendLine(",");
                    }

                    first = false;

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

                    sb.Append(string.Format("{0}    \"{1}\" : {2}", append, name, GetExampleValue(info, append, recursion)));
                }

                sb.AppendLine();

                sb.AppendLine(append + "}");
                return sb.ToString();
            }

            return string.Empty;
        }

        private static string GetExampleValue(PropertyInfo prop, string append, int recursion)
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
                else if ((Attribute.IsDefined(propertyType, typeof(DataContractAttribute)) && !propertyType.IsAbstract) ||
                    (propertyType.IsArray && Attribute.IsDefined(propertyType.GetElementType(), typeof(DataContractAttribute)) && !propertyType.GetElementType().IsAbstract))
                {
                    Type propType = (propertyType.IsArray ? propertyType.GetElementType() : propertyType);

                    if (string.IsNullOrEmpty(exampleValue))
                    {
                        IEnumerable<PropertyInfo> serializableProperties = propType.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(propertyInfo => propertyInfo.CanRead && propertyInfo.CanWrite && !Attribute.IsDefined(propertyInfo, typeof(IgnoreDataMemberAttribute)));
                        exampleValue = GetJSONFormatExample(propType, serializableProperties, append + "    ", false, recursion + 1);
                    }

                    if(propertyType.IsArray)
                    {
                        exampleValue = string.Format("[{0}]", exampleValue);
                    }
                }
                else if (propertyType.IsArray)
                {
                    if (string.IsNullOrEmpty(exampleValue))
                    {
                        exampleValue = "[]";
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
