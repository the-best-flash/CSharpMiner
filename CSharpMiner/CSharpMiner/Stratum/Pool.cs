using DeviceManager;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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

        private Queue WorkSubmitIdQueue = null;
        private TcpClient connection = null;
        private Object[] pendingWork = null;
        private Action<Object[], int> _newWork = null;
        private CancellationTokenSource threadStopping = null;
        private Action _disconnected = null;

        private Object submissionLock = null;

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

                if (_disconnected != null)
                {
                    _disconnected();
                }
            }
        }

        private void Connect()
        {
            WorkSubmitIdQueue = Queue.Synchronized(new Queue());

            this.Running = true;
            this.Alive = false;

            if(connection != null)
            {
                this.Stop();
            }

            string[] splitAddress = Url.Split(':');

            if(splitAddress.Length != 3)
            {
                throw new StratumConnectionFailureException(string.Format("Incorrect pool address: {0}", Url));
            }

            string hostName = splitAddress[1].Replace("/", "").Trim();
            
            int port;
            if(!int.TryParse(splitAddress[2], out port))
            {
                throw new StratumConnectionFailureException(string.Format("Incorrect port format: {0}", splitAddress[1]));
            }

            try
            {
                connection = new TcpClient(hostName, port);
            }
            catch(SocketException e)
            {
                throw new StratumConnectionFailureException(e);
            }

            if (!connection.Connected)
            {
                throw new StratumConnectionFailureException("Unknown connection failure.");
            }

            try
            {
                NetworkStream netStream = connection.GetStream();

                Command subscribeCommand = Command.SubscribeCommand;
                subscribeCommand.Id = this.RequestId;

                subscribeCommand.Serialize(netStream);

                Response response = this.waitForResponse();

                Object[] data = response.Data as Object[];

                this.Extranonce1 = data[1] as String;
                this.Extranonce2Size = (int)data[2];

                // If we recieved work before we started the device manager, give the work to the device manager now
                if(pendingWork != null)
                {
                    _newWork(pendingWork, this.Diff);
                    pendingWork = null;
                }

                if (connection.Connected)
                {
                    this.Alive = true;
                }

                #if DEBUG
                Program.DebugConsoleLog(string.Format("Extranonce1: {0}", data[1]));
                Program.DebugConsoleLog(string.Format("Extranonce2_size: {0}", data[2]));
                #endif

                string[] param = {this.Username, this.Password};

                Command command = new Command(this.RequestId, Command.AuthorizationCommandString, param);
                command.Serialize(netStream);

                Response successResponse = this.waitForResponse();

                if (!successResponse.Data.Equals(true))
                {
                    this.Alive = false;
                    throw new StratumConnectionFailureException(string.Format("Pool Username or Password rejected with: {0}", successResponse.Error));
                }
            }
            catch
            {
                this.Stop();
                throw;
            }

            // Enter loop to monitor pool stratum
            while(this.Running)
            {
                this.processCommands(this.listenForData());
            }
        }

        public void SubmitWork(string jobId, string extranonce2, string ntime, string nonce)
        {
            if (WorkSubmitIdQueue != null && submissionLock != null)
            {
                // TODO: handle null connection
                string[] param = { this.Username, jobId, extranonce2, ntime, nonce };
                Command command = null;

                lock (submissionLock)
                {
                    command = new Command(this.RequestId, Command.SubmitCommandString, param);
                    this.RequestId++;
                    command.Serialize(this.connection.GetStream());
                }

                if (command != null)
                {
                    WorkSubmitIdQueue.Enqueue(command);
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

            foreach (string s in commands)
            {
                if (!string.IsNullOrEmpty(s.Trim()))
                {
                    string str = s + "\n";
                    MemoryStream memStream = new MemoryStream(Encoding.ASCII.GetBytes(str));

                    if (str.Contains("\"result\""))
                    {
                        Response response = null;

                        try
                        {
                            response = Response.Deserialize(memStream);
                        }
                        catch
                        {
                            response = new Response(str);
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
                            command = Command.Deserialize(memStream);
                        }
                        catch
                        {
                            command = new Command(str);
                        }

                        if(command == null)
                        {
                            throw new InvalidDataException(string.Format("Error parsing command {0}", str));
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
                Program.DebugConsoleLog(string.Format("Error. Unknown work result. Mismatch in work queue ID and recieved response ID. Dequing waiting command ID {0} since we recieved response {1}.", sentCommand.Id, response.Id));
                return;
            }

            if (response.Data != null && response.Data.Equals(true))
            {
                Accepted++;
            }
            else
            {
                Program.DebugConsoleLog(string.Format("Rejected with {0}", (response.Error != null ? response.Error[1] : "null")));
                Rejected++;
            }

            ConsoleColor defaultColor = Console.ForegroundColor;
            Console.ForegroundColor = (response.Data != null && response.Data.Equals(true) ? ConsoleColor.Green : ConsoleColor.Red);
            Console.Write((response.Data != null && response.Data.Equals(true) ? "ACCEPTED" : "REJECTED"));
            Console.ForegroundColor = defaultColor;
            Console.Write(" ( ");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(this.Accepted);
            Console.ForegroundColor = defaultColor;
            Console.Write(" : ");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write(this.Rejected);
            Console.ForegroundColor = defaultColor;
            Console.WriteLine(" )");
        }

        private void processCommand(Command command)
        {
            #if DEBUG
            Program.DebugConsoleLog(string.Format("Command: {0}", command.Method));
            #endif

            switch(command.Method.Trim())
            {
                case Command.NotifyCommandString:
                    Program.DebugConsoleLog(string.Format("Got Work from {0}!", this.Url));

                    if (this.Alive && this._newWork != null)
                    {
                        _newWork(command.Params, this.Diff);
                    }
                    else
                    {
                        pendingWork = command.Params;
                    }
                    break;

                case Command.SetDifficlutyCommandString:
                    Program.DebugConsoleLog(string.Format("Got Diff: {0} from {1}", command.Params[0], this.Url));

                    this.Diff = (int)command.Params[0];
                    break;

                default:
                    Program.DebugConsoleLog(string.Format("Unrecognized command: {0}", command.Method));
                    break;
            }
        }

        private string listenForData()
        {
            if (this.threadStopping != null)
            {
                // TODO: Handle null connection
                byte[] arr = new byte[10000];
                Task<int> asyncTask = null;

                try
                {
                    asyncTask = connection.GetStream().ReadAsync(arr, 0, 10000, this.threadStopping.Token);
                    asyncTask.Wait();
                } catch(OperationCanceledException)
                {
                    return string.Empty;
                }
                catch (AggregateException)
                {
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
