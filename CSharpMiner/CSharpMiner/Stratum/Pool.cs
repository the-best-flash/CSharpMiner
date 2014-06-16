using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace CSharpMiner.Stratum
{
    [DataContract]
    public class Pool
    {
        [DataMember(Name = "url")]
        public string Url { get; set; }

        [DataMember(Name = "username")]
        public string Username { get; set; }

        [DataMember(Name = "password")]
        public string Password { get; set; }

        [IgnoreDataMember]
        public bool Alive { get; private set; }

        [IgnoreDataMember]
        public int Accepted { get; private set; }

        [IgnoreDataMember]
        public int Rejected { get; private set; }

        [IgnoreDataMember]
        public event PoolWorkDelegate NewWork;

        private TcpClient connection;

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

        public void Connect()
        {
            if(connection != null)
            {
                connection.Close();
            }

            string[] splitAddress = Url.Split(':');

            if(splitAddress.Length != 2)
            {
                throw new StratumConnectionFailureException(string.Format("Incorrect pool address: {0}", Url));
            }

            string hostName = splitAddress[0];
            
            int port;
            if(!int.TryParse(splitAddress[1], out port))
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
        }
    }
}
