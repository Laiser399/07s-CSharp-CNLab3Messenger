using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace CNLab3_Messenger
{
    //StartupUri="GUI/MainWindow/MainWindow.xaml"

    /// <summary>
    /// Логика взаимодействия для App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            var wnd = new GUI.MainWindow(60404);
            wnd.Show();
            wnd.Left = 100;
            //wnd.ShowDialog();
        }
    }
}
