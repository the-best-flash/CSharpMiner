    Copyright (C) 2014 Colton Manville
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
    along with CSharpMiner.  If not, see <http://www.gnu.org/licenses/>.

CSharpMiner
===========

C# (Mono/.NET) crypto-currency mining stratum client for use with various kinds of ASICs.

This is in an Alpha working state. I am using it to mine with some Blizzards on a Raspberry Pi right now, however I have not run it for over 24 hours without stop yet.

You can run this on .NET or <a href="http://www.mono-project.com/Main_Page">Mono</a>. Mono can run binaries compiled for .NET, but it is recommended that you use a binary compiled for Mono. 

I will be adding precompiled binaries when things are a bit more stable, but if you want to compile it yourself you can use the steps below.

To compile for .NET, use <a href="http://www.visualstudio.com/en-us/products/visual-studio-express-vs.aspx">Visual Studio Express 2013 for Desktop</a> on Windows:
    
    1. Load the solution file. *.sln
    2. Change the dropdown that says "Debug" to say "Release"
    3. Select "Build -> Build Solution"
    4. Navigate to the /Bin/Release folder in the same place as the source files

To compile for Mono on linux use:

    1. sudo apt-get update
    2. sudo apt-get install mono-devl
    3. Navigate to the source folder and execute:
       mcs -optimize+ -recurse:*.cs -out:CSharpMiner.exe -r:System,System.Core,System.Data,System.Data.DataSetExtensions,System.Security,System.Runtime.Serialization,System.Xml,System.Xml.Linq

To run the program under Mono type "mono" before your command line parameters. Like so:

    mono CSharpMiner.exe /path/to/config.conf

When constructing the config file you will need to tell the system what type of object to load (With the C# class name) and then provide values for the various settings for thoes objects. Currently, the only way to find the available class names is to look at the code. One of the next features that I will be adding will be the ability to get a list of all the supported class names.

The example.conf file is an example configuration file. It shows how to use shorthand to easily load multiple Blizzards with the same settings, another object that loads some Thunders, and a third object that shows how to specify settings for a single Fury.

Example:

    {
        "managers" : [
        {
            "__type" : "IndividualWorkManager:#DeviceManager",
            "pools" : [
                    {
                        "url" : "stratum+tcp://SomePool.com:3333",
                        "user" : "SomeUsername",
                        "pass" : "SomePassword"
                    }
                ],
            "devices" : [
                    {
                        "__type" : "ZeusDeviceLoader:#DeviceLoader",
                        "ports" : ["/dev/ttyUSB0", "/dev/ttyUSB1", "/dev/ttyUSB2", "/dev/ttyUSB3", "/dev/ttyUSB4", "/dev/ttyUSB5", "COM1" ],
                        "cores" : 6,
                        "clock" : 328
                    },
                    {
                        "__type" : "ZeusDeviceLoader:#DeviceLoader",
                        "ports" : ["/dev/ttyUSB6", "/dev/ttyUSB7", "/dev/ttyUSB8" ],
                        "cores" : 96,
                        "clock" : 328
                    },
                    {
                        "__type" : "ZeusDevice:#MiningDevice",
                        "port" : "/dev/ttyUSB9",
                        "cores" : 6,
                        "clock" : 382
                    }
                ]
            }
        ]
    }

Expect high reject rates (5 - 10%) until I implement a method of verifying the correctness of the nonce before submitting it. However, you will also see higher hash rates on pools that report your total hashrate including rejects. Overall this seems to be getting about the same hashrate as CGminer for my Furys once the reject rate is taken into account.

Command line format is:

    CSharpMiner.exe /path/to/config.conf

Optional paramaters are: the path to output the error log and whether or not to attempt to keep the program running despite errors. For example:

    CSharpMiner.exe /path/to/config.conf /path/to/error.log false

If no error log path is specified it will log to an error file in the current working directory.

If you run into a bug the error log can help me debug it. There will also be an error log with "_secondary" appended to the file name. This contains a log of almost every exception thrown by the program and can be quite large if you ran into a lot of errors. You should only submit this file in a bug report if I need more context. 

I created this as a hobby so I cannot guarantee that I will get around to implementing all of the following features or fixing all of the submitted bugs.

Current Support:

    ZeusMiner ASICs (Should be able to run different kinds of miners at the same time if the configuration file is set up correctly. Fore example you could run Furys and Thunders at the same time)
    GAWMiner Gen1 ASICs (Fury, Black Widdow, etc.) <- These are the exact same as the ZeusMiner ASICs
    Stratum Pool Mining
    Pool Auto-failover
    Pool Auto-reconnect
   
Untested Features:

    ZeusMiner ASICs other than Furys.
    Multiple kinds of ZeusMiner ASICs.
    Multiple Pools
    Pool Auto-failover
    Mining on pools other than Clevermining using Mono. (I have tested a few pools on .NET on windows, but since the default JSON parser works on .NET any pool that has a valid JSON format, which should be all, should work. The default JSON parser does not work on mono for generic objects so I made a temporary parser until I figure out if I want to use a JSON library or make the parser a bit more robust.)
    Disconnecting a miner while the system is running. 

Planned Features:

    A commandline switch to display a list of all of the supported mining devices and mining managers to aid in config file creation
    Importing of 'plugins' or 'modules' that can be used to add support for new mining devices or work scheduling algorithms
    Importing of 'plugins' or 'modules' that can be used to add supprot for new pool protocols and new pool management features
    Support for hotplugging ASICs
    Gridseed support
    Avalon BTC support
    Local work verification (right now it submits shares even if they are incorrect)
    Exposing device stats through properties (Right now they may be difficlut to get from the mining manager class)
    An ASP.NET wrapper that will expose an HTTP interface for queying mining stats using HTTP requests
    An ASP.NET wrapper that will expose an HTTP interface for changing settings
    
