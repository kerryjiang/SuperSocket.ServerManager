using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using SuperSocket.Management.AgentClient.Config;

namespace SuperSocket.Management.AgentClient
{
    public partial class ConfigWindow : ChildWindow
    {
        public ConfigWindow()
        {
            InitializeComponent();
            ConfigTextBox.Text = AgentConfig.LoadContent();
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var config = ConfigTextBox.Text.XmlDeserialize<AgentConfig>();

                if (config == null)
                {
                    throw new Exception();
                }

                config.Save();
            }
            catch (Exception)
            {
                MessageBox.Show("Invalid configuration");
                return;
            }

            this.DialogResult = true;
            this.Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}

