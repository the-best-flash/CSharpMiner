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
using CSharpMiner.Helpers;
using CSharpMiner.ModuleLoading;
using System;
using System.IO;
using System.Linq;
using System.Threading;

namespace CSharpMinerProgram
{
    class Program
    {
        static void LogCompositeParamError(string param)
        {
            LogHelper.ConsoleLogError(string.Format("Incorrect param format for {0}", param));
            WriteUsage();
        }

        static void RunProgram(string[] args)
        {
            if (args.Length > 3)
            {
                WriteUsage();
                return;
            }

            string configFilePath = "config.conf";

            foreach (string arg in args)
            {
                if (arg.StartsWith("-c", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (!arg.Contains(':'))
                    {
                        LogCompositeParamError("-c");
                        return; // We cannot recover
                    }

                    string[] split = arg.Split(':');
                    configFilePath = split[1];
                }
                else if (arg.StartsWith("-log", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (!arg.Contains(':'))
                    {
                        LogCompositeParamError("-log");
                        return; // We cannot recover
                    }

                    string[] split = arg.Split(':');
                    LogHelper.ErrorLogFilePath = split[1];
                }
                else if (arg.StartsWith("-m", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (!arg.Contains(':'))
                    {
                        LogCompositeParamError("-modules");
                        return; // We cannot recover
                    }

                    string[] split = arg.Split(':');
                    ModuleLoader.ModuleFolder = split[1];
                }
                else if (arg.StartsWith("-ls", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (!arg.Contains(':'))
                    {
                        ModuleLoader.DisplayKnownTypes();
                    }
                    else
                    {
                        string[] split = arg.Split(':');
                        ModuleLoader.DisplayKnownTypeInfo(split[1]);
                    }

                    return;
                }
                else if (arg.StartsWith("-v", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (!arg.Contains(':'))
                    {
                        LogCompositeParamError("-verbosity");
                        return; // We cannot recover
                    }

                    string[] split = arg.Split(':');

                    if (split[1].StartsWith("n", StringComparison.InvariantCultureIgnoreCase))
                    {
                        LogHelper.Verbosity = LogVerbosity.Normal;
                    }
                    else if (split[1].StartsWith("q", StringComparison.InvariantCultureIgnoreCase))
                    {
                        LogHelper.Verbosity = LogVerbosity.Quiet;
                    }
                    else if (split[1].StartsWith("verb", StringComparison.InvariantCultureIgnoreCase))
                    {
                        LogHelper.Verbosity = LogVerbosity.Verbose;
                    }
                    else if (split[1].StartsWith("very", StringComparison.InvariantCultureIgnoreCase))
                    {
                        LogHelper.Verbosity = LogVerbosity.VeryQuiet;
                    }
                    else
                    {
                        LogHelper.ConsoleLogError(string.Format("Unrecognized verbosity option: {0}", split[1]));
                        WriteUsage();
                        return;
                    }
                }
                else if (arg.StartsWith("-h", StringComparison.InvariantCultureIgnoreCase))
                {
                    WriteUsage();
                    return;
                }
                else
                {
                    LogHelper.ConsoleLogError(string.Format("Unrecognized command: {0}", arg));
                    WriteUsage();
                    return; // We cannot recover
                }
            }

            bool loaded = false; // Make sure that we don't loop indefinately if we can't even get the miner started
            Miner miner = null;
            bool loop = true;

            do
            {
                try
                {
                    miner = new Miner();

                    miner.Start(configFilePath);

                    loaded = true;

                    ConsoleKey pressedKey;

                    do
                    {
                        Thread.Sleep(1000);

                        pressedKey = Console.ReadKey().Key;

                        if (pressedKey == ConsoleKey.OemPlus || pressedKey == ConsoleKey.Add || pressedKey.ToString() == "=")
                        {
                            if (LogHelper.Verbosity != LogVerbosity.Verbose)
                            {
                                LogHelper.Verbosity = (LogVerbosity)((int)LogHelper.Verbosity + 1);
                            }

                            WriteVerbosity(LogHelper.Verbosity);
                        }
                        else if (pressedKey == ConsoleKey.OemMinus || pressedKey == ConsoleKey.Subtract)
                        {
                            if (LogHelper.Verbosity != LogVerbosity.VeryQuiet)
                            {
                                LogHelper.Verbosity = (LogVerbosity)((int)LogHelper.Verbosity - 1);
                            }

                            WriteVerbosity(LogHelper.Verbosity);
                        }

                    } while (pressedKey != ConsoleKey.Q);

                    loop = false;
                }
                catch (Exception e)
                {
                    LogHelper.LogError(e);

                    if (miner != null)
                    {
                        miner.Stop();
                    }
                }
            } while (loop && loaded);

            if (miner != null)
            {
                miner.Stop();
            }

            using (StreamWriter log = new StreamWriter(File.Open("log.log", FileMode.Append)))
            {
                log.WriteLine("Exiting normally at {0}", DateTime.Now);
            }
        }

        static void Main(string[] args)
        {
            LogHelper.ErrorLogFilePath = "err.log";

            #if DEBUG
            LogHelper.Verbosity = LogVerbosity.Verbose;
            #else
            LogHelper.Verbosity = LogVerbosity.Normal;
            #endif

            LogHelper.StartConsoleLogThread();
            LogHelper.StartFileLogThread();

            RunProgram(args);

            LogHelper.StopFileLogThread();
            LogHelper.StopConsoleLogThread();
        }

        static void WriteVerbosity(LogVerbosity verbosity)
        {
            string output = "";

            switch (verbosity)
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
            LogHelper.ConsoleLog(new Object[] { 
                "CSharpMiner.exe [Options]",
                "",
                "  -config:FilePath [-c]",
                "      Config file to load (Default: config.conf)",
                "",
                "  -modules:DirectoryPath [-m]", 
                "      Directory containing the modules to load. (Default: /bin)",
                "",
                "  -ls", 
                "      Displays a list of all loaded classes in JSON __type property format.",
                "",
                "  -ls:ClassName",
                "      Displays help information about the specified class.",
                "",
                "  -verbosity:Setting [-v]", 
                "      (q)uiet, (n)ormal, (verb)ose, (very)quiet (Default: n)",
                "",
                "  -log:FilePath",
                "      File to write critical errors to. (Default: err.log)",
                "",
                "  -help [-h]",
                "      Display this text"
            }, LogVerbosity.VeryQuiet);
        }
    }
}
