using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Net;
using System.Text.RegularExpressions;

namespace CNLab3_Messenger.GUI
{
    /// <summary>
    /// Логика взаимодействия для AddContactDialog.xaml
    /// </summary>
    public partial class AddContactDialog : Window
    {
        public IPAddress IpAddress { get; private set; } = IPAddress.None;

        public int Port { get; private set; } = IPEndPoint.MinPort;

        private AddContactDialogViewModel _viewModel = new AddContactDialogViewModel();

        public AddContactDialog()
        {
            InitializeComponent();

            // TODO delete
            IpAddress = IPAddress.Parse("77.37.245.90");
            Port = 60400;

            _viewModel.IpAddress = IpAddress.ToString();
            _viewModel.Port = Port.ToString();
            DataContext = _viewModel;
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            if (!IPAddress.TryParse(_viewModel.IpAddress, out IPAddress ipAddress))
            {
                MessageBox.Show("Wrong ip address format (Example: 128.255.0.1).");
                return;
            }
            IpAddress = ipAddress;

            if (!int.TryParse(_viewModel.Port, out int port) || port < IPEndPoint.MinPort || port > IPEndPoint.MaxPort)
            {
                MessageBox.Show($"Wrong port format. Min port: {IPEndPoint.MinPort}, max port: {IPEndPoint.MaxPort}.");
                return;
            }
            Port = port;
            
            DialogResult = true;
        }

        public bool ShowDialog(out IPAddress ipAddres, out int port)
        {
            bool? res = ShowDialog();
            if (res == true)
            {
                ipAddres = IpAddress;
                port = Port;
                return true;
            }
            else
            {
                ipAddres = null;
                port = 0;
                return false;
            }
        }
    }
}
