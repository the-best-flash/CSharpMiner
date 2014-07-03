using CSharpMiner;
using CSharpMiner.Helpers;
using CSharpMiner.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WpfMiner
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        AsyncOperation asyncOp;

        private Dictionary<IMiningDevice, MiningDeviceData> _deviceMap;
        private Dictionary<IMiningDevice, MiningDeviceData> DeviceMap
        {
            get
            {
                if (_deviceMap == null)
                    _deviceMap = new Dictionary<IMiningDevice, MiningDeviceData>();

                return _deviceMap;
            }
        }

        private ObservableCollection<MiningDeviceData> _devices;
        public ObservableCollection<MiningDeviceData> Devices
        {
            get
            {
                if (_devices == null)
                    _devices = new ObservableCollection<MiningDeviceData>(new List<MiningDeviceData>());

                return _devices;
            }
        }

        private Observable<string> _acceptedHashrate;
        public Observable<string> AcceptedHashrate
        {
            get
            {
                if (_acceptedHashrate == null)
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

        private Observable<int> _days;
        public Observable<int> Days
        {
            get
            {
                if (_days == null)
                    _days = new Observable<int>(0);

                return _days;
            }
        }

        private Observable<int> _hours;
        public Observable<int> Hours
        {
            get
            {
                if (_hours == null)
                    _hours = new Observable<int>(0);

                return _hours;
            }
        }

        private Observable<int> _minutes;
        public Observable<int> Minutes
        {
            get
            {
                if (_minutes == null)
                    _minutes = new Observable<int>(0);

                return _minutes;
            }
        }

        private Observable<int> _sec;
        public Observable<int> Seconds
        {
            get
            {
                if (_sec == null)
                    _sec = new Observable<int>(0);

                return _sec;
            }
        }

        private System.Timers.Timer _timer;

        public static Miner Miner { get; private set; }

        public static Thread MiningThread { get; private set; }

        public MainWindow()
        {
            InitializeComponent();

            this.asyncOp = AsyncOperationManager.CreateOperation(null);

            Miner = ConsoleMiner.Miner;

            Miner.DeviceConnected += Miner_DeviceConnected;
            Miner.DeviceDisconnected += Miner_DeviceDisconnected;
            Miner.WorkAccepted += Miner_WorkAccepted;
            Miner.WorkRejected += Miner_WorkRejected;
            Miner.WorkDiscarded += Miner_WorkDiscarded;

            MiningThread = new Thread(new ThreadStart(() =>
            {
                LogHelper.ErrorLogFilePath = "err.log";

#if DEBUG
                LogHelper.Verbosity = LogVerbosity.Verbose;
#else
                LogHelper.Verbosity = LogVerbosity.Normal;
#endif

                LogHelper.StartConsoleLogThread();
                LogHelper.StartFileLogThread();

                ConsoleMiner.RunFromConsole(App.Args);

                LogHelper.StopFileLogThread();
                LogHelper.StopConsoleLogThread();
            }));
            MiningThread.Start();

            _timer = new System.Timers.Timer(500);
            _timer.Elapsed += Timer_Elapsed;
            _timer.Start();
        }

        int count = 0;
        void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            TimeSpan totalElapsed = DateTime.Now - Process.GetCurrentProcess().StartTime;

            if (Days.Value != totalElapsed.Days)
                Days.Value = totalElapsed.Days;

            if (Hours.Value != totalElapsed.Hours)
                Hours.Value = totalElapsed.Hours;

            if (Minutes.Value != totalElapsed.Minutes)
                Minutes.Value = totalElapsed.Minutes;

            if (Seconds.Value != totalElapsed.Seconds)
                Seconds.Value = totalElapsed.Seconds;

            if (count == 1 && Miner.MiningManagers != null)
            {
                var manager = Miner.MiningManagers.FirstOrDefault();

                if (manager != null && manager.ActivePools != null)
                {
                    IPool p = manager.ActivePools.FirstOrDefault();

                    if (p != null)
                    {
                        UpdateHashrate(p);
                    }
                }

                count = 0;
            }
            else
            {
                count++;
            }
        }

        private void UpdateHashrate(IPool p)
        {
            double acceptedHash = p.AcceptedHashRate;
            double rejectedHash = p.RejectedHashRate;
            double discardedHash = p.DiscardedHashRate;

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
        }

        void Miner_WorkDiscarded(IPool arg1, IPoolWork arg2, IMiningDevice arg3)
        {
            this.UpdateHashrate(arg1);

            MiningDeviceData data;
            if(DeviceMap.TryGetValue(arg3, out data))
            {
                data.UpdatedHashRate(arg3);
            }
        }

        void Miner_WorkRejected(IPool arg1, IPoolWork arg2, IMiningDevice arg3, IShareResponse arg4)
        {
            this.UpdateHashrate(arg1);

            MiningDeviceData data;
            if (DeviceMap.TryGetValue(arg3, out data))
            {
                data.UpdatedHashRate(arg3);
            }
        }

        void Miner_WorkAccepted(IPool arg1, IPoolWork arg2, IMiningDevice arg3)
        {
            this.UpdateHashrate(arg1);

            MiningDeviceData data;
            if (DeviceMap.TryGetValue(arg3, out data))
            {
                data.UpdatedHashRate(arg3);
            }
        }

        void Miner_DeviceDisconnected(IMiningDeviceManager arg1, IMiningDevice arg2)
        {
            asyncOp.Post(this.RemoveDevice, new Tuple<IMiningDeviceManager, IMiningDevice>(arg1, arg2));
        }

        void Miner_DeviceConnected(IMiningDeviceManager arg1, IMiningDevice arg2)
        {
            asyncOp.Post(this.UpdateDevices, new Tuple<IMiningDeviceManager, IMiningDevice>(arg1, arg2));
        }

        private void RemoveDevice(object obj)
        {
            Tuple<IMiningDeviceManager, IMiningDevice> t = obj as Tuple<IMiningDeviceManager, IMiningDevice>;

            MiningDeviceData data;

            if (DeviceMap.TryGetValue(t.Item2, out data))
            {
                data.Done();

                Devices.Remove(data);
                DeviceMap.Remove(t.Item2);
            }
        }

        private void UpdateDevices(object obj)
        {
            Tuple<IMiningDeviceManager, IMiningDevice> t = obj as Tuple<IMiningDeviceManager, IMiningDevice>;

            var arg1 = t.Item1;
            var arg2 = t.Item2;

            if (arg1 != null && arg1.Devices != null)
            {
                if (Devices.Count != arg1.Devices.Count() - 1)
                {
                    foreach (IMiningDevice d in arg1.Devices)
                    {
                        if (!DeviceMap.ContainsKey(d))
                        {
                            MiningDeviceData data = new MiningDeviceData(d);

                            Devices.Add(data);

                            DeviceMap.Add(d, data);
                        }
                    }
                }
                else if (arg2 != null)
                {
                    if (!DeviceMap.ContainsKey(arg2))
                    {
                        MiningDeviceData data = new MiningDeviceData(arg2);

                        Devices.Add(data);

                        DeviceMap.Add(arg2, data);
                    }
                }
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Miner.DeviceConnected -= Miner_DeviceConnected;
            Miner.DeviceDisconnected -= Miner_DeviceDisconnected;
            Miner.WorkAccepted -= Miner_WorkAccepted;
            Miner.WorkRejected -= Miner_WorkRejected;
            Miner.WorkDiscarded -= Miner_WorkDiscarded;

            _timer.Stop();
            _timer.Elapsed -= Timer_Elapsed;

            ConsoleMiner.StopMining = true;
            MiningThread.Join(1000);
            Miner.Stop();
            MiningThread.Abort();
        }
    }
}
