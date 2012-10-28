using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using SuperSocket.Common;
using SuperSocket.Management.Server.Config;
using SuperSocket.Management.Server.Model;
using SuperSocket.SocketBase;
using SuperSocket.SocketBase.Config;
using SuperSocket.SocketBase.Protocol;
using SuperWebSocket;
using SuperWebSocket.Protocol;
using SuperWebSocket.SubProtocol;

namespace SuperSocket.Management.Server
{
    /// <summary>
    /// Server manager app server
    /// </summary>
    public class ManagementServer : WebSocketServer<ManagementSession>
    {
        private Dictionary<string, UserConfig> m_UsersDict;

        private string[] m_ExcludedServers;

        /// <summary>
        /// Initializes a new instance of the <see cref="ManagementServer"/> class.
        /// </summary>
        public ManagementServer()
            : base(new BasicSubProtocol<ManagementSession>("ServerManager"))
        {

        }


        /// <summary>
        /// Setups the specified root config.
        /// </summary>
        /// <param name="rootConfig">The root config.</param>
        /// <param name="config">The config.</param>
        /// <returns></returns>
        protected override bool Setup(IRootConfig rootConfig, IServerConfig config)
        {
            if (!base.Setup(rootConfig, config))
                return false;

            var users = config.GetChildConfig<UserConfigCollection>("users");

            if (users == null || users.Count <= 0)
            {
                Logger.Error("No user defined");
                return false;
            }

            m_UsersDict = new Dictionary<string, UserConfig>(StringComparer.OrdinalIgnoreCase);

            foreach (var u in users)
            {
                m_UsersDict.Add(u.Name, u);
            }

            m_ExcludedServers = config.Options.GetValue("excludedServers", string.Empty).Split(
                new char[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);

            return true;
        }


        internal NodeInfo CurrentNodeInfo { get; private set; }

        internal IWorkItem GetServerByName(string name)
        {
            return Bootstrap.AppServers.FirstOrDefault(i => name.Equals(i.Name, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Called when [server summary collected].
        /// </summary>
        /// <param name="nodeSummary">The node summary.</param>
        /// <param name="serverSummary">The server summary.</param>
        protected override void OnServerSummaryCollected(NodeSummary nodeSummary, ServerSummary serverSummary)
        {
            Async.AsyncRun(this, (o) => MergeServerSummary(o), nodeSummary);
            base.OnServerSummaryCollected(nodeSummary, serverSummary);
        }

        private void MergeServerSummary(object state)
        {
            var globalPerfData = state as NodeSummary;

            var instances = Bootstrap.AppServers.OfType<IWorkItem>().Where(s => !s.Name.Equals(this.Name, StringComparison.OrdinalIgnoreCase));
            
            if(m_ExcludedServers != null && m_ExcludedServers.Length > 0)
            {
                instances = instances.Where(s => !m_ExcludedServers.Contains(s.Name, StringComparer.OrdinalIgnoreCase));
            }

            CurrentNodeInfo = new NodeInfo
                {
                    GlobalInfo = globalPerfData,
                    Instances = instances.Select(s => s.Summary).ToArray()
                };

            if (StateFieldMetadatas == null)
            {
                StateFieldMetadatas = GetStateFieldMetadatas(CurrentNodeInfo.Instances);
            }

            BroadcastServerUpdate();
        }

        private void BroadcastServerUpdate()
        {
            var message = string.Format("{0} {1}", CommandName.UPDATE, JsonSerialize(CurrentNodeInfo));

            //Only push update to loged in sessions
            foreach (var s in this.GetSessions(s => s.Connected && s.LoggedIn))
            {
                s.Send(message);
            }
        }

        internal StateFieldMetadata[] StateFieldMetadatas { get; private set; }

        internal StateFieldMetadata[] GetStateFieldMetadatas(ServerSummary[] summaries)
        {
            var stateMetadataDict = new Dictionary<Type, StateFieldMetadata>();

            foreach (var s in summaries)
            {
                StateFieldMetadata metadata;

                if (stateMetadataDict.TryGetValue(s.GetType(), out metadata))
                {
                    metadata.InstanceNames.Add(s.Name);
                }
                else
                {
                    metadata = new StateFieldMetadata();
                    metadata.InstanceNames = new List<string> { s.Name };
                    metadata.Fields = GetClientFieldAttributes(s.GetType()).ToArray();
                    stateMetadataDict.Add(s.GetType(), metadata);
                }
            }

            var globalDataType = typeof(NodeSummary);

            stateMetadataDict.Add(globalDataType, new StateFieldMetadata
                {
                    InstanceNames = null,
                    Fields = GetClientFieldAttributes(globalDataType).ToArray()
                });

            return stateMetadataDict.Values.ToArray();
        }

        private List<ClientFieldAttribute> GetClientFieldAttributes(Type type)
        {
            var list = new List<ClientFieldAttribute>();

            foreach (var p in type.GetProperties())
            {
                var att = p.GetCustomAttributes(false).FirstOrDefault() as DisplayAttribute;

                if (att != null)
                {
                    var clientAtt = new ClientFieldAttribute(att);
                    clientAtt.PropertyName = p.Name;
                    if (p.PropertyType.IsPrimitive)
                        clientAtt.DataType = p.PropertyType;
                    else
                        clientAtt.DataType = typeof(string);
                    list.Add(clientAtt);
                }
            }

            return list;
        }

        internal UserConfig GetUserByName(string name)
        {
            UserConfig user;
            m_UsersDict.TryGetValue(name, out user);
            return user;
        }

        private static JsonConverter m_IPEndPointConverter = new ListenersJsonConverter();

        /// <summary>
        /// Jsons the serialize.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <returns></returns>
        public override string JsonSerialize(object target)
        {
            return JsonConvert.SerializeObject(target, m_IPEndPointConverter);
        }
    }
}
