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

A full description of why I made this and what benefits I hope it will have are at the end of this file. In short: it supports mining with different kind of ASICs at the same time and can support mining with ASICs for SHA256 (BTC) or Scrypt (LTC) at the same time, and support for more ASICs can be added by copying a 'plugin' to a folder.

This is in an Alpha, working state. I have tried it breifly on a few of the most popular LTC mining sites and it worked. I am using it to mine with some Blizzards on a Raspberry Pi right now to test it during a longer session. If everything checks out I might be ready for the first beta release before too long.

Note: Most of this is out of date. I've modified the program to be almost completely modular and have implemented the module loading code. I'm in the process of testing it overnight and may not get to updating the readme for a bit.

You can run this on .NET or <a href="http://www.mono-project.com/Main_Page">Mono</a>. Mono can run binaries compiled for .NET, but it is recommended that you use a binary compiled for Mono.

If you wish to donate you can donate to the following addresses:

    1DguxkZenDbFY2xMSrYuJBiuYteH6vQTCu (BTC)
    LX68osgfBkDk3r3tS7hYi8u2cVU2omZc2f (LTC)
    DAF4pCnyiMVrkAGkmnxk4fzKS2mNjsDpD4 (DOGE)

I will be adding precompiled binaries when things are a bit more stable, but if you want to compile it yourself you can use the steps below.


To compile for .NET, use <a href="http://www.visualstudio.com/en-us/products/visual-studio-express-vs.aspx">Visual Studio Express 2013 for Desktop</a> on Windows:
    
    1. Load the solution file. *.sln
    2. Change the dropdown that says "Debug" to say "Release"
    3. Select "Build -> Build Solution"
    4. Navigate to the /Bin/Release folder in the same place as the source files

To compile for Mono on linux use:

    1. sudo apt-get update
    2. sudo apt-get install mono-devel
    3. Navigate to the source folder and execute:
       mcs -optimize+ -recurse:*.cs -out:CSharpMiner.exe -r:System,System.Core,System.Data,System.Data.DataSetExtensions,System.Security,System.Runtime.Serialization,System.Xml,System.Xml.Linq

To run the program under Mono type "mono" before your command line parameters. Like so:

    mono CSharpMiner.exe /path/to/config.conf

While running the program you can use the following keyboard commands:

    q - Quit
    + - Increase console logging verbosity (Makes it display more info if you've decreased it.) Note: You have to hold shift if you're using the plus key near backspace on a USA keyboard. I haven't gotten around to making it accept equals too.)
    - - Decrease console logging verbosity (Makes it display less info the more you press it.) Note: This is the minus key.

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
    A commandline switch to display a list of all of the supported mining devices and mining managers to aid in config file creation
    Importing of 'plugins' or 'modules' that can be used to add support for new mining devices or work scheduling algorithms
    Importing of 'plugins' or 'modules' that can be used to add supprot for new pool protocols and new pool management features
   
Untested Features:

    ZeusMiner ASICs other than Furys.
    Multiple kinds of ZeusMiner ASICs.
    Multiple Pools
    Pool Auto-failover
    Mining on pools other than Clevermining using Mono. (I have tested a few pools on .NET on windows, but since the default JSON parser works on .NET any pool that has a valid JSON format, which should be all, should work. The default JSON parser does not work on mono for generic objects so I made a temporary parser until I figure out if I want to use a JSON library or make the parser a bit more robust.)
    Disconnecting a miner while the system is running. 

Planned Features:

    Support for hotplugging ASICs
    Gridseed support
    Avalon BTC support
    Local work verification (right now it submits shares even if they are incorrect)
    An ASP.NET wrapper that will expose an HTTP interface for queying mining stats using HTTP requests
    An ASP.NET wrapper that will expose an HTTP interface for changing settings
    Ability to connect both SHA265 and Scrypt ASICs at once.
    Add a way for modules to specify dependencies so that the program can avoid loading them if they won't run. This will also allow users to be notified of any missing dependencies.
    
Known Issues:

    Possible strange, infinate loop of pool connection attempts after a pool failure.
    May not attempt to reconnect to a pool of the connection is lost.
    Does not work on all stratum pools when running on Mono. Need to make the fallback parser order independant.

Why make this? 

Currently every scrypt asic company seems to have their own version of open source mining programs that users need to download to use. As a result users need to have one mining program per ASIC type. This software aims to make the creation of 'plugins' or 'modules' simple. So companies or the mining community can make a 'plugin' to support new ASICs and the end user can simply place the 'plugin' into the specified folder and their mining program can support the new ASIC without needing to recompile.
    
This project also aims to create one mining program that can control a large number of different kinds of ASICs for different algorithms at once. The current plan is to support Scrypt and SHA265 mining and to provide support for 'plugins' to handle other algorithms. Ideally this would result in one program that can control any ASIC for any algorithm, and could be extended and customized by the community without requiring a full recompile of the code. 
    
Another goal of this project was to create a mining program that could be run behind a RESTful HTTP service that would allow web interfaces to get mining statistics and make configuration changes via HTTP requests. This would allow the statistic analysis and logging code to be done from a device separate from the one that is running the mining software. If a Raspberry PI was used, this would provide a benefit by decreasing the overhead of using a web API.
    
The final goal of this project was to create a mining program written in an Object Oriented language that was easy to modify and compile. Part of this goal is to attempt to make the program without using any libraries outside of the libraries provided as part of the .NET and Mono runtime.

Also, I like programming things in my spare time and I had some mining ASICs. As a result it made sense to write a ASIC mining interface.
