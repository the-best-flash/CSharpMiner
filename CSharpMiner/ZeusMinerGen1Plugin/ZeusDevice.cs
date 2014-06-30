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
using CSharpMiner.Interfaces;
using CSharpMiner.MiningDevice;
using CSharpMiner.ModuleLoading;
using System;
using System.IO.Ports;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using ZeusMinerGen1Plugin;

namespace ZeusMiner
{
    [DataContract]
    [MiningModule(Description = "Configures a ZeusMiner Gen1 or GAWMiner A1 device.")]
    class ZeusDevice : UsbMinerBase, IZeusDeviceSettings
    {
        private const int extraDataThreshold = 2; // Number of times through the main USB reading look that we will allow extra data to sit int he buffer
        private const string deviceLogFile = "device.log";

        internal const string coresJsonName = "cores";
        internal const string coresExampleString = "6";
        internal const string coresDescription = "Number of ZeusChips in the device.";

        internal const string ltcClkJsonName = "clock";
        internal const int ltcClkDefaultValue = 328;
        internal const string ltcClkExampleString = "328";
        internal const string ltcClkDescription = "The clockspeed of the miner. Max = 382";

        private int _cores = 1;
        [DataMember(Name = coresJsonName)]
        [MiningSetting(ExampleValue = coresExampleString, Optional = false, Description = coresDescription)]
        public int Cores
        {
            get
            {
                return _cores;
            }
            set
            {
                _cores = value;

                HashRate = this.GetExpectedHashrate();
            }
        }

        private int _clk;
        [DataMember(Name = ltcClkJsonName)]
        [MiningSetting(ExampleValue = ltcClkExampleString, Description = ltcClkDescription, Optional = true)]
        public int LtcClk
        {
            get
            {
                if (_clk == 0)
                    _clk = ltcClkDefaultValue;

                return _clk;
            }

            set
            {
                if (value > 382)
                    _clk = 382;
                else if (value < 2)
                    _clk = 2;
                else
                    _clk = value;

                byte _freqCode = (byte)(_clk * 2 / 3);

                byte[] cmd = _commandPacket;
                cmd[0] = _freqCode;
                cmd[1] = (byte)(0xFF - _freqCode);

                HashRate = this.GetExpectedHashrate();
            }
        }

        [IgnoreDataMember]
        public override string Name
        {
            get { return this.Port; }
        }

        [IgnoreDataMember]
        public override int BaudRate
        {
            get
            {
                return 115200;
            }
        }

        private byte[] _eventPacket = null;
        private byte[] _commandPacket = null;
        private IPoolWork currentWork = null;
        private int timesNonZero = 0;

        protected override void OnDeserializing()
        {
            SetUpDefaultValues();

            base.OnDeserializing();
        }

        private void SetUpDefaultValues()
        {
            _commandPacket = new byte[84];
            _eventPacket = new byte[4];
        }

        public ZeusDevice(string port, int clk, int cores, int watchdogTimeout = defaultWatchdogTimeout, int pollFrequency = defaultPollTime)
            : base(port, watchdogTimeout, pollFrequency)
        {
            SetUpDefaultValues();

            LtcClk = clk;
            Cores = cores;
        }

        public override void StartWork(IPoolWork work)
        {
            SendWork(work);
        }

        public override void StartWork(IPoolWork work, long startingNonce, long endingNonce)
        {
            SendWork(work, startingNonce);
        }

        private void SendWork(IPoolWork work, long startingNonce = 0)
        {
            try
            {
                if (work != null)
                {
                    timesNonZero = 0;
                    this.RestartWatchdogTimer();

                    if (this.usbPort != null && this.usbPort.IsOpen)
                    {
                        if (LogHelper.ShouldDisplay(LogVerbosity.Verbose))
                        {
                            LogHelper.ConsoleLogAsync(string.Format("Device {0} starting work {1}.", this.Name, work.JobId), LogVerbosity.Verbose);
                        }

                        this.RestartWorkRequestTimer();

                        int diffCode = 0xFFFF / work.Diff;
                        byte[] cmd = _commandPacket;

                        cmd[3] = (byte)diffCode;
                        cmd[2] = (byte)(diffCode >> 8);

                        int offset = 4;

                        // Starting nonce
                        cmd[offset] = (byte)startingNonce;
                        cmd[offset + 1] = (byte)(startingNonce >> 8);
                        cmd[offset + 2] = (byte)(startingNonce >> 16);
                        cmd[offset + 3] = (byte)(startingNonce >> 24);
                        offset += 4;

                        byte[] headerBytes = HexConversionHelper.ConvertFromHexString(HexConversionHelper.Reverse(work.Header));
                        headerBytes.CopyTo(cmd, offset);

                        LogHelper.DebugConsoleLogAsync(string.Format("{0} getting: {1}", this.Name, HexConversionHelper.ConvertToHexString(cmd)));

                        LogHelper.DebugLogToFileAsync(string.Format("{0} getting: {1}", this.Name, HexConversionHelper.ConvertToHexString(cmd)), deviceLogFile);

                        // Send work to the miner
                        this.currentWork = work;
                        this.usbPort.DiscardInBuffer();

                        this.SendCommand(cmd);
                    }
                    else
                    {
                        LogHelper.DebugConsoleLogAsync(string.Format("Device {0} pending work {1}.", this.Name, work.JobId), LogVerbosity.Verbose);

                        this.pendingWork = work;
                    }
                }
            }
            catch (Exception e)
            {
                LogHelper.LogError(e);

                throw e;
            }
        }

        protected override void DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort sp = sender as SerialPort;

            if (sp != null)
            {
                while (sp.BytesToRead >= 4)
                {
                    this.RestartWatchdogTimer();
                    if (sp.Read(_eventPacket, 0, 4) == 4)
                    {
                        ProcessEventPacket(_eventPacket);
                    }
                }

                // Check if there was any left over data
                if (sp.BytesToRead > 0)
                    timesNonZero++;
                else
                    timesNonZero = 0;

                if (timesNonZero >= extraDataThreshold)
                {
                    // Attempt to prevent a synchronization error in the underlying bytestream
                    sp.DiscardInBuffer();
                }
            }
        }

        private void ProcessEventPacket(byte[] packet)
        {
            if (currentWork != null)
            {
                long nonce = ((long)packet[3] << 24) | ((long)packet[2] << 16) | ((long)packet[1] << 8) | (long)packet[0];
                string nonceString = string.Format("{0:X8}", nonce);

                if (this.ValidateNonce(nonceString))
                {
                    this.SubmitWork(currentWork, nonceString);
                }
                else
                {
                    this.OnInvalidNonce(currentWork);
                }

                if (LogHelper.ShouldDisplay(LogVerbosity.Verbose))
                {
                    LogHelper.ConsoleLog(string.Format("Device {0} submitting {1} for job {2}.", this.Name, nonceString, this.currentWork.JobId), ConsoleColor.DarkCyan, LogVerbosity.Verbose);
                }
            }
        }

        protected long GetExpectedHashrate()
        {
            return (long)(LtcClk * 84.5 * 8) * Cores;
        }

        public override void WorkRejected(IPoolWork work)
        {
            LogHelper.DebugConsoleLog(string.Format("Device {0} requesting new work since it had rejected work.", this.Name));
            this.RequestWork();
        }
    }
}
