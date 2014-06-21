using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace CSharpMiner.Stratum
{
    [DataContract]
    public class Command : SerializedJsonObjectBase
    {
        public static DataContractJsonSerializer _serializer = null;

        public static DataContractJsonSerializer SerializerObject
        {
            get
            {
                if (_serializer == null)
                    _serializer = new DataContractJsonSerializer(typeof(Command));

                return _serializer;
            }
        }

        public override DataContractJsonSerializer Serializer
        {
            get
            {
                return Command.SerializerObject;
            }
        }

        public static Command Deserialize(Stream stream)
        {
            return (Command)Command.SerializerObject.ReadObject(stream);
        }

        [DataMember(Name = "id")]
        public Object Id { get; set; }

        [DataMember(Name = "method")]
        public string Method { get; set; }

        [DataMember(Name = "params")]
        public Object[] Params { get; set; }

        public Command()
        {
        }

        public Command(int id, string method, string[] paramArr)
        {
            Id = id;
            Method = method;
            Params = paramArr;
        }

        public const string SubscribeCommandString = "mining.subscribe";
        public const string AuthorizationCommandString = "mining.authorize";
        public const string NotifyCommandString = "mining.notify";
        public const string SubmitCommandString = "mining.submit";
        public const string SetDifficlutyCommandString = "mining.set_difficulty";
        public const string ClientReconnectCommandString = "client.reconnect";
        public const string ClientGetVersionCommandString = "client.get_version";
        public const string ClientShowMessageCommandString = "client.show_message";

        public static Command SubscribeCommand 
        {
            get
            {
                return new Command(1, SubscribeCommandString, Command.SubscribeParamArray);
            }
        }

        private static string[] _emptyParamArray = null;

        public static string[] EmptyParamArray
        {
            get
            {
                if (_emptyParamArray == null)
                    _emptyParamArray = new string[0];

                return _emptyParamArray;
            }
        }

        private static string[] _subscribeParamArray = null;

        public static string[] SubscribeParamArray
        {
            get
            {
                if (_subscribeParamArray == null)
                {
                    _subscribeParamArray = new string[1];
                    _subscribeParamArray[0] = string.Format("CSharpMiner_{0}", CSharpMiner.Program.VersionString);
                }

                return _subscribeParamArray;
            }
        }
    }
}
