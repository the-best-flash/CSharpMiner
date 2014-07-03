using CSharpMiner.Helpers;
using CSharpMiner.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace WpfMiner
{
    public class MiningDeviceData
    {
        private Observable<string> _acceptedHashrate;
        public Observable<string> AcceptedHashrate
        {
            get
            {
                if(_acceptedHashrate == null)
                {
                    _acceptedHashrate = new Observable<string>("0.0Mh (0%)");
                }

                return _acceptedHashrate;
            }
        }

        private Observable<string> _rejectedHashrate;
        public Observable<string> RejectedHashrate
        {
            get
            {
                if (_rejectedHashrate == null)
                {
                    _rejectedHashrate = new Observable<string>("0.0Mh (0%)");
                }

                return _rejectedHashrate;
            }
        }

        private Observable<string> _discardedHashrate;
        public Observable<string> DiscardedHashrate
        {
            get
            {
                if (_discardedHashrate == null)
                {
                    _discardedHashrate = new Observable<string>("0.0Mh (0%)");
                }

                return _discardedHashrate;
            }
        }

        private Observable<string> _name;
        public Observable<string> Name
        {
            get
            {
                if (_name == null)
                {
                    _name = new Observable<string>(string.Empty);
                }

                return _name;
            }
        }

        public IMiningDevice Device { get; private set; }

        Timer _timer;

        public MiningDeviceData(IMiningDevice d)
        {
            _timer = new Timer(1000);
            _timer.Elapsed += Timer_Elapsed;
            _timer.Start();

            Device = d;
            Name.Value = string.Format("{0} ({1})", d.Name, d.GetType().Name);
            UpdatedHashRate(d, false);
        }

        void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if(this.Device != null)
            {
                UpdatedHashRate(this.Device, false);
            }
        }

        public void UpdatedHashRate(IMiningDevice d, bool resetTimer = true)
        {
            double acceptedHash = d.AcceptedHashRate;
            double rejectedHash = d.RejectedHashRate;
            double discardedHash = d.DiscardedHashRate;

            double total = acceptedHash + rejectedHash + discardedHash;

            if (total > 0)
            {
                AcceptedHashrate.Value = string.Format("{0} ({1}%)", HashHelper.MegaHashDisplayString(acceptedHash), (int)(acceptedHash / total * 100));
                RejectedHashrate.Value = string.Format("{0} ({1}%)", HashHelper.MegaHashDisplayString(rejectedHash), (int)(rejectedHash / total * 100));
                DiscardedHashrate.Value = string.Format("{0} ({1}%)", HashHelper.MegaHashDisplayString(discardedHash), (int)(discardedHash / total * 100));
            }
            else
            {
                AcceptedHashrate.Value = string.Format("{0} (0%)", "0.00Mh");
                RejectedHashrate.Value = string.Format("{0} (0%)", "0.00Mh");
                DiscardedHashrate.Value = string.Format("{0} (0%)", "0.00Mh");
            }

            if(resetTimer)
            {
                _timer.Stop();
                _timer.Start();
            }
        }

        public void Done()
        {
            _timer.Stop();
            AcceptedHashrate.Done();
        }
    }
}
