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
using CSharpMiner.ModuleLoading;
using System;
using System.Collections;
using System.IO;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Stratum
{
    [DataContract]
    [MiningModule(Description="Manages a connection to a stratum mining pool.")]
    public class StratumPool : IPool
    {
        public const string StratumPrefix = "stratum+tcp";

        [DataMember(Name = "url", IsRequired=true)]
        [MiningSetting(ExampleValue = "stratum+tcp://www.somewhere.com:4444", Optional=false, Description="The URL and port of the mining server.")]
        public string Url { get; set; }

        [DataMember(Name = "user", IsRequired=true)]
        [MiningSetting(ExampleValue="SomeUser", Optional=false, Description="The username to connect with. Could be a wallet address.")]
        public string Username { get; set; }

        [DataMember(Name = "pass")]
        [MiningSetting(ExampleValue="pass", Optional=true, Description="Password. If none is specified 'x' is used.")]
        public string Password { get; set; }

        [IgnoreDataMember]
        public bool Alive { get; private set; }

        [IgnoreDataMember]
        public int Accepted { get; set; }

        [IgnoreDataMember]
        public int Rejected { get; set; }

        [IgnoreDataMember]
        public int AcceptedWorkUnits { get; private set; }

        [IgnoreDataMember]
        public int RejectedWorkUnits { get; set; }

        [IgnoreDataMember]
        public int DiscardedWorkUnits { get; set; }

        [IgnoreDataMember]
        public int NewBlocks { get; private set; }

        [IgnoreDataMember]
        public int Diff { get; private set; }

        [IgnoreDataMember]
        public int RequestId { get; private set; }

        [IgnoreDataMember]
        public string Extranonce1 { get; private set; }

        [IgnoreDataMember]
        public int Extranonce2Size { get; private set; }

        [IgnoreDataMember]
        public bool Running { get; set; }

        [IgnoreDataMember]
        public Thread Thread { get; private set; }

        [IgnoreDataMember]
        public bool Connected
        {
            get
            {
                if(this.connection != null)
                {
                    return this.connection.Connected;
                }
                else
                {
                    return false;
                }
            }
        }

        [IgnoreDataMember]
        public bool Connecting { get; private set; }

        [IgnoreDataMember]
        public int HardwareErrors { get; set; }

        public event Action<IPool, IPoolWork, bool> NewWorkRecieved;
        public event Action<IPool> Disconnected;
        public event Action<IPool, IPoolWork, IMiningDevice> WorkAccepted;
        public event Action<IPool, IPoolWork, IMiningDevice, string> WorkRejected;

        private Queue WorkSubmitQueue = null;
        private TcpClient connection = null;
        private StratumWork pendingWork = null;

        private CancellationTokenSource threadStopping = null;

        private StratumWork latestWork = null;

        private Object submissionLock = null;

        private bool _allowOldWork = true;

        private string partialData = null;

        private DateTime start;

        public StratumPool()
            : this("", "", "")
        {
        }

        public StratumPool(string url, string username, string password)
        {
            Url = url;
            Username = username;
            Password = password;
            Alive = false;
            Accepted = 0;
            Rejected = 0;
        }

        public void Start()
        {
            if (!this.Running && !this.Connecting)
            {
                start = DateTime.Now;
                Accepted = 0;
                Rejected = 0;
                HardwareErrors = 0;
                AcceptedWorkUnits = 0;
                RejectedWorkUnits = 0;
                DiscardedWorkUnits = 0;

                submissionLock = new Object();
                _writeLock = new Object();

                this.Connecting = true;

                if (this.Thread == null)
                {
                    threadStopping = new CancellationTokenSource();

                    Task.Factory.StartNew(this.Connect);
                }
            }
        }

        public void Stop()
        {
            if (this.WorkSubmitQueue != null)
            {
                this.WorkSubmitQueue.Clear();
            }

            this.Running = false;

            if (this.threadStopping != null)
            {
                this.threadStopping.Cancel();
            }

            try
            {
                if (this.Thread != null)
                {
                    this.Thread.Join(200);
                    this.Thread.Abort();
                }
            }
            finally
            {
                this.Thread = null;
            }

            if (connection != null)
                connection.Close();

            connection = null;

            latestWork = null;

            this.Connecting = false;
            this.Alive = false;

            _allowOldWork = true;
            latestWork = null;
            pendingWork = null;

            this.Diff = 0;
            this.RequestId = 0;
            this.Extranonce1 = null;
            this.Extranonce2Size = 0;

            this.partialData = null;

            WorkSubmitQueue = null;
        }

        private void Connect()
        {
            try
            {
                if (connection != null)
                {
                    this.Stop();
                    connection = null;
                }

                WorkSubmitQueue = Queue.Synchronized(new Queue());

                this.Running = true;
                this.Alive = false;

                this._allowOldWork = true;
                this.latestWork = null;

                string[] splitAddress = Url.Split(':');

                if (splitAddress.Length != 3)
                {
                    Exception e = new StratumConnectionFailureException(string.Format("Incorrect pool address: {0}", Url));
                    LogHelper.LogErrorSecondaryAsync(e);
                    throw e;
                }

                string hostName = splitAddress[1].Replace("/", "").Trim();

                int port;
                if (!int.TryParse(splitAddress[2], out port))
                {
                    Exception e = new StratumConnectionFailureException(string.Format("Incorrect port format: {0}", splitAddress[1]));
                    LogHelper.LogErrorSecondaryAsync(e);
                    throw e;
                }

                try
                {
                    connection = new TcpClient(hostName, port);
                }
                catch (SocketException e)
                {
                    throw new StratumConnectionFailureException(e);
                }

                this.Connecting = false;

                if (!connection.Connected)
                {
                    Exception e = new StratumConnectionFailureException("Unknown connection failure.");
                    LogHelper.LogErrorSecondaryAsync(e);
                    throw e;
                }

                StratumCommand subscribeCommand = StratumCommand.SubscribeCommand;
                subscribeCommand.Id = this.RequestId;

                MemoryStream memStream = new MemoryStream();
                subscribeCommand.Serialize(memStream);
                this.SendData(memStream);

                StratumResponse response = this.waitForResponse();

                Object[] data = (response != null ? response.Data as Object[] : null);

                if(data == null)
                {
                    throw new StratumConnectionFailureException("Recieved null response from server subscription command.");
                }

                this.Extranonce1 = data[1] as String;
                this.Extranonce2Size = (int)data[2];

                // If we recieved work before we started the device manager, give the work to the device manager now
                if (pendingWork != null)
                {
                    this.OnNewWorkRecieved(pendingWork, true);
                    pendingWork = null;
                }

                if (connection.Connected)
                {
                    this.Alive = true;
                }

                LogHelper.ConsoleLog(string.Format("Successfully connected to pool {0}", this.Url));

                LogHelper.DebugConsoleLogAsync(new Object[] {
                        string.Format("Extranonce1: {0}", data[1]),
                        string.Format("Extranonce2_size: {0}", data[2])
                    },
                    LogVerbosity.Verbose);

                string[] param = { this.Username, (!string.IsNullOrEmpty(this.Password) ? this.Password : "x") };

                StratumCommand command = new StratumCommand(this.RequestId, StratumCommand.AuthorizationCommandString, param);
                memStream = new MemoryStream();
                command.Serialize(memStream);
                this.SendData(memStream);

                StratumResponse successResponse = this.waitForResponse();

                if (successResponse.Data == null || !successResponse.Data.Equals(true))
                {
                    this.Alive = false;

                    LogHelper.ConsoleLogError(string.Format("Pool Username or Password rejected with: {0}", successResponse.Error));

                    throw new StratumConnectionFailureException(string.Format("Pool Username or Password rejected with: {0}", successResponse.Error));
                }

                this.Thread = new Thread(new ThreadStart(() =>
                {
                    try
                    {
                        // Enter loop to monitor pool stratum
                        while (this.Running)
                        {
                            this.processCommands(this.listenForData());
                        }
                    }
                    catch (Exception e)
                    {
                        LogHelper.LogErrorSecondaryAsync(e);

                        Task.Factory.StartNew(() =>
                            {
                                this.Stop();
                                this.OnDisconnect();
                            });
                    }
                }));
                this.Thread.Start();
            }
            catch (Exception e)
            {
                this.Connecting = false;

                LogHelper.LogErrorSecondaryAsync(e);

                this.Stop();
                this.OnDisconnect();
            }
        }

        private void OnNewWorkRecieved(StratumWork work, bool forceRestart)
        {
            if (NewWorkRecieved != null)
            {
                Task.Factory.StartNew(() =>
                {
                    NewWorkRecieved(this, work, forceRestart);
                });
            }
        }

        private void OnDisconnect()
        {
            if (this.Disconnected != null)
            {
                Task.Factory.StartNew(() =>
                {
                    this.Disconnected(this);
                });
            }
        }

        private void OnWorkAccepted(StratumWork work, IMiningDevice device, bool async = true)
        {
            if (this.WorkAccepted != null)
            {
                if (async)
                {
                    Task.Factory.StartNew(() =>
                        {
                            this.WorkAccepted(this, work, device);
                        });
                }
                else
                {
                    this.WorkAccepted(this, work, device);
                }
            }
        }

        private void OnWorkRejected(StratumWork work, IMiningDevice device, string reason, bool async = true)
        {
            if (this.WorkRejected != null)
            {
                if (async)
                {
                    Task.Factory.StartNew(() =>
                        {
                            this.WorkRejected(this, work, device, reason);
                        });
                }
                else
                {
                    this.WorkRejected(this, work, device, reason);
                }
            }
        }

        private object _writeLock = null;
        private void SendData(MemoryStream stream)
        {
            if(_writeLock != null && connection != null && connection.Connected)
            {
                lock(_writeLock)
                {
                    stream.WriteTo(connection.GetStream());
                }
            }
        }

        public void SubmitWork(StratumWork work, IMiningDevice device, string nonce)
        {
            if(this.connection == null || !this.connection.Connected)
            {
                LogHelper.ConsoleLogErrorAsync("Attempting to submit share to disconnected pool.");
                return;
            }

            if(!this._allowOldWork && (this.latestWork == null || work.JobId != this.latestWork.JobId))
            {
                LogHelper.ConsoleLogAsync(string.Format("Discarding share for old job {0}.", work.JobId), ConsoleColor.Magenta, LogVerbosity.Verbose);
                return;
            }

            if (WorkSubmitQueue != null && submissionLock != null)
            {
                try
                {
                    string[] param = { this.Username, work.JobId, work.Extranonce2, work.Timestamp, nonce };
                    StratumCommand command = null;

                    lock (submissionLock)
                    {
                        command = new StratumCommand(this.RequestId, StratumCommand.SubmitCommandString, param);
                        this.RequestId++;

                        if (this.connection != null && this.connection.Connected)
                        {
                            MemoryStream memStream = new MemoryStream();
                            command.Serialize(memStream);
                            this.SendData(memStream);
                        }
                    }

                    if (command != null)
                    {
                        WorkSubmitQueue.Enqueue(new Tuple<StratumCommand, StratumWork, IMiningDevice>(command, work, device));
                    }
                }
                catch (Exception e)
                {
                    LogHelper.LogErrorSecondaryAsync(e);

                    if ((this.connection == null || !this.connection.Connected) && this.Running)
                    {
                        Task.Factory.StartNew(() =>
                            {
                                this.Stop();

                                this.OnDisconnect();
                            });
                    }
                }
            }
        }

        private void processCommands(string allCommands)
        {
            string[] responses = allCommands.Split('\n');
            this.processCommands(responses, this.RequestId);
        }

        private StratumResponse processCommands(string[] commands, int id = -1)
        {
            StratumResponse result = null;

            foreach (string s in commands)
            {
                if (!string.IsNullOrWhiteSpace(s))
                {
                    if (!s.Trim().EndsWith("}"))
                    {
                        LogHelper.LogErrorSecondaryAsync(
                            new Object[] { 
                            string.Format("Partial command recieved from {0}", this.Url),
                            s
                        });

                        partialData = s;

                        continue;
                    }

                    LogHelper.DebugConsoleLog(new Object[] {
                            string.Format("Recieved data from {0}:", this.Url),
                            s
                        }, LogVerbosity.Verbose);

                    if (!string.IsNullOrEmpty(s.Trim()))
                    {
                        string str = s; // Attempt to convert this to a friendly format for Mono

                        MemoryStream memStream = new MemoryStream(Encoding.ASCII.GetBytes(str));

                        if (str.Contains("\"result\""))
                        {
                            StratumResponse response = null;

                            try
                            {
                                try
                                {
                                    response = StratumResponse.Deserialize(memStream);
                                }
                                catch
                                {
                                    LogHelper.DebugLogErrorSecondaryAsync(string.Format("Failing over to manual parsing. Could not deserialize:\n\r {0}", str));
                                    response = new StratumResponse(str);
                                }
                            }
                            catch (Exception e)
                            {
                                Exception exception = new InvalidDataException(string.Format("Error parsing response {0}", str), e);
                                LogHelper.LogErrorSecondaryAsync(exception);
                                throw exception;
                            }

                            // This is the response we're looking for
                            if (response.Id == id)
                            {
                                result = response;
                            }
                            else // This should be a work submit response. We expect these to come back in order
                            {
                                if (WorkSubmitQueue != null)
                                {
                                    if (WorkSubmitQueue.Count > 0)
                                    {
                                        Tuple<StratumCommand, StratumWork, IMiningDevice> workItem = WorkSubmitQueue.Peek() as Tuple<StratumCommand, StratumWork, IMiningDevice>;

                                        if (response.Id == workItem.Item1.Id)
                                        {
                                            processWorkAcceptCommand(workItem.Item2, workItem.Item3, response);
                                            WorkSubmitQueue.Dequeue();
                                        }
                                        else if (response.Id > workItem.Item1.Id) // Something odd happened, we probably missed some responses or the server decided not to send them
                                        {
                                            workItem = WorkSubmitQueue.Peek() as Tuple<StratumCommand, StratumWork, IMiningDevice>;

                                            while (WorkSubmitQueue.Count > 0 && response.Id > workItem.Item1.Id)
                                            {
                                                // Get rid of the old stuff
                                                WorkSubmitQueue.Dequeue();

                                                workItem = WorkSubmitQueue.Peek() as Tuple<StratumCommand, StratumWork, IMiningDevice>;
                                            }

                                            if (WorkSubmitQueue.Count > 0 && response.Id == ((Tuple<StratumCommand, StratumWork, IMiningDevice>)WorkSubmitQueue.Peek()).Item1.Id)
                                            {
                                                workItem = WorkSubmitQueue.Dequeue() as Tuple<StratumCommand, StratumWork, IMiningDevice>;

                                                processWorkAcceptCommand(workItem.Item2, workItem.Item3, response);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        else // This is a command from the server
                        {
                            StratumCommand command = null;

                            try
                            {
                                try
                                {
                                    command = StratumCommand.Deserialize(memStream);
                                }
                                catch
                                {
                                    LogHelper.DebugLogErrorSecondary(string.Format("Failed to parse command. Falling back to manual parsing. Command\r\n {0}", str));
                                    command = new StratumCommand(str);
                                }
                            }
                            catch (Exception e)
                            {
                                Exception exception = new InvalidDataException(string.Format("Error parsing command {0}", str), e);
                                LogHelper.LogErrorSecondaryAsync(exception);
                                throw exception;
                            }

                            processCommand(command);
                        }
                    }
                }
            }

            return result;
        }

        private void processWorkAcceptCommand(StratumWork work, IMiningDevice device, StratumResponse response, bool error = false)
        {
            if(error)
            {
                LogHelper.DebugConsoleLogError("Error. Unknown work result. Mismatch in work queue ID and recieved response ID.");
                return;
            }

            bool accepted = response.Data != null && response.Data.Equals(true);

            if (accepted)
            {
                Accepted++;
                AcceptedWorkUnits += work.Diff;
            }
            else
            {
                Rejected++;
                RejectedWorkUnits += work.Diff;
            }

            Task.Factory.StartNew(() =>
                {
                    if (accepted)
                    {
                        this.OnWorkAccepted(work, device, false);
                    }
                    else
                    {
                        this.OnWorkRejected(work, device, GetRejectReason(response), false);
                    }

                    DisplaySubmissionResponse(accepted, response);
                });
        }

        private string GetRejectReason(StratumResponse response)
        {
            return (response.Error != null && response.Error.Length >= 2 ? response.Error[1].ToString() : string.Empty);
        }

        private double ComputeHashRate(int workUnits)
        {
            return 65535.0 * workUnits / DateTime.Now.Subtract(start).TotalSeconds; // Expected hashes per work unit * work units / sec = hashes per sec
        }

        private string MegaHashDisplayString(double hashesPerSec)
        {
            double mHash = hashesPerSec / 1000000;

            return string.Format("{0:N2}Mh", mHash);
        }

        private void DisplaySubmissionResponse(bool accepted, StratumResponse response)
        {
            LogHelper.ConsoleLogAsync(new Object[] {
                new Object[] {(accepted? "" : string.Format("Rejected with {0}", GetRejectReason(response))), ConsoleColor.Magenta, !accepted},
                new Object[] { (accepted ? "ACCEPTED" : "REJECTED"), (accepted ? ConsoleColor.Green : ConsoleColor.Red), false },
                new Object[] { " ( ", false },
                new Object[] { this.Accepted, ConsoleColor.Green, false },
                new Object[] { " : ", false},
                new Object[] { this.Rejected, ConsoleColor.Red, false },
                new Object[] { " : ", false},
                new Object[] { this.HardwareErrors, ConsoleColor.Magenta, false },
                new Object[] {" ) ", false},
                new Object[] {" ( ", false},
                new Object[] {MegaHashDisplayString(ComputeHashRate(this.AcceptedWorkUnits)), ConsoleColor.Green, false},
                new Object[] {" : ", false},
                new Object[] {MegaHashDisplayString(ComputeHashRate(this.RejectedWorkUnits)), ConsoleColor.Red, false},
                new Object[] {" : ", false},
                new Object[] {MegaHashDisplayString(ComputeHashRate(this.DiscardedWorkUnits)), ConsoleColor.Magenta, false},
                new Object[] {" )", true}
            });
        }

        private void processCommand(StratumCommand command)
        {
            LogHelper.DebugConsoleLogAsync(string.Format("Command: {0}", command.Method), LogVerbosity.Verbose);

            switch(command.Method.Trim())
            {
                case StratumCommand.NotifyCommandString:
                    LogHelper.ConsoleLogAsync(string.Format("Got Work from {0}!", this.Url), LogVerbosity.Verbose);

                    if (command.Params.Length >= 9 && command.Params[8] != null && command.Params[8].Equals(true))
                    {
                        this.NewBlocks++;
                        LogHelper.ConsoleLogAsync(string.Format("New block! ({0})", this.NewBlocks), ConsoleColor.DarkYellow, LogVerbosity.Verbose);
                    }

                    StratumWork work = new StratumWork(command.Params, this.Extranonce1, "00000000", this.Diff);

                    if (this.Alive && this.NewWorkRecieved != null)
                    {
                        bool forceRestart = (command.Params != null && command.Params.Length >= 9 && command.Params[8] != null && command.Params[8] is string ? command.Params[8].Equals(true) : true);

                        this.OnNewWorkRecieved(work, forceRestart);
                    }
                    else
                    {
                        pendingWork = work;
                    }
                    break;

                case StratumCommand.SetDifficlutyCommandString:
                    LogHelper.ConsoleLogAsync(string.Format("Got Diff: {0} from {1}", command.Params[0], this.Url), LogVerbosity.Verbose);

                    this.Diff = (int)command.Params[0];
                    break;

                default:
                    LogHelper.ConsoleLogErrorAsync(string.Format("Unrecognized command: {0}", command.Method));
                    break;
            }
        }

        private string listenForData()
        {
            if (this.threadStopping != null && this.connection != null && this.connection.Connected && this.Running)
            {
                // TODO: Handle null connection
                byte[] arr = new byte[10000];
                Task<int> asyncTask = null;

                try
                {
                    asyncTask = connection.GetStream().ReadAsync(arr, 0, 10000, this.threadStopping.Token);
                    asyncTask.Wait(this.threadStopping.Token);
                } catch(OperationCanceledException e)
                {
                    LogHelper.LogErrorSecondaryAsync(e);
                    return string.Empty;
                }
                catch (AggregateException e)
                {
                    LogHelper.LogErrorSecondaryAsync(e);
                    return string.Empty;
                }

                if (asyncTask != null && !asyncTask.IsCanceled)
                {
                    int bytesRead = asyncTask.Result;

                    if (string.IsNullOrEmpty(partialData))
                    {
                        return Encoding.ASCII.GetString(arr, 0, bytesRead);
                    }
                    else
                    {
                        string result = Encoding.ASCII.GetString(arr, 0, bytesRead).Trim();
                        
                        // Guard against messing up good commands because a bad partial command was recieved
                        if (!result.StartsWith("{"))
                        {
                            result = partialData + result;
                        }

                        partialData = null;

                        return result;
                    }
                }
            }

            return string.Empty;
        }

        private StratumResponse waitForResponse()
        {
            // TODO: handle null connection
            StratumResponse response = null;
            string responseStr = "";

            // TODO: Make this not an infinate loop
            while (response == null && connection != null && connection.Connected)
            {
                responseStr = this.listenForData();

                string[] responses = responseStr.Split('\n');
                response = this.processCommands(responses, this.RequestId);

                if (response != null)
                {
                    this.RequestId++;
                }
            }

            return response;
        }

        public void Dispose()
        {
            if(this.Running)
            {
                this.Stop();
            }
        }

        public void SubmitWork(IPoolWork work, object workData)
        {
            StratumWork stratumWork = work as StratumWork;
            Object[] data = workData as Object[];

            if (stratumWork != null && data != null && data.Length >= 2)
            {
                IMiningDevice device = data[0] as IMiningDevice;
                string nonce = data[1] as string;

                if (device != null && nonce != null)
                {
                    this.SubmitWork(stratumWork, device, nonce);
                }
            }
        }
    }
}
