using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace CNLab3_Messenger.GUI
{
    /// <summary>
    /// Логика взаимодействия для Window1.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainWindowViewModel _viewModel;

        public MainWindow(int port)
        {
            InitializeComponent();
            _viewModel = new MainWindowViewModel(port);
            DataContext = _viewModel;
        }

        private void InputKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ICommand cmd = _viewModel.SendMessageCmd;
                if (cmd.CanExecute(null))
                    cmd.Execute(null);
            }
        }
    }
}
