using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace SecondUserTest
{
    /// <summary>
    /// Логика взаимодействия для App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            var wnd = new CNLab3_Messenger.GUI.MainWindow(60400);
            wnd.Show();
            wnd.Left = 1000;
            //wnd.ShowDialog();
        }
    }
}