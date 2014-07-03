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

            ConsoleMiner.RunFromConsole(args);

            LogHelper.StopFileLogThread();
            LogHelper.StopConsoleLogThread();
        }
    }
}
