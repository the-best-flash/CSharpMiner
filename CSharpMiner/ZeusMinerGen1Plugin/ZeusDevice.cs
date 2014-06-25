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

        private int _cores = 1;
        [DataMember(Name = "cores")]
        [MiningSetting(ExampleValue = "6", Optional = false, Description = "Number of ZeusChips in the device.")]
        public int Cores
        {
            get
            {
                return _cores;
            }
            set
            {
                _cores = value;

                HashRate = this.GetExpectedHashrate() * Cores;
            }
        }

        private int _clk;
        [DataMember(Name = "clock")]
        [MiningSetting(ExampleValue = "328", Description = "The clockspeed of the miner. Max = 382", Optional = true)]
        public int LtcClk 
        { 
            get
            {
                if (_clk == 0)
                    _clk = 328;

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

                byte[] cmd = CommandPacket;
                cmd[0] = _freqCode;
                cmd[1] = (byte)(0xFF - _freqCode);

                HashRate = this.GetExpectedHashrate() * Cores;
            }
        }

        private byte[] _eventPacket = null;
        [IgnoreDataMember]
        private byte[] EventPacket
        {
            get
            {
                if (_eventPacket == null)
                    _eventPacket = new byte[4];

                return _eventPacket;
            }
        }

        private byte[] _commandPacket = null;
        [IgnoreDataMember]
        private byte[] CommandPacket
        {
            get
            {
                if (_commandPacket == null)
                    _commandPacket = new byte[84];

                return _commandPacket;
            }
        }

        [IgnoreDataMember]
        public override string Name
        {
            get { return this.Port; }
        }

        private IPoolWork currentWork = null;
        private int timesNonZero = 0;

        public ZeusDevice(string port, int clk, int cores, int watchdogTimeout, int pollFrequency = defaultPollTime)
        {
            Port = port;
            LtcClk = clk;
            Cores = cores;
            WatchdogTimeout = watchdogTimeout;
            PollFrequency = defaultPollTime;
        }

        public override void StartWork(IPoolWork work)
        {
            if (work != null)
            {
                timesNonZero = 0;
                this.RestartWatchdogTimer();

                if (this.usbPort != null && this.usbPort.IsOpen)
                {
                    LogHelper.ConsoleLogAsync(string.Format("Device {0} starting work {1}.", this.Name, work.JobId), LogVerbosity.Verbose);

                    this.RestartWorkRequestTimer();

                    int diffCode = 0xFFFF / work.Diff;
                    byte[] cmd = CommandPacket;

                    cmd[3] = (byte)diffCode;
                    cmd[2] = (byte)(diffCode >> 8);

                    int offset = 4;

                    // Starting nonce
                    byte[] nonceBytes = HexConversionHelper.ConvertFromHexString(HexConversionHelper.Swap(string.Format("{0,8:X8}", work.StartingNonce)));
                    CopyToByteArray(nonceBytes, offset, cmd);
                    offset += nonceBytes.Length;

                    byte[] headerBytes = HexConversionHelper.ConvertFromHexString(HexConversionHelper.Reverse(work.Header));
                    CopyToByteArray(headerBytes, offset, cmd);

                    LogHelper.DebugConsoleLogAsync(string.Format("{0} getting: {1}", this.Name, HexConversionHelper.ConvertToHexString(cmd)));

                    // Send work to the miner
                    this.currentWork = work;
                    this.usbPort.DiscardInBuffer();
                    this.usbPort.Write(cmd, 0, cmd.Length);
                }
                else
                {
                    LogHelper.DebugConsoleLogAsync(string.Format("Device {0} pending work {1}.", this.Name, work.JobId), LogVerbosity.Verbose);

                    this.pendingWork = work;
                }
            }
        }

        private void CopyToByteArray(byte[] src, int offset, byte[] dest)
        {
            for (int i = 0; i < src.Length; i++)
            {
                dest[i + offset] = src[i];
            }
        }

        public override int GetBaud()
        {
            return 115200;
        }

        protected override void DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort sp = sender as SerialPort;

            if(sp != null)
            {
                while (sp.BytesToRead >= 4)
                {
                    this.RestartWatchdogTimer();
                    sp.Read(EventPacket, 0, 4);
                    ProcessEventPacket(EventPacket);
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

        private bool ValidateNonce(string nonce)
        {
            // TODO: Make this do something
            return true;
        }

        private void ProcessEventPacket(byte[] packet)
        {
            if(currentWork != null)
            {
                Task.Factory.StartNew(() =>
                    {
                        LogHelper.ConsoleLogAsync(string.Format("Device {0} submitting {1} for job {2}.", this.Name, HexConversionHelper.ConvertToHexString(packet), (this.currentWork != null ? this.currentWork.JobId : "null")), ConsoleColor.DarkCyan, LogVerbosity.Verbose);

                        string nonce = HexConversionHelper.Swap(HexConversionHelper.ConvertToHexString(packet));

                        if (this.ValidateNonce(nonce))
                        {
                            this.SubmitWork(currentWork, nonce);
                        }
                        else
                        {
                            this.OnInvalidNonce(currentWork);
                        }
                    });
            }
        }

        protected long GetExpectedHashrate()
        {
            return (long)(LtcClk * 87.5 * 8);
        }

        public override void WorkRejected(IPoolWork work)
        {
            LogHelper.DebugConsoleLog(string.Format("Device {0} requesting new work since it had rejected work.", this.Name));
            this.RequestWork();
        }
    }
}
