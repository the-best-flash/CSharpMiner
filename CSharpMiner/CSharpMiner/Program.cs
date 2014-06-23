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

using CSharpMiner.Configuration;
using CSharpMiner.Helpers;
using CSharpMiner.Stratum;
using DeviceManager;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace CSharpMiner
{
    class Program
    {
        public const string VersionString = "0.0.1";

        private static IEnumerable<Type> GetKnownTypes()
        {
            Assembly thisAssembly = Assembly.GetAssembly(typeof(Program));

            // TODO: Get types from module assemblies

            return thisAssembly.GetTypes().Where(t => Attribute.IsDefined(t, typeof(DataContractAttribute)) && !t.IsAbstract);
        }

        static void Main(string[] args)
        {
            if(args.Length < 1 || args.Length > 3)
            {
                WriteUsage();
                return;
            }

            bool loop = true;

            if (args.Length >= 1 && args[0] == "-ls")
            {
                if (args.Length == 1)
                {
                    foreach (Type t in GetKnownTypes())
                    {
                        Console.ForegroundColor = ConsoleColor.DarkGreen;
                        Console.Write("{0}", t.Name);
                        Console.ResetColor();
                        Console.WriteLine("#{0}", t.Namespace);
                    }
                }
                else
                {
                    foreach(Type t in GetKnownTypes().Where(t => t.Name.ToLowerInvariant() == args[1].ToLowerInvariant()))
                    {
                        Console.WriteLine("{0}#{1}", t.Name, t.Namespace);
                        Console.WriteLine();
                        PropertyInfo descriptionProperty = t.GetProperty("UsageDescription", BindingFlags.Static | BindingFlags.Public);

                        if(descriptionProperty != null && descriptionProperty.CanRead)
                        {
                            Console.WriteLine(descriptionProperty.GetValue(null));
                        }
                        else
                        {
                            foreach (PropertyInfo prop in t.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(prop => prop.CanRead && prop.CanWrite && !Attribute.IsDefined(prop, typeof(IgnoreDataMemberAttribute))))
                            {
                                if(Attribute.IsDefined(prop, typeof(DataMemberAttribute)))
                                {
                                    DataMemberAttribute dataMemberAttrib = Attribute.GetCustomAttribute(prop, typeof(DataMemberAttribute)) as DataMemberAttribute;
                                    
                                    if(dataMemberAttrib != null)
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
                return;
            }

            if (args.Length == 2)
            {
                loop = (args[1].ToLower().Trim() != "false");

                if (args[1].ToLower().Trim() != "false" && args[1].ToLower().Trim() != "true")
                {
                    LogHelper.ErrorLogFilePath = args[1];
                }
            }
            else
            {
                loop = (args[2].ToLower().Trim() != "false");

                LogHelper.ErrorLogFilePath = args[1];
            }

            LogHelper.Verbosity = LogVerbosity.Verbose;

            do
            {
                JsonConfiguration config = null;

                try
                {
                    DataContractJsonSerializer jsonSerializer = new DataContractJsonSerializer(typeof(JsonConfiguration), GetKnownTypes());

                    try
                    {
                        using (var inputFile = File.OpenRead(args[0]))
                        {
                            config = jsonSerializer.ReadObject(inputFile) as JsonConfiguration;
                        }
                    }
                    catch (FileNotFoundException e)
                    {
                        LogHelper.ConsoleLogError(string.Format("Configuration file not found. {0}", args[0]));

                        loop = false; // We cannot recover

                        throw new FileNotFoundException(string.Format("Configuration file not found. {0}", args[0]), e);
                    }
                    catch (SerializationException e)
                    {
                        LogHelper.ConsoleLogError("There was an error loading the configuration file:");

                        if (e.InnerException != null)
                        {
                            LogHelper.ConsoleLog(e.InnerException.Message, LogVerbosity.Quiet);
                        }
                        else
                        {
                            LogHelper.ConsoleLog(e, LogVerbosity.Quiet);
                        }

                        loop = false; // We cannot recover

                        throw new SerializationException("There was an error loading the configuration file:", e);
                    }

                    foreach (IMiningDeviceManager m in config.Managers)
                    {
                        m.Start();
                    }

                    ConsoleKey pressedKey;

                    do
                    {
                        pressedKey = Console.ReadKey().Key;

                        if(pressedKey == ConsoleKey.OemPlus || pressedKey == ConsoleKey.Add)
                        {
                            if(LogHelper.Verbosity != LogVerbosity.Verbose)
                            {
                                LogHelper.Verbosity = (LogVerbosity)((int)LogHelper.Verbosity + 1);
                            }

                            WriteVerbosity(LogHelper.Verbosity);
                        }
                        else if(pressedKey == ConsoleKey.OemMinus || pressedKey == ConsoleKey.Subtract)
                        {
                            if(LogHelper.Verbosity != LogVerbosity.VeryQuiet)
                            {
                                LogHelper.Verbosity = (LogVerbosity)((int)LogHelper.Verbosity - 1);
                            }

                            WriteVerbosity(LogHelper.Verbosity);
                        }

                    } while (pressedKey != ConsoleKey.Q);

                    loop = false;

                    using (StreamWriter log = new StreamWriter(File.Open("log.log", FileMode.Append)))
                    {
                        log.WriteLine("Exiting normally at {0}", DateTime.Now);
                    }
                }
                catch (Exception e)
                {
                    LogHelper.LogError(e);

                    LogHelper.ConsoleLogError(string.Format("There was an error. It has been logged to '{0}'", LogHelper.ErrorLogFilePath));
                    LogHelper.ConsoleLog(e, LogVerbosity.Verbose);
                }
                finally
                {
                    if (config != null)
                    {
                        foreach (IMiningDeviceManager m in config.Managers)
                        {
                            m.Stop();
                        }
                    }
                }
            }while(loop);
        }

        static void WriteVerbosity(LogVerbosity verbosity)
        {
            string output = "";

            switch(verbosity)
            {
                case LogVerbosity.Normal:
                    output = "Normal";
                    break;

                case LogVerbosity.Quiet:
                    output = "Quiet";
                    break;
                    
                case LogVerbosity.Verbose:
                    output = "Verbose";
                    break;

                case LogVerbosity.VeryQuiet:
                    output = "VeryQuiet";
                    break;
            }

            LogHelper.ConsoleLog(string.Format("Verbosity: {0}", output), LogVerbosity.VeryQuiet);
        }

        static void WriteUsage()
        {
            LogHelper.ConsoleLog("CSharpMiner.exe <Configuration File Path> [LogFilePath] [true|false]", LogVerbosity.VeryQuiet);
        }
    }
}
