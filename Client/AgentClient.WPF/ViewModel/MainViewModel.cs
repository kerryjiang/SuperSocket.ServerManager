using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SuperSocket.Management.AgentClient.Config;

namespace SuperSocket.Management.AgentClient.ViewModel
{
    public class MainViewModel
    {
        public MainViewModel(AgentConfig config)
        {
            var nodes = config.Nodes;

            if (nodes != null)
                Nodes = nodes.Select(n => new NodeMasterViewModel(n)).ToArray();
            else
                Nodes = new NodeMasterViewModel[0];
        }

        public NodeMasterViewModel[] Nodes { get; private set; }
    }
}
