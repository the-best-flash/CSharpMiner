using CSharpMiner.Stratum;
using DeviceManager;
using System;
using System.Collections.Generic;
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
        [DataMember(Name = "port")]
        public string UARTPort { get; set; }

        [DataMember(Name = "cores")]
        public int Cores { get; set; }

        [IgnoreDataMember]
        public int HashRate { get; protected set; }

        [IgnoreDataMember]
        public int HardwareErrors { get; protected set; }

        protected Action<PoolWork, string> _submitWork = null;
        protected Thread listenerThread = null;

        private bool continueRunning = true;

        public void Load(Action<PoolWork, string> submitWork)
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
            continueRunning = true;

            // TODO Set up UART

            while (continueRunning) ;

            throw new NotImplementedException();
        }

        public void Unload()
        {
            continueRunning = false;
            // TODO shutdown UART

            throw new NotImplementedException();
        }

        public abstract void StartWork(PoolWork work);

        public void Dispose()
        {
            this.Unload();
        }
    }
}
