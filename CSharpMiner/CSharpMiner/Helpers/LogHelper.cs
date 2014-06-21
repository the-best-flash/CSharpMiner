using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpMiner.Helpers
{
    public static class LogHelper
    {
        private static Object consoleLock = new Object();

        [Conditional("DEBUG")]
        public static void DebugConsoleLogErrorAsync(Object thing)
        {
            ConsoleLogErrorAsync(thing);
        }

        public static void ConsoleLogErrorAsync(Object thing)
        {
            Task.Factory.StartNew(() =>
            {
                ConsoleLogError(thing);
            });
        }

        [Conditional("DEBUG")]
        public static void DebugConsoleLogError(Object thing)
        {
            ConsoleLogError(thing);
        }

        public static void ConsoleLogError(Object thing)
        {
            ConsoleLog(thing, ConsoleColor.Red);
        }

        [Conditional("DEBUG")]
        public static void DebugConsoleLogAsync(Object[] things)
        {
            ConsoleLogAsync(things);
        }

        public static void ConsoleLogAsync(Object[] things)
        {
            Task.Factory.StartNew(() =>
            {
                ConsoleLog(things);
            });
        }

        [Conditional("DEBUG")]
        public static void DebugConsoleLog(Object[] things)
        {
            ConsoleLog(things);
        }

        public static void ConsoleLog(Object[] things)
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
                                WriteToConsoleLog(thingArray[0], (ConsoleColor)thingArray[1], (bool)thingArray[2]);
                            }
                            else
                            {
                                WriteToConsoleLog(thing);
                            }
                        }
                        else if (thingArray.Length == 2)
                        {
                            if (thingArray[1] is bool)
                            {
                                WriteToConsoleLog(thingArray[0], (bool)thingArray[1]);
                            }
                            else if (thingArray[1] is ConsoleColor)
                            {
                                WriteToConsoleLog(thingArray[0], (ConsoleColor)thingArray[1]);
                            }
                            else
                            {
                                WriteToConsoleLog(thing);
                            }
                        }
                        else
                        {
                            WriteToConsoleLog(thing);
                        }
                    }
                    else
                    {
                        WriteToConsoleLog(thing);
                    }
                }
            }
        }

        [Conditional("DEBUG")]
        public static void DebugConsoleLogAsync(Object thing, ConsoleColor color, bool writeLine = true)
        {
            ConsoleLogAsync(thing, color, writeLine);
        }

        public static void ConsoleLogAsync(Object thing, ConsoleColor color, bool writeLine = true)
        {
            Task.Factory.StartNew(() =>
            {
                ConsoleLog(thing, color, writeLine);
            });
        }

        [Conditional("DEBUG")]
        public static void DebugConsoleLogAsync(Object thing, bool writeLine = true)
        {
            ConsoleLogAsync(thing, writeLine);
        }

        public static void ConsoleLogAsync(Object thing, bool writeLine = true)
        {
            Task.Factory.StartNew(() =>
            {
                ConsoleLog(thing, writeLine);
            });
        }

        [Conditional("DEBUG")]
        public static void DebugConsoleLogAsync()
        {
            ConsoleLogAsync();
        }

        public static void ConsoleLogAsync()
        {
            Task.Factory.StartNew(() =>
                {
                    ConsoleLog();
                });
        }

        [Conditional("DEBUG")]
        public static void DebugConsoleLog(Object thing, ConsoleColor color, bool writeLine = true)
        {
            ConsoleLog(thing, color, writeLine);
        }

        public static void ConsoleLog(Object thing, ConsoleColor color, bool writeLine = true)
        {
            lock (consoleLock)
            {
                WriteToConsoleLog(thing, color, writeLine);
            }
        }

        [Conditional("DEBUG")]
        public static void DebugConsoleLog(Object thing, bool writeLine = true)
        {
            ConsoleLog(thing, writeLine);
        }

        public static void ConsoleLog(Object thing, bool writeLine = true)
        {
            lock (consoleLock)
            {
                WriteToConsoleLog(thing, writeLine);
            }
        }

        [Conditional("DEBUG")]
        public static void DebugConsoleLog()
        {
            ConsoleLog();
        }

        public static void ConsoleLog()
        {
            lock(consoleLock)
            {
                Console.WriteLine();
            }
        }

        private static void WriteToConsoleLog(Object thing, bool writeLine = true)
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

        private static void WriteToConsoleLog(Object thing, ConsoleColor color, bool writeLine = true)
        {
            Console.ForegroundColor = color;
            WriteToConsoleLog(thing, writeLine);
            Console.ResetColor();
        }

        private static Object errorLogLock = new Object();

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
            }
        }

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
    }
}
