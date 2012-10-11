using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SuperSocket.SocketBase;

namespace SuperSocket.Management.Server.Model
{
    /// <summary>
    /// Node's information
    /// </summary>
    public class NodeInfo
    {
        /// <summary>
        /// Gets or sets the global info.
        /// </summary>
        /// <value>
        /// The global info.
        /// </value>
        public GlobalPerformanceData GlobalInfo { get; set; }

        /// <summary>
        /// Gets or sets the instances.
        /// </summary>
        /// <value>
        /// The instances.
        /// </value>
        public ServerState[] Instances { get; set; }
    }
}
