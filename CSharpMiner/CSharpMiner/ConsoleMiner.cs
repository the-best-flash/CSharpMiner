using CSharpMiner.Helpers;
using CSharpMiner.ModuleLoading;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CSharpMiner
{
    public static class ConsoleMiner
    {
        public static CancellationTokenSource _stopToken = new CancellationTokenSource(); 
        public static bool _stopMining;
        public static bool StopMining
        {
            get
            {
                return _stopMining;
            }
            set
            {
                _stopMining = value;

                if (_stopMining)
                    _stopToken.Cancel();
            }
        }

        private static Miner _miner;
        public static Miner Miner
        {
            get
            {
                if (_miner == null)
                    _miner = new Miner();

                return _miner;
            }
        }

        static void LogCompositeParamError(string param)
        {
            LogHelper.ConsoleLogError(string.Format("Incorrect param format for {0}", param));
            WriteUsage();
        }

        public static void RunFromConsole(string[] args)
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
                    miner = Miner;

                    miner.Start(configFilePath);

                    loaded = true;

                    ConsoleKey pressedKey;

                    do
                    {
                        Thread.Sleep(250);

                        pressedKey = ConsoleKey.Q;

                        Task t = Task.Factory.StartNew(() =>
                            {
                                pressedKey = Console.ReadKey(true).Key;
                            });
                        t.Wait(_stopToken.Token);

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

                    } while (pressedKey != ConsoleKey.Q && !StopMining);

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
            } while (loop && loaded && !StopMining);

            if (miner != null)
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
