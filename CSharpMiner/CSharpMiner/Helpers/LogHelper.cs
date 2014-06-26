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

        public static bool ShouldDisplay(LogVerbosity verbosity)
        {
            return ((int)verbosity <= (int)Verbosity);
        }

        [Conditional("DEBUG")]
        public static void DebugConsoleLogErrorAsync(Object thing)
        {
            ConsoleLogErrorAsync(thing);
        }

        public static void ConsoleLogErrorAsync(Object thing)
        {
            if (ShouldDisplay(LogVerbosity.Quiet))
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
            if (ShouldDisplay(verbosity))
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
            if (ShouldDisplay(verbosity))
            {
                lock (consoleLock)
                {
                    WriteToConsoleLog(things, verbosity);
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
            if (ShouldDisplay(verbosity))
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
            if (ShouldDisplay(verbosity))
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
            if (ShouldDisplay(verbosity))
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
                if (ShouldDisplay(verbosity))
                {
                    Console.WriteLine();
                }
            }
        }

        private static void WriteToConsoleLog(Object thing, LogVerbosity verbosity = LogVerbosity.Normal, bool writeLine = true)
        {
            if (ShouldDisplay(verbosity))
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

        public static void WriteToConsoleLog(Object[] things, LogVerbosity verbosity = LogVerbosity.Normal)
        {
            if (ShouldDisplay(verbosity))
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
                                WriteToConsoleLog(thingArray, verbosity);
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
                                WriteToConsoleLog(thingArray, verbosity);
                            }
                        }
                        else
                        {
                            WriteToConsoleLog(thingArray, verbosity);
                        }
                    }
                    else
                    {
                        WriteToConsoleLog(thing, verbosity);
                    }
                }
            }
        }

        private static void WriteToConsoleLog(Object thing, ConsoleColor color, LogVerbosity verbosity = LogVerbosity.Normal, bool writeLine = true)
        {
            if (ShouldDisplay(verbosity))
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
            LogErrorToFile(error, _errorLogPath, true);
        }

        private static void LogErrorToFile(Object error, string filePath, bool displayToScreen)
        {
            if (displayToScreen)
            {
                ConsoleLog(new Object[] {
                    string.Format("{0} at {1}.", (error is Exception ? "Exception caught" : "Error Occured"), DateTime.Now),
                    error,
                    ""
                });
            }

            lock (errorLogLock)
            {
                using (StreamWriter errLog = new StreamWriter(File.Open(filePath, FileMode.Append)))
                {
                    errLog.WriteLine("{0} at {1}.", (error is Exception ? "Exception caught" : "Error Occured"), DateTime.Now);
                    errLog.WriteLine(error);
                    errLog.WriteLine();
                }
            }           
        }

        private static void LogErrorsToFile(Object[] errors, string filePath, bool displayToScreen)
        {
            if (errors.Length > 0)
            {
                lock (errorLogLock)
                {
                    using (StreamWriter errLog = new StreamWriter(File.Open(filePath, FileMode.Append)))
                    {
                        errLog.WriteLine("{0} at {1}.", (errors[0] is Exception ? "Exception caught" : "Error Occured"), DateTime.Now);

                        foreach (object error in errors)
                        {
                            errLog.WriteLine(error);
                        }

                        errLog.WriteLine();
                    }
                }

                if (displayToScreen)
                {
                    List<Object> obj = new List<object>();

                    obj.Add(string.Format("{0} at {1}.", (errors[0] is Exception ? "Exception caught" : "Error Occured"), DateTime.Now));

                    foreach (object error in errors)
                    {
                        obj.Add(error);
                    }

                    obj.Add("");

                    ConsoleLog(obj.ToArray());
                }
            }
        }

        [Conditional("DEBUG")]
        public static void DebugLogErrorSecondary(Object error)
        {
            LogErrorSecondary(error, true);
        }

        [Conditional("DEBUG")]
        public static void DebugLogErrorSecondary(Object[] errors)
        {
            LogErrorSecondary(errors, true);
        }

        public static void LogErrorSecondary(Object error, bool displayToScreen = false)
        {
            LogErrorToFile(error, _secondaryErrorLogPath, false);
        }

        public static void LogErrorSecondary(Object[] errors, bool displayToScreen = false)
        {
            LogErrorsToFile(errors, _secondaryErrorLogPath, false);
        }

        [Conditional("DEBUG")]
        public static void DebugLogErrorAsync(Object error)
        {
            LogErrorAsync(error);
        }

        [Conditional("DEBUG")]
        public static void DebugLogErrorAsync(Object[] errors)
        {
            LogErrorAsync(errors);
        }

        public static void LogErrorAsync(Object error)
        {
            Task.Factory.StartNew(() =>
            {
                LogError(error);
            });
        }

        public static void LogErrorAsync(Object[] errors)
        {
            Task.Factory.StartNew(() =>
            {
                LogError(errors);
            });
        }

        [Conditional("DEBUG")]
        public static void DebugLogErrorSecondaryAsync(Object error)
        {
            LogErrorSecondaryAsync(error);
        }

        [Conditional("DEBUG")]
        public static void DebugLogErrorSecondaryAsync(Object[] errors)
        {
            LogErrorSecondaryAsync(errors);
        }

        public static void LogErrorSecondaryAsync(Object error)
        {
            Task.Factory.StartNew(() =>
                {
                    LogErrorSecondary(error);
                });
        }

        public static void LogErrorSecondaryAsync(Object[] errors)
        {
            Task.Factory.StartNew(() =>
            {
                LogErrorSecondary(errors);
            });
        }

        private static Dictionary<string, Object> fileLocks = new Dictionary<string, object>();

        private static Object GetFileLockObject(string filename)
        {
            Object lockObj = null;

            if (!fileLocks.TryGetValue(filename, out lockObj))
            {
                lockObj = new Object();
                fileLocks.Add(filename, lockObj);
            }

            return lockObj;
        }

        [Conditional("DEBUG")]
        public static void DebugLogToFileAsync(Object obj, string filename)
        {
            Task.Factory.StartNew(() =>
                {
                    lock (GetFileLockObject(filename))
                    {
                        try
                        {
                            using (StreamWriter writer = new StreamWriter(File.Open(filename, FileMode.Append)))
                            {
                                writer.WriteLine(string.Format("{0}: {1}", DateTime.Now, obj));
                            }
                        }
                        catch (Exception e)
                        {
                            LogError(new Object[] {
                                string.Format("Could not write to file {0}:", filename),
                                e
                            });
                        }
                    }
                });
        }

        [Conditional("DEBUG")]
        public static void DebugLogToFileAsync(Object[] objects, string filename)
        {
            Task.Factory.StartNew(() =>
                {
                    lock (GetFileLockObject(filename))
                    {
                        try
                        {
                            bool first = true;

                            using (StreamWriter writer = new StreamWriter(File.Open(filename, FileMode.Append)))
                            {
                                foreach (Object obj in objects)
                                {
                                    if (first)
                                    {
                                        writer.WriteLine(string.Format("{0}: {1}", DateTime.Now, obj));
                                        first = false;
                                    }
                                    else
                                    {
                                        writer.WriteLine(obj);
                                    }
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            LogError(new Object[] {
                                string.Format("Could not write to file {0}:", filename),
                                e
                            });
                        }
                    }
                });
        }
    }
}
