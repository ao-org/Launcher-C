using System;
using System.Threading;
using System.IO;
using System.Windows;

namespace Launcher
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        // Obtiene el nombre del .exe que se va a generar al compilar la aplicación.
        public static string ExecutableName = System.Reflection.Assembly.GetEntryAssembly().GetName().Name;

        // Para prevenir múltiples ejecuciones de esta aplicación.
        private static Mutex _mutex = null;

        // Todos los archivos del cliente en la subcarpeta Argentum20, para no mezclarlos con los archivos del Launcher.
        public static string ARGENTUM_PATH;

        void App_Startup(object sender, StartupEventArgs e)
        {
            // Chequeo que solo haya 1 instancia de la aplicacion.
            _mutex = new Mutex(true, "LauncherAO20", out bool singleInstance);
            if (!singleInstance)
            {
                // ya hay una instancia de esta aplicación, cerramos la nueva.
                MessageBox.Show("Ya hay una instancia de esta aplicación abierta");
                Environment.Exit(0);
            }
            // Create main application window, starting minimized if specified
            if (e.Args.Length > 0)
            {
                ARGENTUM_PATH = Directory.GetCurrentDirectory() + "\\..\\..\\";
            }
            else
            {
                ARGENTUM_PATH = Directory.GetCurrentDirectory() + "\\..\\";
            }
            Main mainWindow = new Main();

            mainWindow.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
			_mutex.ReleaseMutex();
            base.OnExit(e);
        }
    }
}
