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
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Gridseed
{
    [DataContract]
    public class GridseedDevice : UsbMinerBase, IGridseedDeviceSettings
    {
        internal const int defaultChips = 5;
        internal const int defaultFreq = 700;
        internal const int gBladeChips = 80;
        internal const string chipsExampleString = "5";
        internal const string chipsDescriptionString = "Number of Gridseed chips in the device. [Default = 5, GBlade = 80]";
        internal const string freqDescriptionString = "Core Clock in Mhz. [Default = 700, Recommended Max = 900]";

        private const double fRef = 12.6760; // Estimated value for PLL reference frequency in Mhz
        private const byte freqMaskHigh = 0xF0;
        private const byte freqMaskLow = 0x0F;
        private const byte pllBandMask = 0xBF;
        private const int minFreq = 300;
        private const int maxLowBandFreq = 550;

        private static byte[] resetDeviceCommand = { 0x55, 0xAA, 0xC0, 0x00, 
                                                     0xE0, 0xE0, 0xE0, 0xE0, 
                                                     0x00, 0x00, 0x00, 0x00, 
                                                     0x01, 0x00, 0x00, 0x00 }; // Ask the CPM to reset the device

        private static byte[] setChipsCommand = { 0x55, 0xAA, 0xC0, 0x00, 
                                                  0xC0, 0xC0, 0xC0, 0xC0,
                                                  0x05, 0x00, 0x00, 0x00, 
                                                  0x01, 0x00, 0x00, 0x00 }; // Tell the CPM how many chips it has (default 5)

        private static byte[] ltcResetCommand = { 0x55, 0xAA, 0x1F, 0x28, 
                                                  0x10, 0x00, 0x00, 0x00 }; // Set SW reset bits low for calculation engine and reporting engine

        private static byte[] ltcConfigCommand = { 0x55, 0xAA, 0x1F, 0x28, 
                                                   0x13, 0x00, 0x00, 0x00 }; // Set SW bits high for calc engine and reporting engine, also look for <= target

        private static byte[] ltcConfigRegCommand = { 0x55, 0xAA, 0xEF, 0x30,
                                                      0x20, 0x00, 0x00, 0x00 }; // Set rpt_p as ltc_rpt (From documentation, not sure what this is) also LTC_CLK = BTC_CLK (No clock division)

        private static byte[] coreFreqCommand = { 0x55, 0xAA, 0xEF, 0x00, 
                                                  0x05, 0x00, 0x70, 0xC3 }; // Default to 700 Mhz, bypass PLL, High band PLL, PLL output clock gate, Apply settings

        private static byte[] disableBtcCommand = { 0x55, 0xAA, 0xEF, 0x02, 
                                                    0x00, 0x00, 0x00, 0x00, 
                                                    0x00, 0x00, 0x00, 0x00, 
                                                    0x00, 0x00, 0x00, 0x00, 
                                                    0x00, 0x00, 0x00, 0x00, 
                                                    0x00, 0x00, 0x00, 0x00 }; // Disable the BTC cores

        private int _chips;
        [DataMember(Name = "chips", IsRequired = true)]
        [MiningSetting(ExampleValue = chipsExampleString, Optional = false, Description = chipsDescriptionString)]
        public int Chips
        {
            get
            {
                return _chips;
            }

            set
            {
                _chips = value;

                HashRate = this.GetExpectedHashrate();
            }
        }

        private int _freq;
        [DataMember(Name = "freq", IsRequired = true)]
        [MiningSetting(ExampleValue = "850", Optional = false, Description = freqDescriptionString)]
        public int Frequency
        {
            get
            {
                return _freq;
            }

            set
            {
                _freq = value;

                HashRate = this.GetExpectedHashrate();

                if (this.IsConnected)
                    this.Restart();
            }
        }

        [IgnoreDataMember]
        public override int BaudRate
        {
            get
            {
                return 115200;
            }
        }

        private string _lastPort;
        private string _name;
        [IgnoreDataMember]
        public override string Name
        {
            get
            {
                if (_name == null || _lastPort == null || _lastPort != this.Port)
                    _name = string.Format("{0}-{1}", (this.Chips != gBladeChips ? "GC3355" : "GBlade"), this.Port);

                _lastPort = this.Port;

                return _name;
            }
        }

        private byte[] _CommandBuf;
        private byte[] _ResponsePacket;
        private int _currentTaskId;
        private IPoolWork _currentWork;

        public GridseedDevice(string port, int freq = defaultFreq, int chips = defaultChips, int watchdogTimeout = defaultWatchdogTimeout, int pollFrequency = defaultPollTime)
            : base(port, watchdogTimeout, pollFrequency)
        {
            this.Frequency = freq;
            this.Chips = chips;
        }

        private long GetExpectedHashrate()
        {
            return (long)(_chips * 84.7 * _freq); // estimated hashes per second
        }

        protected override void OnDeserializing()
        {
            base.OnDeserializing();

            Chips = defaultChips;
            Frequency = defaultFreq;
        }

        protected override void OnConnected()
        {
            base.OnConnected();

            InitDevice();
        }

        private void InitDevice()
        {
            this.SendCommand(resetDeviceCommand);

            byte[] chipCommand = setChipsCommand.Clone() as byte[];

            if (Chips == 0)
                Chips = defaultChips;

            int chips = Chips;
            chipCommand[chipCommand.Length - 8] = (byte)(chips);
            chipCommand[chipCommand.Length - 7] = (byte)(chips >> 8);
            chipCommand[chipCommand.Length - 6] = (byte)(chips >> 16);
            chipCommand[chipCommand.Length - 5] = (byte)(chips >> 24);

            this.SendCommand(chipCommand);

            // Send gridseed settings to device
            this.SendCommand(disableBtcCommand);
            this.SendCommand(ltcResetCommand);
            this.SendCommand(ltcConfigCommand);
            this.SendCommand(ltcConfigRegCommand);

            if(Frequency < minFreq)
            {
                Frequency = defaultFreq;
            }

            int freqSetting = (byte)(Math.Ceiling(Frequency / fRef) - 1);
            byte[] freqCommand = coreFreqCommand.Clone() as byte[];

            freqCommand[freqCommand.Length - 1] &= freqMaskHigh;
            freqCommand[freqCommand.Length - 1] |= (byte)(freqSetting >> 4);

            freqCommand[freqCommand.Length - 2] &= freqMaskLow;
            freqCommand[freqCommand.Length - 2] |= (byte)(freqSetting << 4);

            if (Frequency > maxLowBandFreq)
            {
                freqCommand[freqCommand.Length - 1] &= pllBandMask;
                freqCommand[freqCommand.Length - 1] |= 0x40; // Set the pll high band bit
            }
            else
            {
                freqCommand[freqCommand.Length - 1] &= pllBandMask; // Clear the pll high band bit
            }
        }

        private void SetDefaultValues()
        {
            _currentWork = null;
            _currentTaskId = 0;
            _ResponsePacket = new byte[24];
            _CommandBuf = new byte[156];

            // Encode the command header (4 bytes)
            _CommandBuf[0] = 0x55;
            _CommandBuf[1] = 0xaa;
            _CommandBuf[2] = 0x1f;
            _CommandBuf[3] = 0x00;

            Random rand = new Random();
            int taskId = rand.Next();
        }

        protected override void DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort sp = sender as SerialPort;
            bool error = false;

            if (sp != null)
            {
                while (sp.BytesToRead >= 24)
                {
                    this.RestartWatchdogTimer();
                    if (sp.Read(_ResponsePacket, 0, 24) == 24)
                    {
                        if (!ProcessResponsePacket(_ResponsePacket))
                        {
                            error = true;
                        }
                    }
                }

                if(error)
                {
                    // Something is wrong, lets discard any left over data to attempt to synchronize the bytestream
                    sp.DiscardInBuffer();
                }
            }
        }

        private bool ProcessResponsePacket(byte[] response)
        {
            // Check for the response header
            if(response[0] == 0x55 && response[3] == 0x00)
            {
                if (response[1] == 0x20) // LTC nonce
                {
                    if (_currentWork != null)
                    {
                        int taskId = (int)(((long)response[8] << 24) | ((long)response[9] << 16) | ((long)response[10] << 8) | (long)response[11]);

                        if (taskId == _currentTaskId)
                        {
                            long nonce = ((long)response[7] << 24) | ((long)response[6] << 16) | ((long)response[5] << 8) | (long)response[4];
                            string nonceString = string.Format("{0:X8}", nonce);

                            if (this.ValidateNonce(nonceString))
                            {
                                this.SubmitWork(_currentWork, nonceString);
                            }
                            else
                            {
                                this.OnInvalidNonce(_currentWork);
                            }

                            if (LogHelper.ShouldDisplay(LogVerbosity.Verbose))
                            {
                                LogHelper.ConsoleLog(string.Format("Device {0} submitting {1} for job {2}.", this.Name, nonceString, this._currentWork.JobId), ConsoleColor.DarkCyan, LogVerbosity.Verbose);
                            }
                        }
                        else
                        {
                            LogHelper.DebugConsoleLogError(string.Format("Device {0} discarding share with old task ID. Got {0}. Expected {1}.", taskId, _currentTaskId));
                        }
                    }
                }
                else if(response[1] == 0xAA && response[2] == 0xC0)
                {
                    // Interaction Response
                    // TODO
                }

                return true;
            }

            return false;
        }

        public override void WorkRejected(IPoolWork work)
        {
            LogHelper.DebugConsoleLog(string.Format("Device {0} requesting new work since it had rejected work.", this.Name));
            this.RequestWork();
        }

        public override void StartWork(IPoolWork work)
        {
            CopyWorkToCommandBuffer(work);
        }

        public override void StartWork(IPoolWork work, long startingNonce, long endingNonce)
        {
            CopyWorkToCommandBuffer(work, startingNonce, endingNonce);
        }

        private void CopyWorkToCommandBuffer(IPoolWork work, long startingNonce = 0x00000000, long endingNonce = 0xFFFFFFFF)
        {
            int taskId = _currentTaskId + 1;

            byte[] headerBytes = HexConversionHelper.ConvertFromHexString(work.Header);

            byte[] target = MathHelper.ConvertDifficultyToTarget(work.Diff);

            // Encode the target (32 bytes)
            //4
            for (int i = 0; i < target.Length; i++)
            {
                _CommandBuf[4 + i] = target[target.Length - i - 1];
            }

            //35
            // Midstate (32 bytes)
            //36
            // TODO

            //67
            // Block Header (76 bytes)
            //68 
            headerBytes.CopyTo(_CommandBuf, 68);

            //143
            // Encode the starting nonce (4 bytes)
            //144
            _CommandBuf[144] = (byte)(startingNonce);
            _CommandBuf[145] = (byte)(startingNonce >> 8);
            _CommandBuf[146] = (byte)(startingNonce >> 16);
            _CommandBuf[147] = (byte)(startingNonce >> 24);

            //147
            // Encode the ending nonce (4 bytes)
            //148
            _CommandBuf[148] = (byte)(endingNonce);
            _CommandBuf[149] = (byte)(endingNonce >> 8);
            _CommandBuf[150] = (byte)(endingNonce >> 16);
            _CommandBuf[151] = (byte)(endingNonce >> 24);

            //151
            // Encode the LTC task ID (4 bytes)
            //152
            _CommandBuf[152] = (byte)taskId;
            _CommandBuf[153] = (byte)(taskId >> 8);
            _CommandBuf[154] = (byte)(taskId >> 16);
            _CommandBuf[155] = (byte)(taskId >> 24);

            this.SendCommand(_CommandBuf);

            _currentWork = work;
            _currentTaskId = taskId;
        }
    }
}
