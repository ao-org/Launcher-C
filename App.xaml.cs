using System;
using System.IO;
using System.Threading;
using System.Windows;

namespace Launcher
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        // Todos los archivos del cliente en la subcarpeta Argentum20, para no mezclarlos con los archivos del Launcher.
        public static string ARGENTUM_FILES = Directory.GetCurrentDirectory() + "\\Argentum20\\";
    }
}
