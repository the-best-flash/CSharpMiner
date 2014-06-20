using CSharpMiner;
using CSharpMiner.Helpers;
using CSharpMiner.Stratum;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace MiningDevice
{
    [DataContract]
    class ZeusDevice : UsbMinerBase
    {
        private int _clk;
        [DataMember(Name = "clock")]
        public int LtcClk 
        { 
            get
            {
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

        private PoolWork currentWork = null;

        public ZeusDevice(string port, int clk, int cores)
        {
            UARTPort = port;
            LtcClk = clk;
            Cores = cores;
        }

        public override void StartWork(PoolWork work)
        {
            if (this.usbPort != null && this.usbPort.IsOpen)
            {
                Program.DebugConsoleLog(string.Format("Device {0} starting work {1}.", this.UARTPort, work.JobId));

                int diffCode = 0xFFFF / work.Diff;
                byte[] cmd = CommandPacket;

                cmd[3] = (byte)diffCode;
                cmd[2] = (byte)(diffCode >> 8);

                int offset = 4;

                // Starting nonce
                byte[] nonceBytes = HexConversionHelper.ConvertFromHexString(Swap(string.Format("{0,8:X8}", work.StartingNonce)));
                CopyToByteArray(nonceBytes, offset, cmd);
                offset += nonceBytes.Length;

                // nbits
                byte[] netDiffBytes = HexConversionHelper.ConvertFromHexString(Swap(work.NetworkDiff));
                CopyToByteArray(netDiffBytes, offset, cmd);
                offset += netDiffBytes.Length;

                // timestamp
                byte[] timestampBytes = HexConversionHelper.ConvertFromHexString(Swap(work.Timestamp));
                CopyToByteArray(timestampBytes, offset, cmd);
                offset += timestampBytes.Length;

                // merkel root
                Program.DebugConsoleLog(string.Format("merkel: {0}", work.MerkelRoot));
                Program.DebugConsoleLog(string.Format("merkel: {0}", Swap(work.MerkelRoot)));

                byte[] merkelBytes = HexConversionHelper.ConvertFromHexString(Swap(work.MerkelRoot));
                CopyToByteArray(merkelBytes, offset, cmd);
                offset += merkelBytes.Length;

                // previous hash
                byte[] previousHashBytes = HexConversionHelper.ConvertFromHexString(Swap(work.PreviousHash));
                CopyToByteArray(previousHashBytes, offset, cmd);
                offset += previousHashBytes.Length;

                // version
                Program.DebugConsoleLog(string.Format("Version: {0}", work.Version));
                Program.DebugConsoleLog(string.Format("Version: {0}", Swap(work.Version)));

                byte[] versionBytes = HexConversionHelper.ConvertFromHexString(Swap(work.Version));
                CopyToByteArray(versionBytes, offset, cmd);

                // Send work to the miner
                this.currentWork = work;
                this.usbPort.Write(cmd, 0, cmd.Length);
            }
            else
            {
                Program.DebugConsoleLog(string.Format("Device {0} pending work {1}.", this.UARTPort, work.JobId));

                this.pendingWork = work;
            }
        }

        private void CopyToByteArray(byte[] src, int offset, byte[] dest)
        {
            for (int i = 0; i < src.Length; i++)
            {
                dest[i + offset] = src[i];
            }
        }

        private string Swap(string hex)
        {
            StringBuilder sb = new StringBuilder(hex.Length);

            // Split into sections of 8 and then reverse the bytes in thoes sections
            for(int i = 0; i < hex.Length; i += 8)
            {
                for(int j = i + 6; j >= i; j -= 2)
                {
                    sb.Append(hex.Substring(j, 2));
                }
            }

            return sb.ToString();
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
                Task.Factory.StartNew(() =>
                    {
                        while (sp.BytesToRead >= 4)
                        {
                            sp.Read(EventPacket, 0, 4);
                            ProcessEventPacket(EventPacket);
                        }
                    });
            }
        }

        private void ProcessEventPacket(byte[] packet)
        {
            if(currentWork != null)
            {
                Program.DebugConsoleLog(string.Format("Submitting {0} for job {1}.",HexConversionHelper.ConvertToHexString(packet), (this.currentWork != null ? this.currentWork.JobId : "null")));
                _submitWork(currentWork, Swap(HexConversionHelper.ConvertToHexString(packet)));
            }
        }
    }
}
