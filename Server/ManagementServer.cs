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
using SuperSocket.SocketEngine;

namespace SuperSocket.Management.Server
{
    /// <summary>
    /// Server manager app server
    /// </summary>
    public class ManagementServer : WebSocketServer<ManagementSession>
    {
        private Dictionary<string, UserConfig> m_UsersDict;

        private string[] m_ExcludedServers;

        private IList<IAppServer> m_Servers;

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
        /// <param name="socketServerFactory">The socket server factory.</param>
        /// <param name="protocol">The protocol.</param>
        /// <returns></returns>
        public override bool Setup(IRootConfig rootConfig, IServerConfig config, ISocketServerFactory socketServerFactory, ICustomProtocol<IWebSocketFragment> protocol)
        {
            if (!base.Setup(rootConfig, config, socketServerFactory, protocol))
                return false;

            var users = config.GetChildConfig<UserConfigCollection>("users");

            if (users == null || users.Count <= 0)
            {
                Logger.LogError("No user defined");
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

        /// <summary>
        /// Called when [startup].
        /// </summary>
        protected override void OnStartup()
        {
            Messanger.Register<List<IAppServer>>(HandleLoadedServers);
            Messanger.Register<PermformanceDataEventArgs>(HandlePermformanceData);
            base.OnStartup();
        }

        /// <summary>
        /// Called when [stopped].
        /// </summary>
        protected override void OnStopped()
        {
            Messanger.UnRegister<List<IAppServer>>();
            Messanger.UnRegister<PermformanceDataEventArgs>();
            base.OnStopped();
        }

        void HandlePermformanceData(PermformanceDataEventArgs e)
        {
            var globalPerfData = e.GlobalData as GlobalPerformanceData;

            var currentNodeInfo = CurrentNodeInfo;

            var instanceDataDict = e.InstancesData.ToDictionary(i => i.ServerName, StringComparer.OrdinalIgnoreCase);

            currentNodeInfo.GlobalInfo = e.GlobalData;

            for (var i = 0; i < currentNodeInfo.Instances.Length; i++)
            {
                var instanceState = currentNodeInfo.Instances[i];

                var instanceData = instanceDataDict[instanceState.Name];

                var server = m_Servers[i];

                instanceState.IsRunning = server.IsRunning;
                instanceState.TotalConnections = instanceData.Data.CurrentRecord.TotalConnections;
                instanceState.TotalHandledCommands = instanceData.Data.CurrentRecord.TotalHandledCommands;
                instanceState.CommandHandlingSpeed = (instanceData.Data.CurrentRecord.TotalHandledCommands - instanceData.Data.PreviousRecord.TotalHandledCommands) / instanceData.Data.CurrentRecord.RecordSpan;
                instanceState.CollectedTime = instanceData.Data.CurrentRecord.RecordTime;
            }

            CurrentNodeInfo = currentNodeInfo;

            if (StateFieldMetadatas == null)
            {
                StateFieldMetadatas = GetStateFieldMetadatas(CurrentNodeInfo.Instances);
            }

            BroadcastServerUpdate();
        }

        void HandleLoadedServers(IList<IAppServer> servers)
        {
            var serverList = servers.Where(s => !s.Name.Equals(this.Name, StringComparison.OrdinalIgnoreCase));

            if (m_ExcludedServers != null && m_ExcludedServers.Length > 0)
            {
                serverList = serverList.Where(s => !m_ExcludedServers.Contains(s.Name, StringComparer.OrdinalIgnoreCase));
            }

            m_Servers = serverList.ToList();

            CurrentNodeInfo = new NodeInfo
            {
                Instances = m_Servers.Select(s => new ServerState
                {
                    Name = s.Name,
                    IsRunning = s.IsRunning,
                    StartedTime = s.StartedTime,
                    Listeners = string.Format("{0}:{1}", s.Config.Ip, s.Config.Port)
                }).ToArray()
            };
        }

        internal NodeInfo CurrentNodeInfo { get; private set; }

        internal IAppServer GetServerByName(string name)
        {
            return m_Servers.FirstOrDefault(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        private void BroadcastServerUpdate()
        {
            var message = string.Format("{0} {1}", CommandName.UPDATE, JsonSerialize(CurrentNodeInfo));

            //Only push update to loged in sessions
            foreach (var s in this.GetSessions(s => s.Status == SessionStatus.Healthy && s.LoggedIn))
            {
                s.SendResponse(message);
            }
        }

        internal StateFieldMetadata[] StateFieldMetadatas { get; private set; }

        internal StateFieldMetadata[] GetStateFieldMetadatas(ServerState[] states)
        {
            var stateMetadataDict = new Dictionary<Type, StateFieldMetadata>();

            foreach (var s in states)
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

            var globalDataType = typeof(GlobalPerformanceMetadata);

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
    }
}
