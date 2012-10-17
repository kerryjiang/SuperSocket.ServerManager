using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SuperSocket.Management.Server
{
    /// <summary>
    /// Server State
    /// </summary>
    [Serializable]
    public class ServerState
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the started time.
        /// </summary>
        /// <value>
        /// The started time.
        /// </value>
        [Display("Started Time", Order = 0)]
        public DateTime StartedTime { get; set; }

        /// <summary>
        /// Gets or sets the collected time.
        /// </summary>
        /// <value>
        /// The collected time.
        /// </value>
        public DateTime CollectedTime { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is running.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is running; otherwise, <c>false</c>.
        /// </value>
        [Display("Is Running", Order = 1)]
        public bool IsRunning { get; set; }

        /// <summary>
        /// Gets or sets the total count of the connections.
        /// </summary>
        /// <value>
        /// The total count of the connections.
        /// </value>
        [Display("Total Connections", Order = 2)]
        public int TotalConnections { get; set; }


        /// <summary>
        /// Gets or sets the total handled requests count.
        /// </summary>
        /// <value>
        /// The total handled requests count.
        /// </value>
        [Display("Total Handled Commands", Format = "{0:N0}", Order = 4)]
        public long TotalHandledCommands { get; set; }

        /// <summary>
        /// Gets or sets the request handling speed, per second.
        /// </summary>
        /// <value>
        /// The request handling speed.
        /// </value>
        [Display("Command Handling Speed (#/second)", Format = "{0:f0}", Order = 5)]
        public double CommandHandlingSpeed { get; set; }


        /// <summary>
        /// Gets or sets the listeners.
        /// </summary>
        /// <value>
        /// The listeners.
        /// </value>
        [Display("Listeners", Order = 6, OutputInPerfLog = false)]
        public string Listeners { get; set; }
    }
}
