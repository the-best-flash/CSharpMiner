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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
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

        private const double fRef = 6.32; // Estimated value for PLL reference frequency in Mhz
        private const byte freqMaskHigh = 0xF0;
        private const byte freqMaskLow = 0x0F;
        private const byte pllBandMask = 0xBF;
        private const int minFreq = 300;
        private const int maxLowBandFreq = 550;

        private static byte[] resetDeviceCommand = { 0x55, 0xAA, 0xC0, 0x00, 
                                                     0x80, 0x80, 0x80, 0x80, 
                                                     0x00, 0x00, 0x00, 0x00, 
                                                     0x01, 0x00, 0x00, 0x00 }; // Ask the CPM to reset the device

        private static byte[] setChipsCommand = { 0x55, 0xAA, 0xC0, 0x00, 
                                                  0xC0, 0xC0, 0xC0, 0xC0,
                                                  0x05, 0x00, 0x00, 0x00, 
                                                  0x01, 0x00, 0x00, 0x00 }; // Tell the CPM how many chips it has (default 5)

        private static byte[] ltcResetCommand = { 0x55, 0xAA, 0x1F, 0x28, 
                                                  0x16, 0x00, 0x00, 0x00 }; // Set SW reset bits low for calculation engine and not the reporting engine

        private static byte[] ltcStartCommand = { 0x55, 0xAA, 0x1F, 0x28, 
                                                  0x17, 0x00, 0x00, 0x00 }; // Set SW bits high for calc engine and reporting engine, also look for <= target

        private static byte[] ltcConfigRegCommand = { 0x55, 0xAA, 0xEF, 0x30,
                                                      0x20, 0x00, 0x00, 0x00 }; // Set rpt_p as ltc_rpt (From documentation, not sure what this is) also LTC_CLK = BTC_CLK (No clock division)

        private static byte[] coreFreqCommand = { 0x55, 0xAA, 0xEF, 0x00, 
                                                  0x05, 0x00, 0x60, 0x98 }; // Default to 850 Mhz, bypass PLL, Low band PLL, PLL output clock gate, Apply settings, Fvco / 2

        private static byte[] disableBtcCommand = { 0x55, 0xAA, 0xEF, 0x02, 
                                                    0x00, 0x00, 0x00, 0x00, 
                                                    0x00, 0x00, 0x00, 0x00, 
                                                    0x00, 0x00, 0x00, 0x00, 
                                                    0x00, 0x00, 0x00, 0x00, 
                                                    0x00, 0x00, 0x00, 0x00 }; // Disable the BTC cores

        private static byte[] getFirmwareVersionCommand = { 0x55, 0xAA, 0xC0, 0x00, 
                                                            0x90, 0x90, 0x90, 0x90, 
                                                            0x00, 0x00, 0x00, 0x00, 
                                                            0x01, 0x00, 0x00, 0x00 }; // Ask the gridseed for its firmware version

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

        private bool _workPending;
        private bool _deviceInitialized;
        private byte[] _CommandBuf;
        private byte[] _ResponsePacket;
        private int _currentTaskId;
        private IPoolWork _currentWork;

        private Thread _commandThread;

        private BlockingCollection<byte[]> _commandBuffer;
        private BlockingCollection<byte[]> CommandBuffer
        {
            get
            {
                if(_commandBuffer == null)
                {
                    _commandBuffer = new BlockingCollection<byte[]>();
                }

                return _commandBuffer;
            }
        }

        [OnDeserializing]
        private void OnDeserializing(StreamingContext context)
        {
            this.OnDeserializing();
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            this.OnDeserialized();
        }

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

            SetDefaultValues();
        }

        protected override void OnConnected()
        {
            base.OnConnected();

            if(_commandThread == null)
            {
                _commandThread = new Thread(new ThreadStart(this.ProcessCommands));
                _commandThread.Start();
            }

            InitDevice();
        }

        public override void Unload()
        {
            if(_commandThread != null)
            {
                CommandBuffer.CompleteAdding();
                _commandThread.Join(200);
                _commandThread.Abort();
                _commandThread = null;
            }

            _deviceInitialized = false;

            base.Unload();
        }

        private void ProcessCommands()
        {
            foreach(byte[] cmd in this.CommandBuffer.GetConsumingEnumerable())
            {
                SendCommand(cmd);
            }
        }

        protected override void SendCommand(byte[] cmd)
        {
            base.SendCommand(cmd);

            LogHelper.DebugConsoleLog(string.Format("Device {0} getting: {1}", this.Name, HexConversionHelper.ConvertToHexString(cmd)), ConsoleColor.Cyan);

            Thread.Sleep(60);
        }

        private void AddCommandToQueue(byte[] cmd)
        {
            this.CommandBuffer.Add(cmd);
        }

        public override void Reset()
        {
            this.InitDevice();

            if(this._currentWork != null)
                this.SendWorkToDevice(this._currentWork, this._lastStartingNonce, this._lastEndingNonce);
        }

        private void InitDevice()
        {
            this._deviceInitialized = false;

            this.AddCommandToQueue(getFirmwareVersionCommand);

            this.AddCommandToQueue(resetDeviceCommand);

            byte[] chipCommand = setChipsCommand.Clone() as byte[];

            if (Chips == 0)
                Chips = defaultChips;

            int chips = Chips;
            chipCommand[chipCommand.Length - 8] = (byte)(chips);
            chipCommand[chipCommand.Length - 7] = (byte)(chips >> 8);
            chipCommand[chipCommand.Length - 6] = (byte)(chips >> 16);
            chipCommand[chipCommand.Length - 5] = (byte)(chips >> 24);

            this.AddCommandToQueue(chipCommand);

            // Send gridseed settings to device
            this.AddCommandToQueue(ltcResetCommand);
            this.AddCommandToQueue(ltcStartCommand);
            this.AddCommandToQueue(disableBtcCommand);
            this.AddCommandToQueue(ltcConfigRegCommand);

            if (Frequency < minFreq)
            {
                Frequency = defaultFreq;
            }

            int freqSetting = (byte)(Math.Ceiling(Frequency / fRef) - 1);
            byte[] freqCommand = coreFreqCommand.Clone() as byte[];

            freqCommand[freqCommand.Length - 1] &= freqMaskHigh;
            freqCommand[freqCommand.Length - 1] |= (byte)(freqSetting >> 4);

            freqCommand[freqCommand.Length - 2] &= freqMaskLow;
            freqCommand[freqCommand.Length - 2] |= (byte)(freqSetting << 4);

            this.AddCommandToQueue(freqCommand);

            this._deviceInitialized = true;

            if (this._workPending)
            {
                this.AddCommandToQueue(_CommandBuf);
                this._workPending = false;
            }
        }

        private void SetDefaultValues()
        {
            Chips = defaultChips;
            Frequency = defaultFreq;

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
                if (sp.BytesToRead >= 24)
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
                }
                else if(sp.BytesToRead >= 12)
                {
                    while (sp.BytesToRead >= 12)
                    {
                        this.RestartWatchdogTimer();
                        if (sp.Read(_ResponsePacket, 0, 12) == 12)
                        {
                            if (!ProcessResponsePacket(_ResponsePacket))
                            {
                                error = true;
                            }
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
                        int taskId = (int)(((long)response[11] << 24) | ((long)response[10] << 16) | ((long)response[9] << 8) | (long)response[8]);

                        if (taskId >= _currentTaskId)
                        {
                            long nonce = ((long)response[4] << 24) | ((long)response[5] << 16) | ((long)response[6] << 8) | (long)response[7];
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
                            LogHelper.DebugConsoleLogError(string.Format("Device {0} discarding share with old task ID. Got {1}. Expected {2}.", this.Name, taskId, _currentTaskId));
                        }
                    }
                }
                else if(response[1] == 0xAA && response[2] == 0xC0)
                {
                    // Interaction Response
                    // TODO

                    LogHelper.DebugConsoleLog(string.Format("Got response from {0}: {1}", this.Name, HexConversionHelper.ConvertToHexString(response).Substring(0, 24)));
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

        protected override void SendWorkToDevice(IPoolWork work)
        {
            CopyWorkToCommandBuffer(work);
        }

        protected override void SendWorkToDevice(IPoolWork work, long startingNonce, long endingNonce)
        {
            CopyWorkToCommandBuffer(work, startingNonce, endingNonce);
        }

        private long _lastStartingNonce, _lastEndingNonce;

        private void CopyWorkToCommandBuffer(IPoolWork work, long startingNonce = 0x00000000, long endingNonce = 0xFFFFFFFF)
        {
            _lastEndingNonce = endingNonce;
            _lastStartingNonce = startingNonce;

            int taskId = _currentTaskId + 1;

            byte[] headerBytes = HexConversionHelper.ConvertFromHexString(work.Header);

            byte[] midstate = HashHelper.ComputeMidstate(headerBytes.Take(64).ToArray());

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
            midstate.CopyTo(_CommandBuf, 36);

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

            _currentWork = work;
            _currentTaskId = taskId;

            if (this._deviceInitialized)
            {
                this._workPending = false;
                this.AddCommandToQueue(ltcResetCommand);
                this.AddCommandToQueue(ltcStartCommand);
                this.AddCommandToQueue(_CommandBuf);
            }
            else
            {
                this._workPending = true;
            }
        }
    }
}
