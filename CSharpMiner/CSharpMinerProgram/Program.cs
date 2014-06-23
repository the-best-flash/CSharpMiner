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
using CSharpMiner.ModuleLoading;
using System;
using System.IO;

namespace CSharpMiner
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 1 || args.Length > 3)
            {
                WriteUsage();
                return;
            }

            bool loop = true;

            if (args.Length >= 1 && args[0] == "-ls")
            {
                if (args.Length == 1)
                {
                    ModuleLoader.DisplayKnownTypes();
                }
                else
                {
                    ModuleLoader.DisplayKnownTypeInfo(args[1]);
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

            bool loaded = false; // Make sure that we don't loop indefinately if we can't even get the miner started
            CSharpMiner miner = null;

            do
            {
                try
                {
                    miner = new CSharpMiner();

                    miner.Start(args[0]);

                    loaded = true;

                    ConsoleKey pressedKey;

                    do
                    {
                        pressedKey = Console.ReadKey().Key;

                        if (pressedKey == ConsoleKey.OemPlus || pressedKey == ConsoleKey.Add)
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

                    if(miner != null)
                    {
                        miner.Stop();
                    }
                }
            } while (loop && loaded);

            if(miner != null)
            {
                miner.Stop();
            }

            using (StreamWriter log = new StreamWriter(File.Open("log.log", FileMode.Append)))
            {
                log.WriteLine("Exiting normally at {0}", DateTime.Now);
            }
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
            LogHelper.ConsoleLog("CSharpMiner.exe <Configuration File Path> [LogFilePath] [true|false]", LogVerbosity.VeryQuiet);
        }
    }
}
