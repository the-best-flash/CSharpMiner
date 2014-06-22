using CSharpMiner;
using CSharpMiner.Helpers;
using CSharpMiner.Stratum;
using DeviceManager;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MiningDevice
{
    [DataContract]
    public abstract class UsbMinerBase : IMiningDevice
    {
        [IgnoreDataMember]
        public int Id { get; set; }

        [DataMember(Name = "port")]
        public string UARTPort { get; set; }

        [DataMember(Name = "cores")]
        public int Cores { get; set; }

        [IgnoreDataMember]
        public int HashRate { get; protected set; }

        [IgnoreDataMember]
        public int HardwareErrors { get; protected set; }

        protected Action<PoolWork, string, int> _submitWork = null;
        protected Thread listenerThread = null;
        protected SerialPort usbPort = null;
        protected PoolWork pendingWork = null;

        private bool continueRunning = true;

        public void Load(Action<PoolWork, string, int> submitWork)
        {
            _submitWork = submitWork;

            if (this.listenerThread == null)
            {
                this.listenerThread = new Thread(new ThreadStart(this.Connect));
                this.listenerThread.Start();
            }
        }

        private void Connect()
        {
            try
            {
                string[] portNames = SerialPort.GetPortNames();

                if (!portNames.Contains(UARTPort))
                {
                    Exception e = new SerialConnectionException(string.Format("{0} is not a valid USB port.", (UARTPort != null ? UARTPort : "null")));

                    LogHelper.LogErrorSecondary(e);

                    throw e;
                }

                try
                {
                    continueRunning = true;
                    usbPort = new SerialPort(UARTPort, GetBaud());
                    //usbPort.DataReceived += DataReceived;
                    usbPort.Open();
                }
                catch (Exception e)
                {
                    LogHelper.ConsoleLogErrorAsync(string.Format("Error connecting to {0}.", UARTPort));
                    throw new SerialConnectionException(string.Format("Error connecting to {0}: {1}", UARTPort, e), e);
                }

                LogHelper.ConsoleLogAsync(string.Format("Successfully connected to {0}.", UARTPort), LogVerbosity.Verbose);

                if (this.pendingWork != null)
                {
                    Task.Factory.StartNew(() =>
                        {
                            this.StartWork(pendingWork);
                            pendingWork = null;
                        });
                }

                while (this.continueRunning)
                {
                    if (usbPort.BytesToRead > 0)
                    {
                        DataReceived(usbPort, null);
                    }

                    Thread.Sleep(100);
                }
            }
            catch (Exception e)
            {
                LogHelper.LogErrorAsync(e);
                this.Unload();
                this.Load(_submitWork);
            }
        }

        public void Unload()
        {
            if (continueRunning)
            {
                continueRunning = false;

                if (usbPort != null && usbPort.IsOpen)
                    usbPort.Close();
            }
        }

        public abstract void StartWork(PoolWork work);
        public abstract int GetBaud();
        protected abstract void DataReceived(object sender, SerialDataReceivedEventArgs e);

        public void Dispose()
        {
            this.Unload();
        }
    }
}
