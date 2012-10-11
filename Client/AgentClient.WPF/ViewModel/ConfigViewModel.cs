using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SuperSocket.Management.AgentClient.Config;
using System.Collections.ObjectModel;

namespace SuperSocket.Management.AgentClient.ViewModel
{
    public class NewNodeConfig : NodeConfig
    {
        public NewNodeConfig()
        {
            Name = "* New Node";
        }
    }

    public class ConfigViewModel : ViewModelBase
    {
        private AgentConfig m_AgentConfig;

        private ObservableCollection<NodeConfig> m_Nodes;

        public ObservableCollection<NodeConfig> Nodes
        {
            get { return m_Nodes; }
            set
            {
                m_Nodes = value;
                RaisePropertyChanged("Nodes");
            }
        }

        public ConfigViewModel(AgentConfig agentConfig)
        {
            m_AgentConfig = agentConfig;
            m_Nodes = new ObservableCollection<NodeConfig>(agentConfig.Nodes);
            m_Nodes.Add(new NewNodeConfig());
            SelectedNode = m_Nodes.First();
        }

        private NodeConfig m_SelectedNode;

        public NodeConfig SelectedNode
        {
            get { return m_SelectedNode; }
            set
            {
                m_SelectedNode = value;
                RaisePropertyChanged("SelectedNode");
                SelectedNodeViewModel = new NodeConfigViewModel(value);
            }
        }

        private NodeConfigViewModel m_SelectedNodeViewModel;

        public NodeConfigViewModel SelectedNodeViewModel
        {
            get { return m_SelectedNodeViewModel; }
            set
            {
                m_SelectedNodeViewModel = value;
                if (m_SelectedNodeViewModel != null)
                {
                    m_SelectedNodeViewModel.Saved += new EventHandler(OnNodeViewModelSaved);
                    m_SelectedNodeViewModel.Removed += new EventHandler(OnNodeViewModelRemoved);
                }
                RaisePropertyChanged("SelectedNodeViewModel");
            }
        }

        void OnNodeViewModelRemoved(object sender, EventArgs e)
        {
            var currentNode = SelectedNode;

            m_Nodes.Remove(currentNode);
            SelectedNode = m_Nodes.First();

            m_AgentConfig.Nodes.Remove(currentNode);

            //Save
            m_AgentConfig.Save();
        }

        void OnNodeViewModelSaved(object sender, EventArgs e)
        {
            var nodeViewMode = sender as NodeConfigViewModel;

            var currentNode = SelectedNode;

            if (currentNode is NewNodeConfig)
            {
                //Update UI
                m_Nodes.Remove(currentNode);

                currentNode = new NodeConfig();
                m_Nodes.Add(currentNode);
                SelectedNode = currentNode;

                m_Nodes.Add(new NewNodeConfig());

                //Update configuration model
                m_AgentConfig.Nodes.Add(currentNode);
            }

            currentNode.Name = nodeViewMode.Name;
            currentNode.Uri = nodeViewMode.Uri;
            currentNode.UserName = nodeViewMode.UserName;
            currentNode.Password = nodeViewMode.Password;

            //Save
            m_AgentConfig.Save();
        }
    }
}
