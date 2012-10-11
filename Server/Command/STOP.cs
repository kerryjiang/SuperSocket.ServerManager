using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SuperSocket.Common;
using SuperSocket.Management.Server.Model;
using SuperSocket.SocketBase.Protocol;
using SuperWebSocket.SubProtocol;

namespace SuperSocket.Management.Server.Command
{
    /// <summary>
    /// Stop command, which is used for stopping AppServer instance
    /// </summary>
    public class STOP : AsyncJsonSubCommand<ManagementSession, string>
    {
        /// <summary>
        /// Executes the async json command.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="token">The token.</param>
        /// <param name="commandInfo">The command info.</param>
        protected override void ExecuteAsyncJsonCommand(ManagementSession session, string token, string commandInfo)
        {
            if (!session.LoggedIn)
            {
                session.Close();
                return;
            }

            var server = session.AppServer.GetServerByName(commandInfo);

            if (server == null)
            {
                SendJsonMessage(session, token,
                    new StopResult
                    {
                        Result = false,
                        Message = string.Format("The server instance \"{0}\" doesn't exist", commandInfo)
                    });
                return;
            }

            server.Stop();

            SendJsonMessage(session, token,
                new StartResult
                {
                    Result = true,
                    NodeInfo = session.AppServer.CurrentNodeInfo
                });
        }
    }
}
