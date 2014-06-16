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
        public bool ShouldDisconnect { get; set; }

        [IgnoreDataMember]
        public Thread Thread { get; private set; }

        private Queue WorkSubmitIdQueue = Queue.Synchronized(new Queue());
        private TcpClient connection = null;

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

        public void Start()
        {
            if(this.Thread == null)
            {
                this.Thread = new Thread(new ThreadStart(this.Connect));
                this.Thread.Start();
            }
        }

        public void Stop()
        {
            this.ShouldDisconnect = true;
        }

        private void Connect()
        {
            this.ShouldDisconnect = false;
            this.Alive = false;

            if(connection != null)
            {
                connection.Close();
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

            if (connection.Connected)
            {
                this.Alive = true;
            }
            else
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

                this.Extranonce1 = data[1].ToString();
                this.Extranonce2Size = (int)data[2];

                Program.DebugConsoleLog(string.Format("Extranonce1: {0}", data[1]));
                Program.DebugConsoleLog(string.Format("Extranonce2_size: {0}", data[2]));

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
                connection.Close();
                throw;
            }

            // Enter loop to monitor pool stratum
            while(!this.ShouldDisconnect)
            {
                this.processCommands(this.listenForData());
            }

            connection.Close();
        }

        private void SubmitWork(string jobId, string extranonce2, string ntime, string nonce)
        {
            // TODO: handle null connection
            string[] param = {this.Username, jobId, extranonce2, ntime, nonce};
            Command command = new Command(this.RequestId, Command.SubmitCommandString, param);
            command.Serialize(this.connection.GetStream());

            WorkSubmitIdQueue.Enqueue(command);
            this.RequestId++;
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
                if (!string.IsNullOrEmpty(str.Trim()))
                {
                    MemoryStream memStream = new MemoryStream(Encoding.ASCII.GetBytes(str));

                    if (str.Contains("\"result\""))
                    {
                        Response response = Response.Deserialize(memStream);

                        // This is the response we're looking for
                        if (response.Id == id)
                        {
                            result = response;
                        }
                        else // This should be a work submit response. We expect these to come back in order
                        {
                            if(WorkSubmitIdQueue.Count > 0)
                            {
                                if(response.Id == ((Command)WorkSubmitIdQueue.Peek()).Id)
                                {
                                    processWorkAcceptCommand((Command)WorkSubmitIdQueue.Dequeue(), response);
                                }
                                else if(response.Id > ((Command)WorkSubmitIdQueue.Peek()).Id) // Something odd happened, we probably missed some responses or the server decided not to send them
                                {
                                    while(WorkSubmitIdQueue.Count > 0 && response.Id > ((Command)WorkSubmitIdQueue.Peek()).Id)
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
                    else // This is a command from the server
                    {
                        Command command = Command.Deserialize(memStream);
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

            // TODO: More info?

            Console.Write("Work {0}", response.Id);
            ConsoleColor defaultColor = Console.ForegroundColor;
            Console.ForegroundColor = (response.Data.Equals(true) ? ConsoleColor.Green : ConsoleColor.Red);
            Console.Write((response.Data.Equals(true) ? "ACCEPTED" : "REJECTED"));
            Console.ForegroundColor = defaultColor;
            Console.WriteLine(".");
        }

        private void processCommand(Command command)
        {
            Program.DebugConsoleLog(string.Format("Command: {0}", command.Method));

            switch(command.Method)
            {
                case Command.NotifyCommandString:
                    Program.DebugConsoleLog(string.Format("Got Work from {0}!", this.Url));

                    // TODO: Make use work
                    break;

                case Command.SetDifficlutyCommandString:
                    Program.DebugConsoleLog(string.Format("Got Diff: {0} from {1}", command.Params[0], this.Url));

                    this.Diff = (int)command.Params[0];
                    break;
            }
        }

        private string listenForData()
        {
            // TODO: Handle null connection
            byte[] arr = new byte[10000];
            int bytesRead = connection.GetStream().Read(arr, 0, arr.Length);
            return Encoding.ASCII.GetString(arr, 0, bytesRead);
        }

        private Response waitForResponse()
        {
            // TODO: handle null connection
            Response response = null;
            NetworkStream netStream = connection.GetStream();
            string responseStr = "";

            // TODO: Make this not an infinate loop
            while (response == null)
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
            if (connection != null)
                connection.Close();

            connection = null;
        }
    }
}
