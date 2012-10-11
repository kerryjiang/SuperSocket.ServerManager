using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using SuperSocket.Management.AgentClient.ViewModel;
using SuperSocket.Management.AgentClient.Config;

namespace SuperSocket.Management.AgentClient
{
    /// <summary>
    /// Interaction logic for MainPanel.xaml
    /// </summary>
    public partial class MainPanel : UserControl
    {
        public MainPanel()
        {
            InitializeComponent();
        }

#if SILVERLIGHT
        private void Configure_Click(object sender, RoutedEventArgs e)
        {
            var window = new ConfigWindow();
            window.Show();
        }
#else
        private void Configure_Click(object sender, RoutedEventArgs e)
        {
            var win = new Window();
            win.Title = "Configure";
            win.Owner = App.Current.MainWindow;
            win.Content = new ConfigPanel()
            {
                DataContext = new ConfigViewModel(AgentConfig.Load())
            };
            win.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            win.WindowStyle = WindowStyle.SingleBorderWindow;
            win.ResizeMode = ResizeMode.NoResize;
            win.Width = 600;
            win.Height = 300;
            win.ShowDialog();
        }
#endif
    }
}
