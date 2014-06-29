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

A full description of why I made this and what benefits I hope it will have are at the end of this file. In short: it is portable and easy to compile/install, it supports mining with different kind of ASICs at the same time and can support mining with ASICs for SHA256 (BTC) or Scrypt (LTC) at the same time, and support for more ASICs can be added by copying a 'plugin' to a folder.

This is in an Alpha, working state. I have tried it breifly on a few of the most popular LTC mining sites and it worked. I am using it to mine with some Blizzards on a Raspberry Pi right now to test it during a longer session. If everything checks out I might be ready for the first beta release before too long.


This should work on Windows, Linux, and OSX. You can run this on .NET or <a href="http://www.mono-project.com/Main_Page">Mono</a>. Mono can run binaries compiled for .NET, but it is recommended that you use a binary compiled for Mono. I have verified that I can run this on Windows and the Debian Linux on my raspberry PI.

Compilation
===

I will be adding precompiled binaries when things are a bit more stable, but if you want to compile it yourself you can use the steps below.

To compile for .NET, use <a href="http://www.visualstudio.com/en-us/products/visual-studio-express-vs.aspx">Visual Studio Express 2013 for Desktop</a> on Windows:
    
    1. Load the solution file. *.sln
    2. Change the dropdown that says "Debug" to say "Release"
    3. Select "Build -> Build Solution"
    4. Navigate to the CSharpMinerProgram\bin\Release folder and copy the .exe, and .dll files to wherever you want to run the program from. (ex. C:\CSMiner)
    5. Make a \bin folder wherever you put the .exe (ex. C:\CSMiner\bin)
    6. Copy the .dll files from CSharpMinerProgram\bin\Release\bin to the \bin folder you just made.
    7. Create a "config.conf" file in the same folder as the .exe (ex. C:\CSMiner\bin) you can follow the example further down.
    7. Right click on the .exe and make a shortcut. Copy the shortcut to your desktop.


To compile for Mono on linux use:

    1. sudo apt-get update
    2. sudo apt-get install mono-devel
    3. Navigate to the source folder and execute the MonoCompile.sh script
    4. Make a script to run:
        mono CSharpMiner.exe -m:/home/username/GitRepo/bin -c:/home/username/config.conf
    5. Change the path after -m to be the path to the /bin folder that was created in the directory that you compiled the executable in.
    6. Create a config file and change the path after -c to be the path to that file. You can follow the example further down.

To run the program under Mono type "mono" before your command line parameters. Like so:

    mono CSharpMiner.exe -c:/path/to/config.conf 

Commands
===

While running the program you can use the following keyboard commands:

    q - Quit
    + - Increase console logging verbosity (Makes it display more info if you've decreased it.) Note: You have to hold shift if you're using the plus key near backspace on a USA keyboard. I haven't gotten around to making it accept equals too.)
    - - Decrease console logging verbosity (Makes it display less info the more you press it.) Note: This is the minus key.

When starting the program you can specify the following command line parameters. (You can see the list by running with "-help")

    CSharpMiner.exe [Options]
    -config:FilePath [-c]
        Config file to load (Default: config.conf)
    -modules:DirectoryPath [-m]
        Directory containing the modules to load. (Default: /bin)
    -ls
        Displays a list of all loaded classes in JSON __type property format.
    -ls:ClassName (ex. -ls:ZeusDevice)
        Displays help information about the specified class.
    -verbosity:Setting [-v]
        (q)uiet, (n)ormal, (verb)ose, (very)quiet (Default: n)
    -log:FilePath
        File to write critical errors to. (Default: err.log)
    -help [-h]
        Display this text

Making a Config File
====

When creating the config file you will need to tell the system what type of object to load (With the C# class name) and then provide values for the various settings for thoes objects. 

To aid in this you can use the "-ls" command to display a list of all the valid classes in your current plugins.

For example:

    > CSharpMiner.exe -ls
    Devices:
        ZeusDevice:#ZeusMiner
    Device Loaders:
        ZeusDeviceLoader:#ZeusMiner
    Device Managers:
        DistributedWorkManager:#StratumManager
        IndividualWorkManager:#StratumManager
    Pool Managers:
        StratumPool:#Stratum
    Others:
        StratumRecieveCommand:#Stratum
        StratumResponse:#Stratum
        StratumSendCommand:#Stratum
        JsonConfiguration:#CSharpMiner.Configuration

For information about a type and an example of how to use it, type the class name after the -ls command. For example:

    > CSharpMiner.exe -ls:ZeusDevice
        ZeusDevice:#ZeusMiner
            Description:
                Configures a ZeusMiner Gen1 or GAWMiner A1 device.
            cores : Int32
                Number of ZeusChips in the device.
            clock : Int32 (Optional)
                The clockspeed of the miner. Max = 382
            port : String
                The port the device is connected to. Linux /dev/tty* and Windows COM*
            poll : Int32 (Optional)
                Milliseconds the thread waits before looking for incoming data. A larger value will decrease the processor usage but shares won't be submitted right away.
            timeout : Int32 (Optional)
                Number of seconds to wait without response before restarting the device.
        Example JSON Format:
        {
            "__type" : "ZeusDevice:#ZeusMiner",
            "cores" : 6,
            "clock" : 328,
            "port" : "dev/ttyUSB0",
            "poll" : 50,
            "timeout" : 60
        }    

Plugin developers can specify help information using assembly metadata, however if they don't the program will gather as much info about the type as possible and do its best to generate a JSON example.

The example.conf file in the repository is an example configuration file. It shows how to use shorthand to easily load multiple Blizzards with the same settings, another object that loads some Thunders, and a third object that shows how to specify settings for a single Fury.

Example:

    {
        "managers" : [
        {
            "__type" : "IndividualWorkManager:#StratumManager",
            "pools" : [
                    {
                        "url" : "stratum+tcp://SomePool.com:3333",
                        "user" : "SomeUsername",
                        "pass" : "SomePassword"
                    }
                ],
            "devices" : [
                    {
                        "__type" : "ZeusDeviceLoader:#ZeusMiner",
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
                        "__type" : "ZeusDevice:#ZeusMiner",
                        "port" : "/dev/ttyUSB9",
                        "cores" : 6,
                        "clock" : 382
                    }
                ]
            }
        ]
    }

I didn't implement a method of verifying the correctness of a nonce before submitting it. As a result all hardware errors appear as work reject messages. However, they are tracked in the statistics as hardware erros and are shown in Magenta. 

Expect high reject rates (5 - 10%) until I implement a method of verifying the correctness of the nonce before submitting it. However, you will also see higher hash rates on pools that report your total hashrate including rejects. Overall this seems to be getting about the same hashrate as other mining programs for my Blizzards once the reject rate is taken into account.

Error Logging
===

With the "-log" parameter you can control where the program logs errors. By default, it will log to err.log in the current working directory.

If you run into a bug the error log can help me debug it. There will also be an error log with "_secondary" appended to the file name. (ex. err._secondary.log) This contains a log of almost every exception thrown by the program and can be quite large if you run into a lot of errors. You should only submit this file in a bug report if I need more context. 

Current/Planned Features
===

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
    Implement stratum reconnection protocol to avoid losing shares during temporary disconnects.
    Implement stratum suggest difficluty command for suggesting a difficluty on startup.

Known Issues
===
    
Known Issues:

    Pool auto failover not working
    Pool restarting not working

Why make this? 
===

Currently every scrypt asic company seems to have their own version of open source mining programs that users need to download to use. As a result users need to have one mining program per ASIC type. This software aims to make the creation of 'plugins' or 'modules' simple. So companies or the mining community can make a 'plugin' to support new ASICs and the end user can simply place the 'plugin' into the specified folder and their mining program can support the new ASIC without needing to recompile.
    
This project also aims to create one mining program that can control a large number of different kinds of ASICs for different algorithms at once. The current plan is to support Scrypt and SHA265 mining and to provide support for 'plugins' to handle other algorithms. Ideally this would result in one program that can control any ASIC for any algorithm, and could be extended and customized by the community without requiring a full recompile of the code. 
    
Another goal of this project was to create a mining program that could be run behind a RESTful HTTP service that would allow web interfaces to get mining statistics and make configuration changes via HTTP requests. This would allow the statistic analysis and logging code to be done from a device separate from the one that is running the mining software. If a Raspberry PI was used, this would provide a benefit by decreasing the overhead of using a web API.
    
The final goal of this project was to create a mining program written in an Object Oriented language that was easy to modify and compile. Part of this goal is to attempt to make the program without using any libraries outside of the libraries provided as part of the .NET and Mono runtime.

Also, I like programming things in my spare time and I had some mining ASICs. As a result it made sense to write a ASIC mining interface.

Donation
===
If you wish to donate you can donate to the following addresses:

    1DguxkZenDbFY2xMSrYuJBiuYteH6vQTCu (BTC)
    LX68osgfBkDk3r3tS7hYi8u2cVU2omZc2f (LTC)
    DAF4pCnyiMVrkAGkmnxk4fzKS2mNjsDpD4 (DOGE)
