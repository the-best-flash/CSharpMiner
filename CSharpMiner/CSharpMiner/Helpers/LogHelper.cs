using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpMiner.Helpers
{
    public enum LogVerbosity
    {
        VeryQuiet = 0,
        Quiet = 1,
        Normal = 2,
        Verbose = 3
    }

    public static class LogHelper
    {
        private static string _secondaryErrorLogPath = "log_secondary.err";
        private static string _errorLogPath = "log.err";
        public static string ErrorLogFilePath
        {
            get
            {
                return _errorLogPath;
            }

            set
            {
                _errorLogPath = value;

                if (_secondaryErrorLogPath.Contains('.'))
                {
                    _secondaryErrorLogPath = value.Insert(value.LastIndexOf('.'), "_secondary");
                }
                else
                {
                    _secondaryErrorLogPath = value + "_secondary";
                }
            }
        }

        private static LogVerbosity _verbosity = LogVerbosity.Normal;
        public static LogVerbosity Verbosity
        {
            get
            {
                return _verbosity;
            }

            set
            {
                _verbosity = value;
            }
        }

        private static Object consoleLock = new Object();

        [Conditional("DEBUG")]
        public static void DebugConsoleLogErrorAsync(Object thing)
        {
            ConsoleLogErrorAsync(thing);
        }

        public static void ConsoleLogErrorAsync(Object thing)
        {
            if (Verbosity != LogVerbosity.VeryQuiet)
            {
                Task.Factory.StartNew(() =>
                {
                    ConsoleLogError(thing);
                });
            }
        }

        [Conditional("DEBUG")]
        public static void DebugConsoleLogError(Object thing)
        {
            ConsoleLogError(thing);
        }

        public static void ConsoleLogError(Object thing)
        {
            ConsoleLog(thing, ConsoleColor.Red, LogVerbosity.Quiet);
        }

        [Conditional("DEBUG")]
        public static void DebugConsoleLogAsync(Object[] things, LogVerbosity verbosity = LogVerbosity.Normal)
        {
            ConsoleLogAsync(things, verbosity);
        }

        public static void ConsoleLogAsync(Object[] things, LogVerbosity verbosity = LogVerbosity.Normal)
        {
            if ((int)Verbosity >= (int)verbosity)
            {
                Task.Factory.StartNew(() =>
                {
                    ConsoleLog(things, verbosity);
                });
            }
        }

        [Conditional("DEBUG")]
        public static void DebugConsoleLog(Object[] things, LogVerbosity verbosity = LogVerbosity.Normal)
        {
            ConsoleLog(things, verbosity);
        }

        public static void ConsoleLog(Object[] things, LogVerbosity verbosity = LogVerbosity.Normal)
        {
            if ((int)Verbosity >= (int)verbosity)
            {
                lock (consoleLock)
                {
                    foreach (Object thing in things)
                    {
                        Object[] thingArray = thing as Object[];

                        if (thingArray != null)
                        {
                            if (thingArray.Length == 3)
                            {
                                if (thingArray[1] is ConsoleColor && thingArray[2] is bool)
                                {
                                    WriteToConsoleLog(thingArray[0], (ConsoleColor)thingArray[1], verbosity, (bool)thingArray[2]);
                                }
                                else
                                {
                                    WriteToConsoleLog(thing, verbosity);
                                }
                            }
                            else if (thingArray.Length == 2)
                            {
                                if (thingArray[1] is bool)
                                {
                                    WriteToConsoleLog(thingArray[0], verbosity, (bool)thingArray[1]);
                                }
                                else if (thingArray[1] is ConsoleColor)
                                {
                                    WriteToConsoleLog(thingArray[0], (ConsoleColor)thingArray[1], verbosity);
                                }
                                else
                                {
                                    WriteToConsoleLog(thing, verbosity);
                                }
                            }
                            else
                            {
                                WriteToConsoleLog(thing, verbosity);
                            }
                        }
                        else
                        {
                            WriteToConsoleLog(thing, verbosity);
                        }
                    }
                }
            }
        }

        [Conditional("DEBUG")]
        public static void DebugConsoleLogAsync(Object thing, ConsoleColor color, LogVerbosity verbosity = LogVerbosity.Normal, bool writeLine = true)
        {
            ConsoleLogAsync(thing, color, verbosity, writeLine);
        }

        public static void ConsoleLogAsync(Object thing, ConsoleColor color, LogVerbosity verbosity = LogVerbosity.Normal, bool writeLine = true)
        {
            if ((int)Verbosity >= (int)verbosity)
            {
                Task.Factory.StartNew(() =>
                {
                    ConsoleLog(thing, color, verbosity, writeLine);
                });
            }
        }

        [Conditional("DEBUG")]
        public static void DebugConsoleLogAsync(Object thing, LogVerbosity verbosity = LogVerbosity.Normal, bool writeLine = true)
        {
            ConsoleLogAsync(thing, verbosity, writeLine);
        }

        public static void ConsoleLogAsync(Object thing, LogVerbosity verbosity = LogVerbosity.Normal, bool writeLine = true)
        {
            if ((int)Verbosity >= (int)verbosity)
            {
                Task.Factory.StartNew(() =>
                {
                    ConsoleLog(thing, verbosity, writeLine);
                });
            }
        }

        [Conditional("DEBUG")]
        public static void DebugConsoleLogAsync(LogVerbosity verbosity = LogVerbosity.Normal)
        {
            ConsoleLogAsync(verbosity);
        }

        public static void ConsoleLogAsync(LogVerbosity verbosity = LogVerbosity.Normal)
        {
            if ((int)Verbosity >= (int)verbosity)
            {
                Task.Factory.StartNew(() =>
                    {
                        ConsoleLog(verbosity);
                    });
            }
        }

        [Conditional("DEBUG")]
        public static void DebugConsoleLog(Object thing, ConsoleColor color, LogVerbosity verbosity = LogVerbosity.Normal, bool writeLine = true)
        {
            ConsoleLog(thing, color, verbosity, writeLine);
        }

        public static void ConsoleLog(Object thing, ConsoleColor color, LogVerbosity verbosity = LogVerbosity.Normal, bool writeLine = true)
        {
            lock (consoleLock)
            {
                WriteToConsoleLog(thing, color, verbosity, writeLine);
            }
        }

        [Conditional("DEBUG")]
        public static void DebugConsoleLog(Object thing, LogVerbosity verbosity = LogVerbosity.Normal, bool writeLine = true)
        {
            ConsoleLog(thing, verbosity, writeLine);
        }

        public static void ConsoleLog(Object thing, LogVerbosity verbosity = LogVerbosity.Normal, bool writeLine = true)
        {
            lock (consoleLock)
            {
                WriteToConsoleLog(thing, verbosity, writeLine);
            }
        }

        [Conditional("DEBUG")]
        public static void DebugConsoleLog(LogVerbosity verbosity = LogVerbosity.Normal)
        {
            ConsoleLog(verbosity);
        }

        public static void ConsoleLog(LogVerbosity verbosity = LogVerbosity.Normal)
        {
            lock(consoleLock)
            {
                Console.WriteLine(verbosity);
            }
        }

        private static void WriteToConsoleLog(Object thing, LogVerbosity verbosity = LogVerbosity.Normal, bool writeLine = true)
        {
            if ((int)Verbosity >= (int)verbosity)
            {
                if (writeLine)
                {
                    Console.WriteLine(thing);
                }
                else
                {
                    Console.Write(thing);
                }
            }
        }

        private static void WriteToConsoleLog(Object thing, ConsoleColor color, LogVerbosity verbosity = LogVerbosity.Normal, bool writeLine = true)
        {
            if ((int)Verbosity >= (int)verbosity)
            {
                Console.ForegroundColor = color;
                WriteToConsoleLog(thing, verbosity, writeLine);
                Console.ResetColor();
            }
        }

        private static Object errorLogLock = new Object();

        [Conditional("DEBUG")]
        public static void DebugLogError(Object error)
        {
            LogError(error);
        }

        public static void LogError(Object error)
        {
            lock (errorLogLock)
            {
                using (StreamWriter errLog = new StreamWriter(File.Open(_errorLogPath, FileMode.Append)))
                {
                    errLog.WriteLine("{0} at {1}.", (error is Exception ? "Exception caught" : "Error Occured"), DateTime.Now);
                    errLog.WriteLine(error);
                    errLog.WriteLine();
                }

                ConsoleLog(string.Format("{0} at {1}.", (error is Exception ? "Exception caught" : "Error Occured"), DateTime.Now));
                ConsoleLog(error);
                ConsoleLog();
            }
        }

        [Conditional("DEBUG")]
        public static void DebugLogErrorSecondary(Object error)
        {
            LogErrorSecondary(error);
        }

        public static void LogErrorSecondary(Object error)
        {
            lock (errorLogLock)
            {
                using (StreamWriter errLog = new StreamWriter(File.Open(_secondaryErrorLogPath, FileMode.Append)))
                {
                    errLog.WriteLine("{0} at {1}.", (error is Exception ? "Exception caught" : "Error Occured"), DateTime.Now);
                    errLog.WriteLine(error);
                    errLog.WriteLine();
                }

                ConsoleLog(string.Format("{0} at {1}.", (error is Exception ? "Exception caught" : "Error Occured"), DateTime.Now));
                ConsoleLog(error);
                ConsoleLog();
            }
        }

        [Conditional("DEBUG")]
        public static void DebugLogErrorAsync(Object error)
        {
            LogErrorAsync(error);
        }

        public static void LogErrorAsync(Object error)
        {
            Task.Factory.StartNew(() =>
            {
                LogError(error);
            });
        }

        [Conditional("DEBUG")]
        public static void DebugLogErrorSecondaryAsync(Object error)
        {
            LogErrorSecondaryAsync(error);
        }

        public static void LogErrorSecondaryAsync(Object error)
        {
            Task.Factory.StartNew(() =>
                {
                    LogErrorSecondary(error);
                });
        }
    }
}
