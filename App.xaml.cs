using System;
using System.Threading;
using System.Windows;

namespace Launcher
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            // Si detectamos otra instancia de la aplicación, la cerramos.
            _ = new Mutex(true, @"Global\WinterAO_Launcher", out bool onlyInstance);
            if (!onlyInstance)
            {
                MessageBox.Show("El launcher ya esta abierto.", "Error", MessageBoxButton.OK);
                Environment.Exit(0);
            }
        }
    }
}
