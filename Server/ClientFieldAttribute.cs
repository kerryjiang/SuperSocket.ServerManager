using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SuperSocket.Common;
using SuperSocket.SocketBase;

namespace SuperSocket.Management.Server
{
    /// <summary>
    /// Server state field's attribute for client application
    /// </summary>
    public class ClientFieldAttribute : DisplayAttribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ClientFieldAttribute" /> class.
        /// </summary>
        /// <param name="att">The att.</param>
        public ClientFieldAttribute(DisplayAttribute att)
            : base(att.Name)
        {
            this.Format = att.Format;
            this.Order = att.Order;
            this.OutputInPerfLog = att.OutputInPerfLog;
            this.ShortName = att.ShortName;
        }

        /// <summary>
        /// Gets or sets the name of the property.
        /// </summary>
        /// <value>
        /// The name of the property.
        /// </value>
        public string PropertyName { get; set; }

        /// <summary>
        /// Gets or sets the type of the data.
        /// </summary>
        /// <value>
        /// The type of the data.
        /// </value>
        public Type DataType { get; set; }
    }
}
