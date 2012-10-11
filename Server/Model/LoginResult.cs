using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SuperSocket.SocketBase;

namespace SuperSocket.Management.Server.Model
{
    /// <summary>
    /// Login command result
    /// </summary>
    public class LoginResult
    {
        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="LoginResult"/> is result.
        /// </summary>
        /// <value>
        ///   <c>true</c> if result; otherwise, <c>false</c>.
        /// </value>
        public bool Result { get; set; }

        /// <summary>
        /// Gets or sets the field metadata.
        /// </summary>
        /// <value>
        /// The field metadata.
        /// </value>
        public StateFieldMetadata[] FieldMetadatas { get; set; }

        /// <summary>
        /// Gets or sets the server's information.
        /// </summary>
        /// <value>
        /// The server's information.
        /// </value>
        public NodeInfo NodeInfo { get; set; }
    }
}
