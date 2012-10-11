using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.Linq;
using System.Text;
using DynamicViewModel;
using SuperSocket.ClientEngine;
using SuperSocket.Management.AgentClient.Config;
using SuperSocket.Management.AgentClient.Metadata;
using WebSocket4Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Threading;

namespace SuperSocket.Management.AgentClient.ViewModel
{
    public class NodeMasterViewModel : ViewModelBase
    {
#if SILVERLIGHT
        private static AsyncOperation m_AsyncOper = AsyncOperationManager.CreateOperation(null);
#endif

        private AgentWebSocket m_WebSocket;
        private NodeConfig m_Config;
        private StateFieldMetadata[] m_FieldMetadatas;

        private ClientFieldAttribute[] m_ColumnAttributes;

        private ClientFieldAttribute[] m_NodeDetailAttributes;

        public NodeMasterViewModel(NodeConfig config)
        {
            m_Config = config;
            Name = m_Config.Name;

            try
            {
                m_WebSocket = new AgentWebSocket(config.Uri);
            }
            catch (Exception)
            {
                return;
            }

#if !SILVERLIGHT
            m_WebSocket.AllowUnstrustedCertificate = true;
            m_WebSocket.Closed += new EventHandler(WebSocket_Closed);
            m_WebSocket.Error += new EventHandler<ClientEngine.ErrorEventArgs>(WebSocket_Error);
            m_WebSocket.Opened += new EventHandler(WebSocket_Opened);
#else
            m_WebSocket.ClientAccessPolicyProtocol = System.Net.Sockets.SocketClientAccessPolicyProtocol.Tcp;
            m_WebSocket.Closed += new EventHandler(CreateAsyncOperation<object, EventArgs>(WebSocket_Closed));
            m_WebSocket.Error += new EventHandler<ClientEngine.ErrorEventArgs>(CreateAsyncOperation<object, ClientEngine.ErrorEventArgs>(WebSocket_Error));
            m_WebSocket.Opened += new EventHandler(CreateAsyncOperation<object, EventArgs>(WebSocket_Opened));
#endif
            
            m_WebSocket.Open();
            State = NodeState.Connecting;
        }

        void WebSocket_Opened(object sender, EventArgs e)
        {
            var websocket = sender as AgentWebSocket;
            State = NodeState.Logging;

            dynamic loginInfo = new ExpandoObject();
            loginInfo.UserName = m_Config.UserName;
            loginInfo.Password = m_Config.Password;

#if !SILVERLIGHT
            websocket.Query<dynamic>("LOGIN", (object)loginInfo, OnLoggedIn);
#else
            websocket.Query<dynamic>("LOGIN", (object)loginInfo, OnLoggedInAsync);
#endif
        }

#if SILVERLIGHT
        public void OnLoggedInAsync(dynamic result)
        {
            CreateAsyncOperation<dynamic>(OnLoggedIn)(result);
        }
#endif

        void OnLoggedIn(dynamic result)
        {
            if (result["Result"].ToObject<bool>())
            {                
                m_FieldMetadatas = result["FieldMetadatas"].ToObject<StateFieldMetadata[]>();
                var nodeInfo = DynamicViewModelFactory.Create(result["NodeInfo"].ToString());
                BuildGridColumns(m_FieldMetadatas);
                DetailViewModel = nodeInfo;
                GlobalInfo = nodeInfo.GlobalInfo;
                State = NodeState.Connected;
            }
            else
            {
                //login failed
            }
        }

        void BuildGridColumns(StateFieldMetadata[] fieldMetadatas)
        {
            var dict = new Dictionary<string, ClientFieldAttribute>(StringComparer.OrdinalIgnoreCase);

            foreach (var metadata in fieldMetadatas)
            {
                if(metadata.InstanceNames == null || metadata.InstanceNames.Count == 0)
                {
                    m_NodeDetailAttributes = metadata.Fields;
                    continue;
                }

                foreach(var f in metadata.Fields)
                {
                    if (dict.ContainsKey(f.Name))
                        continue;

                    dict.Add(f.Name, f);
                }
            }

            m_ColumnAttributes = dict.Values.OrderBy(a => a.Order).ToArray();
        }

        void WebSocket_Error(object sender, ClientEngine.ErrorEventArgs e)
        {
            if (e.Exception != null)
                throw e.Exception;
        }

        void WebSocket_Closed(object sender, EventArgs e)
        {
            State = NodeState.Offline;
        }

        public string Name { get; private set; }

        private NodeState m_State = NodeState.Offline;

        public NodeState State
        {
            get { return m_State; }
            set
            {
                m_State = value;
                RaisePropertyChanged("State");
            }
        }

        private DynamicViewModel.DynamicViewModel m_DetailViewModel;

        public DynamicViewModel.DynamicViewModel DetailViewModel
        {
            get { return m_DetailViewModel; }
            set
            {
                m_DetailViewModel = value;
                RaisePropertyChanged("DetailViewModel");
            }
        }

        private DynamicViewModel.DynamicViewModel m_GlobalInfo;

        public DynamicViewModel.DynamicViewModel GlobalInfo
        {
            get { return m_GlobalInfo; }
            set
            {
                m_GlobalInfo = value;
                RaisePropertyChanged("GlobalInfo");
            }
        }

        public void DataGridLoaded(object sender, RoutedEventArgs e)
        {
            var grid = sender as DataGrid;
            var existingColumns = grid.Columns.Select(c => c.Header.ToString()).ToArray();

            foreach(var a in m_ColumnAttributes)
            {
                if (existingColumns.Contains(a.Name, StringComparer.OrdinalIgnoreCase))
                    continue;

                grid.Columns.Add(new DataGridTextColumn()
                    {
                        Header = a.Name,
                        Binding = new Binding(GetColumnValueBindingName(a.PropertyName))
                            {
                                StringFormat = string.IsNullOrEmpty(a.Format) ? "{0}" : a.Format
                            },
                        SortMemberPath = a.Name
                    });
            }
        }

        public void NodeDetailDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var grid = sender as Grid;

            var columns = 4;
            var rows = (int)Math.Ceiling((double)m_NodeDetailAttributes.Length / (double)columns);

            for (var i = 0; i < columns; i++)
            {
                grid.ColumnDefinitions.Add(new ColumnDefinition());
            }

            for (var i = 0; i < rows; i++)
            {
                grid.RowDefinitions.Add(new RowDefinition());
            }

            var k = 0;

            for (var i = 0; i < rows; i++)
            {
                for (var j = 0; j < columns; j++)
                {
                    var att = m_NodeDetailAttributes[k++];

                    var nameValuePanel = new StackPanel();
                    nameValuePanel.Orientation = Orientation.Horizontal;

                    var label = new TextBlock();
                    label.Style = App.Current.Resources["GlobalInfoLabel"] as Style;
                    label.Text = att.Name + ":";
                    nameValuePanel.Children.Add(label);

                    var value = new TextBlock();
                    value.Style = App.Current.Resources["GlobalInfoValue"] as Style;
                    value.SetBinding(TextBlock.TextProperty, new Binding(GetColumnValueBindingName(att.PropertyName))
                    {
                        StringFormat = string.IsNullOrEmpty(att.Format) ? "{0}" : att.Format
                    });
                    nameValuePanel.Children.Add(value);

                    nameValuePanel.SetValue(Grid.ColumnProperty, j);
                    nameValuePanel.SetValue(Grid.RowProperty, i);
                    grid.Children.Add(nameValuePanel);

                    if (k >= m_NodeDetailAttributes.Length)
                        break;
                }
            }
        }

#if !SILVERLIGHT
        private string GetColumnValueBindingName(string name)
        {
            return name;
        }

        private string GetColumnValueBindingName(string parent, string name)
        {
            return parent + "." + name;
        }
#else
        private string GetColumnValueBindingName(string name)
        {
            return string.Format("[{0}]", name);
        }

        private string GetColumnValueBindingName(string parent, string name)
        {
            return string.Format("[{0}][{1}]", parent, name);
        }
#endif

#if SILVERLIGHT

        protected Action CreateAsyncOperation(Action operation)
        {
            return () =>
            {
                m_AsyncOper.Post(x => operation(), null);
            };
        }

        protected Action<T> CreateAsyncOperation<T>(Action<T> operation)
        {
            return (t) =>
            {
                m_AsyncOper.Post(x => operation((T)x), t);
            };
        }

        protected Action<T1, T2> CreateAsyncOperation<T1, T2>(Action<T1, T2> operation)
        {
            return (t1, t2) =>
            {
                m_AsyncOper.Post(x =>
                {
                    var args = (Tuple<T1, T2>)x;
                    operation(args.Item1, args.Item2);
                }, new Tuple<T1, T2>(t1, t2));
            };
        }

        protected Action<T1, T2, T3> CreateAsyncOperation<T1, T2, T3>(Action<T1, T2, T3> operation)
        {
            return (t1, t2, t3) =>
            {
                m_AsyncOper.Post(x =>
                {
                    var args = (Tuple<T1, T2, T3>)x;
                    operation(args.Item1, args.Item2, args.Item3);
                }, new Tuple<T1, T2, T3>(t1, t2, t3));
            };
        }

#endif
    }
}
