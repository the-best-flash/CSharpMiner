using CSharpMiner.Helpers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CSharpMiner.Stratum
{
    [DataContract]
    public class Pool : IDisposable
    {
        public const string StratumPrefix = "stratum+tcp";

        [DataMember(Name = "url")]
        public string Url { get; set; }

        [DataMember(Name = "user")]
        public string Username { get; set; }

        [DataMember(Name = "pass")]
        public string Password { get; set; }

        [IgnoreDataMember]
        public bool Alive { get; private set; }

        [IgnoreDataMember]
        public int Accepted { get; private set; }

        [IgnoreDataMember]
        public int Rejected { get; private set; }

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

        private Queue WorkSubmitIdQueue = null;
        private TcpClient connection = null;
        private Object[] pendingWork = null;
        private Action<Object[], int> _newWork = null;
        private CancellationTokenSource threadStopping = null;
        private Action _disconnected = null;

        private PoolWork latestWork = null;

        private Object submissionLock = null;

        private bool _allowOldWork = true;

        private Tuple<Object[], int> mostRecentWork = null;
        private Tuple<Object[], int> mostRecentWorkCopy = null;

        public Pool()
            : this("", "", "")
        {
        }

        public Pool(string url, string username, string password)
        {
            Url = url;
            Username = username;
            Password = password;
            Alive = false;
            Accepted = 0;
            Rejected = 0;
        }

        public void Start(Action<Object[], int> newWork, Action disconnected)
        {
            submissionLock = new Object();

            this.Connecting = false;

            if(newWork == null)
            {
                throw new ArgumentNullException("newWork");
            }

            if(disconnected == null)
            {
                throw new ArgumentNullException("disconnected");
            }

            if(this.Thread == null)
            {
                threadStopping = new CancellationTokenSource();

                _newWork = newWork;
                _disconnected = disconnected;
                this.Thread = new Thread(new ThreadStart(this.Connect));
                this.Thread.Start();
            }
        }

        public void Stop()
        {
            if (this.Running)
            {
                this.Running = false;

                if (this.threadStopping != null)
                {
                    this.threadStopping.Cancel();
                }

                if (connection != null)
                    connection.Close();

                connection = null;

                latestWork = null;

                if (this.Thread != null)
                    this.Thread.Join();

                this.Thread = null;
            }
        }

        private void Connect()
        {
            try
            {
                this.Connecting = true;

                try
                {
                    WorkSubmitIdQueue = Queue.Synchronized(new Queue());
                    this.NewBlocks = 0;

                    this.Running = true;
                    this.Alive = false;

                    this._allowOldWork = true;
                    this.latestWork = null;

                    if (connection != null)
                    {
                        this.Stop();
                        connection = null;
                    }

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
                        Exception exception = new StratumConnectionFailureException(e);
                        LogHelper.LogErrorSecondaryAsync(exception);
                        throw exception;
                    }
                }
                catch
                {
                    throw;
                }
                finally
                {
                    this.Connecting = false;
                }

                if (!connection.Connected)
                {
                    Exception e = new StratumConnectionFailureException("Unknown connection failure.");
                    LogHelper.LogErrorSecondaryAsync(e);
                    throw e;
                }

                NetworkStream netStream = connection.GetStream();

                Command subscribeCommand = Command.SubscribeCommand;
                subscribeCommand.Id = this.RequestId;

                subscribeCommand.Serialize(netStream);

                Response response = this.waitForResponse();

                Object[] data = response.Data as Object[];

                this.Extranonce1 = data[1] as String;
                this.Extranonce2Size = (int)data[2];

                // If we recieved work before we started the device manager, give the work to the device manager now
                if (pendingWork != null)
                {
                    _newWork(pendingWork, this.Diff);
                    pendingWork = null;
                }

                if (connection.Connected)
                {
                    this.Alive = true;
                }

                LogHelper.DebugConsoleLogAsync(new Object[] {
                        string.Format("Extranonce1: {0}", data[1]),
                        string.Format("Extranonce2_size: {0}", data[2])
                    },
                    LogVerbosity.Verbose);

                string[] param = { this.Username, this.Password };

                Command command = new Command(this.RequestId, Command.AuthorizationCommandString, param);
                command.Serialize(netStream);

                Response successResponse = this.waitForResponse();

                if (!successResponse.Data.Equals(true))
                {
                    this.Alive = false;

                    LogHelper.ConsoleLogError(string.Format("Pool Username or Password rejected with: {0}", successResponse.Error));

                    throw new StratumConnectionFailureException(string.Format("Pool Username or Password rejected with: {0}", successResponse.Error));
                }

                // Enter loop to monitor pool stratum
                while (this.Running)
                {
                    this.processCommands(this.listenForData());
                }
            }
            catch (Exception e)
            {
                LogHelper.LogErrorSecondaryAsync(e);

                this.Stop();

                if (this._disconnected != null)
                {
                    this._disconnected();
                }
            }
        }

        public void SubmitWork(string jobId, string extranonce2, string ntime, string nonce)
        {
            if(this.connection == null || !this.connection.Connected)
            {
                LogHelper.ConsoleLogErrorAsync("Attempting to submit share to disconnected pool.");
                return;
            }

            if(!this._allowOldWork && (this.latestWork == null || jobId != this.latestWork.JobId))
            {
                LogHelper.ConsoleLogAsync(string.Format("Discarding share for old job {0}.", jobId), ConsoleColor.Magenta, LogVerbosity.Verbose);
                return;
            }

            if (WorkSubmitIdQueue != null && submissionLock != null)
            {
                try
                {
                    string[] param = { this.Username, jobId, extranonce2, ntime, nonce };
                    Command command = null;

                    lock (submissionLock)
                    {
                        command = new Command(this.RequestId, Command.SubmitCommandString, param);
                        this.RequestId++;

                        try
                        {
                            if (this.connection != null && this.connection.Connected)
                            {
                                command.Serialize(this.connection.GetStream());
                            }
                        }
                        catch (Exception e)
                        {
                            LogHelper.LogErrorSecondaryAsync(e);
                        }
                        finally
                        {
                            if ((this.connection == null || !this.connection.Connected) && this.Running)
                            {
                                if (this._disconnected != null)
                                {
                                    this.Stop();
                                    this._disconnected();
                                }
                            }
                        }
                    }

                    if (command != null)
                    {
                        WorkSubmitIdQueue.Enqueue(command);
                    }
                }
                catch (Exception e)
                {
                    LogHelper.LogErrorSecondaryAsync(e);
                }
            }
        }

        private void processCommands(string allCommands)
        {
            string[] responses = allCommands.Split('\n');
            this.processCommands(responses, this.RequestId);
        }

        private Response processCommands(string[] commands, int id = -1)
        {
            Response result = null;

            foreach (string str in commands)
            {
                LogHelper.DebugConsoleLog(new Object[] {
                    string.Format("Recieved data from {0}:", this.Url),
                    str
                }, LogVerbosity.Verbose);

                if (!string.IsNullOrEmpty(str.Trim()))
                {
                    MemoryStream memStream = new MemoryStream(Encoding.ASCII.GetBytes(str));

                    if (str.Contains("\"result\""))
                    {
                        Response response = null;

                        try
                        {
                            try
                            {
                                response = Response.Deserialize(memStream);
                            }
                            catch
                            {
                                response = new Response(str);
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
                            if (WorkSubmitIdQueue != null)
                            {
                                if (WorkSubmitIdQueue.Count > 0)
                                {
                                    if (response.Id == ((Command)WorkSubmitIdQueue.Peek()).Id)
                                    {
                                        processWorkAcceptCommand((Command)WorkSubmitIdQueue.Dequeue(), response);
                                    }
                                    else if (response.Id > ((Command)WorkSubmitIdQueue.Peek()).Id) // Something odd happened, we probably missed some responses or the server decided not to send them
                                    {
                                        while (WorkSubmitIdQueue.Count > 0 && response.Id > ((Command)WorkSubmitIdQueue.Peek()).Id)
                                        {
                                            // Get rid of the old stuff
                                            processWorkAcceptCommand((Command)WorkSubmitIdQueue.Dequeue(), response, true);
                                        }

                                        if (WorkSubmitIdQueue.Count > 0 && response.Id == ((Command)WorkSubmitIdQueue.Peek()).Id)
                                        {
                                            processWorkAcceptCommand((Command)WorkSubmitIdQueue.Dequeue(), response);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else // This is a command from the server
                    {
                        Command command = null;

                        try
                        {
                            try
                            {
                                command = Command.Deserialize(memStream);
                            }
                            catch
                            {
                                command = new Command(str);
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

            return result;
        }

        private void processWorkAcceptCommand(Command sentCommand, Response response, bool error = false)
        {
            if(error)
            {
                LogHelper.DebugConsoleLogAsync(string.Format("Error. Unknown work result. Mismatch in work queue ID and recieved response ID. Dequing waiting command ID {0} since we recieved response {1}.", sentCommand.Id, response.Id), ConsoleColor.Red);
                return;
            }

            if (response.Data != null && response.Data.Equals(true))
            {
                Accepted++;
            }
            else
            {
                string reason = (response.Error != null && response.Error.Length >= 2 ? response.Error[1] : "null").ToString();

                LogHelper.ConsoleLogAsync(string.Format("Rejected with {0}", reason), ConsoleColor.Magenta, LogVerbosity.Verbose);
                Rejected++;

                if(reason.ToLower().Trim() == "job not found")
                {
                    if(mostRecentWorkCopy != null)
                    {
                        _newWork(mostRecentWorkCopy.Item1, mostRecentWorkCopy.Item2);
                    }
                }
            }

            ConsoleColor color = (response.Data != null && response.Data.Equals(true) ? ConsoleColor.Green : ConsoleColor.Red);
            string resultStr = (response.Data != null && response.Data.Equals(true) ? "ACCEPTED" : "REJECTED");

            LogHelper.ConsoleLogAsync(new Object[] {
                new Object[] { resultStr, color, false },
                new Object[] { " ( ", false },
                new Object[] { this.Accepted, ConsoleColor.Green, false },
                new Object[] { " : ", false},
                new Object[] { this.Rejected, ConsoleColor.Red, false },
                new Object[] { " )", true }
            });
        }

        private void processCommand(Command command)
        {
            LogHelper.DebugConsoleLogAsync(string.Format("Command: {0}", command.Method), LogVerbosity.Verbose);

            switch(command.Method.Trim())
            {
                case Command.NotifyCommandString:
                    LogHelper.ConsoleLogAsync(string.Format("Got Work from {0}!", this.Url), LogVerbosity.Verbose);

                    if (command.Params.Length >= 9 && command.Params[8] != null && command.Params[8].Equals(true))
                    {
                        this.NewBlocks++;
                        LogHelper.ConsoleLogAsync(string.Format("New block! ({0})", this.NewBlocks), ConsoleColor.DarkYellow, LogVerbosity.Verbose);
                    }

                    if (this.Alive && this._newWork != null)
                    {
                        _newWork(command.Params, this.Diff);
                    }
                    else
                    {
                        pendingWork = command.Params;
                    }

                    mostRecentWork = new Tuple<object[], int>(command.Params, this.Diff);

                    Object[] copy = new Object[mostRecentWork.Item1.Length];
                    mostRecentWork.Item1.CopyTo(copy, 0);

                    if(copy.Length >= 9)
                        copy[8] = true;

                    mostRecentWorkCopy = new Tuple<object[], int>(copy, this.Diff);
                    break;

                case Command.SetDifficlutyCommandString:
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
            if (this.threadStopping != null && this.connection != null && this.connection.Connected)
            {
                // TODO: Handle null connection
                byte[] arr = new byte[10000];
                Task<int> asyncTask = null;

                try
                {
                    asyncTask = connection.GetStream().ReadAsync(arr, 0, 10000, this.threadStopping.Token);
                    asyncTask.Wait();
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
                    return Encoding.ASCII.GetString(arr, 0, bytesRead);
                }
            }

            return string.Empty;
        }

        private Response waitForResponse()
        {
            // TODO: handle null connection
            Response response = null;
            NetworkStream netStream = connection.GetStream();
            string responseStr = "";

            // TODO: Make this not an infinate loop
            while (response == null && connection != null && connection.Connected)
            {
                if (connection.Available != 0)
                {
                    byte[] arr = new byte[connection.Available];

                    netStream.Read(arr, 0, arr.Length);
                    responseStr = Encoding.ASCII.GetString(arr, 0, arr.Length);
                }
                else
                {
                    responseStr = this.listenForData();
                }

                string[] responses = responseStr.Split('\n');
                response = this.processCommands(responses, this.RequestId);

                if (response != null)
                {
                    this.RequestId++;
                }
            }

            return response;
        }

        void IDisposable.Dispose()
        {
            if(this.Running)
            {
                this.Stop();
            }
        }
    }
}
