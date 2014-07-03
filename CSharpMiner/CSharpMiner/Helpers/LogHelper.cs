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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
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
        private static BlockingCollection<Tuple<Object, Nullable<ConsoleColor>, LogVerbosity, bool>> _consoleLogQueue;
        private static BlockingCollection<Tuple<Object, Nullable<ConsoleColor>, LogVerbosity, bool>> ConsoleLogQueue
        {
            get
            {
                if (_consoleLogQueue == null)
                    _consoleLogQueue = new BlockingCollection<Tuple<object, Nullable<ConsoleColor>, LogVerbosity, bool>>();

                return _consoleLogQueue;
            }
        }

        private static BlockingCollection<Tuple<Object, string, bool>> _fileLogQueue;
        private static BlockingCollection<Tuple<Object, string, bool>> FileLogQueue
        {
            get
            {
                if (_fileLogQueue == null)
                    _fileLogQueue = new BlockingCollection<Tuple<Object, string, bool>>();

                return _fileLogQueue;
            }
        }

        private static Thread _consoleLoggingThread;
        private static Thread ConsoleLoggingThread
        {
            get
            {
                if (_consoleLoggingThread == null)
                    _consoleLoggingThread = new Thread(new ThreadStart(ProcessConsoleLogItem));

                return _consoleLoggingThread;
            }
        }

        private static Thread _fileLoggingThread;
        private static Thread FileLoggingThread
        {
            get
            {
                if (_fileLoggingThread == null)
                    _fileLoggingThread = new Thread(new ThreadStart(ProcessFileLogItem));

                return _fileLoggingThread;
            }
        }

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

        private static bool _fileLogEnabled;
        private static bool _consoleLogEnabled;

        public static bool ShouldDisplay(LogVerbosity verbosity)
        {
            return ((int)verbosity <= (int)Verbosity);
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
        public static void DebugConsoleLog(Object[] things, LogVerbosity verbosity = LogVerbosity.Normal)
        {
            ConsoleLog(things, verbosity);
        }

        public static void ConsoleLog(Object[] things, LogVerbosity verbosity = LogVerbosity.Normal)
        {
            AddToConsoleLogQueue(things, null, verbosity);
        }

        [Conditional("DEBUG")]
        public static void DebugConsoleLog(Object thing, ConsoleColor color, LogVerbosity verbosity = LogVerbosity.Normal, bool writeLine = true)
        {
            ConsoleLog(thing, color, verbosity, writeLine);
        }

        public static void ConsoleLog(Object thing, ConsoleColor color, LogVerbosity verbosity = LogVerbosity.Normal, bool writeLine = true)
        {
            AddToConsoleLogQueue(thing, color, verbosity, writeLine);
        }

        [Conditional("DEBUG")]
        public static void DebugConsoleLog(Object thing, LogVerbosity verbosity = LogVerbosity.Normal, bool writeLine = true)
        {
            ConsoleLog(thing, verbosity, writeLine);
        }

        public static void ConsoleLog(Object thing, LogVerbosity verbosity = LogVerbosity.Normal, bool writeLine = true)
        {
            AddToConsoleLogQueue(thing, null, verbosity, writeLine);
        }

        [Conditional("DEBUG")]
        public static void DebugConsoleLog(LogVerbosity verbosity = LogVerbosity.Normal)
        {
            ConsoleLog(verbosity);
        }

        public static void ConsoleLog(LogVerbosity verbosity = LogVerbosity.Normal)
        {
            AddToConsoleLogQueue(string.Empty);
        }

        private static void AddToConsoleLogQueue(Object thing, Nullable<ConsoleColor> color = null, LogVerbosity verbosity = LogVerbosity.Normal, bool writeLine = true)
        {
            if (_consoleLogEnabled && ShouldDisplay(verbosity))
            {
                ConsoleLogQueue.Add(new Tuple<object, Nullable<ConsoleColor>, LogVerbosity, bool>(thing, color, verbosity, writeLine));
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

        private static void ProcessConsoleLogItem()
        {
            foreach (var item in ConsoleLogQueue.GetConsumingEnumerable())
            {
                Object thing = item.Item1;
                Nullable<ConsoleColor> color = item.Item2;
                LogVerbosity verbosity = item.Item3;
                bool writeLine = item.Item4;

                if(color != null && color.HasValue)
                {
                    WriteToConsoleLog(thing, color.Value, verbosity, writeLine);
                }
                else
                {
                    if(thing is Object[])
                    {
                        WriteToConsoleLog(thing as object[], verbosity);
                    }
                    else
                    {
                        WriteToConsoleLog(thing, verbosity, writeLine);
                    }
                }
            }
        }

        public static void StartConsoleLogThread()
        {
            ConsoleLoggingThread.Priority = ThreadPriority.Lowest;
            ConsoleLoggingThread.Start();
            _consoleLogEnabled = true;
        }

        public static void StopConsoleLogThread()
        {
            ConsoleLogQueue.CompleteAdding();
            ConsoleLoggingThread.Join(500);
            ConsoleLoggingThread.Abort();
            _consoleLogEnabled = false;
        }

        [Conditional("DEBUG")]
        public static void DebugLogError(Object error)
        {
            LogError(error);
        }

        [Conditional("DEBUG")]
        public static void DebugLogErrors(Object[] errors)
        {
            LogErrors(errors);
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

        [Conditional("DEBUG")]
        public static void DebugLogToFile(Object obj, string filename, bool displayToScreen = false)
        {
            if (_fileLogEnabled)
            {
                FileLogQueue.Add(new Tuple<object, string, bool>(obj, filename, displayToScreen));
            }
        }

        [Conditional("DEBUG")]
        public static void DebugLogToFileAsync(Object[] objects, string filename, bool displayToScreen = false)
        {
            if (_fileLogEnabled)
            {
                FileLogQueue.Add(new Tuple<object, string, bool>(objects, filename, displayToScreen));
            }
        }

        public static void LogError(Object error)
        {
            if (_fileLogEnabled)
            {
                FileLogQueue.Add(new Tuple<object, string, bool>(error, _errorLogPath, true));
            }
        }

        public static void LogErrors(Object[] errors)
        {
            if (_fileLogEnabled)
            {
                FileLogQueue.Add(new Tuple<object, string, bool>(errors, _errorLogPath, true));
            }
        }

        public static void LogErrorSecondary(Object error, bool displayToScreen = false)
        {
            if (_fileLogEnabled)
            {
                FileLogQueue.Add(new Tuple<object, string, bool>(error, _secondaryErrorLogPath, displayToScreen));
            }
        }

        public static void LogErrorSecondary(Object[] errors, bool displayToScreen = false)
        {
            if (_fileLogEnabled)
            {
                FileLogQueue.Add(new Tuple<object, string, bool>(errors, _secondaryErrorLogPath, displayToScreen));
            }
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

            try
            {
                using (StreamWriter errLog = new StreamWriter(File.Open(filePath, FileMode.Append)))
                {
                    errLog.WriteLine("{0} at {1}.", (error is Exception ? "Exception caught" : "Error Occured"), DateTime.Now);
                    errLog.WriteLine(error);
                    errLog.WriteLine();
                }
            }
            catch (Exception e)
            {
                LogErrors(new Object[] {
                                string.Format("Could not write to file {0}:", filePath),
                                e
                            });
            }
        }

        private static void LogErrorsToFile(Object[] errors, string filePath, bool displayToScreen)
        {
            if (errors.Length > 0)
            {
                try
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
                catch (Exception e)
                {
                    LogErrors(new Object[] {
                                    string.Format("Could not write to file {0}:", filePath),
                                    e
                                });
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

        private static void ProcessFileLogItem()
        {
            foreach (var item in FileLogQueue.GetConsumingEnumerable())
            {
                Object thing = item.Item1;
                string filename = item.Item2;
                bool displayOnScreen = item.Item3;

                if(thing is object[])
                {
                    LogErrorsToFile(thing as object[], filename, displayOnScreen);
                }
                else
                {
                    LogErrorToFile(thing, filename, displayOnScreen);
                }
            }
        }

        public static void StartFileLogThread()
        {
            FileLoggingThread.Priority = ThreadPriority.Lowest;
            FileLoggingThread.Start();
            _fileLogEnabled = true;
        }

        public static void StopFileLogThread()
        {
            FileLogQueue.CompleteAdding();
            FileLoggingThread.Join(500);
            FileLoggingThread.Abort();
            _fileLogEnabled = false;
        }
    }
}
