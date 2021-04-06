// AO Libre C# Launcher by Pablo M. Duval (Discord: Abusivo#1215)
// Este launcher y todo su contenido incluyendo sus códigos son de uso público y gratuito.

using Launcher.src;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Windows;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Text.Json;
using System.Drawing.Text;

namespace Launcher
{
    public partial class MainWindow : Window, IComponentConnector
    {
        private readonly IO local = new IO();
        private readonly Networking networking = new Networking();

        //METODOS

        /**
         * Constructor
         */
        public MainWindow()
        {
            // Inicializamos los componentes de este formulario.
            InitializeComponent();
            //PrivateFontCollection privateFontCollection = new PrivateFontCollection();
            //privateFontCollection.AddFontFile("../assets/fonts/Cardo.ttf");
            //txtStatus.FontFamily = FontFamily

            BuscarActualizaciones();
            getServerStatus();
            getChangelog();
        }

        private int BuscarActualizaciones()
        {
            local.ArchivosDesactualizados = networking.CheckOutdatedFiles().Count;

            // Comprobamos la version actual del cliente
            if (local.ArchivosDesactualizados == 0)
            {
                lblDow.Content = "¡Cliente al día!";
                lblDow.HorizontalContentAlignment = HorizontalAlignment.Center;
                lblDow.Foreground = new SolidColorBrush(Colors.Yellow);
            }
            else // Si el cliente no esta actualizado, lo notificamos
            {
                lblDow.Content = "Tienes " + local.ArchivosDesactualizados + " archivos desactualizados...";
                lblDow.HorizontalContentAlignment = HorizontalAlignment.Center;
                lblDow.Foreground = new SolidColorBrush(Colors.Red);
            }

            return local.ArchivosDesactualizados;
        }

        /**
         * Inicia el proceso de actualizacion del cliente
         */
        private void Actualizar()
        {
            // ¿Hay archivos desactualizados?
            if (local.ArchivosDesactualizados > 0)
            {
                // Le indico al programa que estamos en medio de una actualización.
                local.Actualizando = true;

                // Anunciamos el numero de archivo que estamos descargando
                lblDow.Content = "Descargando " + networking.fileQueue[local.ArchivoActual] + ". Archivo " + local.ArchivoActual + " de " + (networking.fileQueue.Count - 1);
                lblDow.HorizontalContentAlignment = HorizontalAlignment.Left;
                lblDow.Foreground = new SolidColorBrush(Colors.White);

                // Comenzamos la descarga
                DescargarActualizaciones();
            }
        }

        /**
         * Comienza a descargar los archivos desactualizados.
         */
        private async void DescargarActualizaciones()
        {
            networking.CrearCarpetasRequeridas();
            
            WebClient client = new WebClient();
            client.DownloadFileCompleted += new AsyncCompletedEventHandler(UpdateDone);
            await networking.IniciarDescarga(client);
        }

        private async void getServerStatus()
        {
            HttpClient client = new HttpClient();
            HttpResponseMessage response = await client.GetAsync("http://api.ao20.com.ar/");
            response.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync();

            ServerStatus serverStatus = JsonSerializer.Deserialize<ServerStatus>(responseBody);    
            
            if(serverStatus != null)
            {
                if (serverStatus.ok)
                {
                    txtStatus.Content = "ONLINE: " + serverStatus.onlineCount;
                    txtStatus.Foreground = new SolidColorBrush(Colors.ForestGreen);
                }
                else
                {
                    txtStatus.Content = "OFFLINE";
                    txtStatus.Foreground = new SolidColorBrush(Colors.DarkRed);
                }
            }

        }


        private void getChangelog()
        {
            string Url = "http://autoupdate.ao20.com.ar/changelog.txt";
            var webRequest = WebRequest.Create(Url);
            var responseStream = webRequest.GetResponse().GetResponseStream();

            using var streamReader = new StreamReader(responseStream);
            // Return next available character or -1 if there are no characters to be read
            while (streamReader.Peek() > -1)
            {
                txtChangelog.Text += streamReader.ReadLine() + "\n";
            }
        }
       
        private void UpdateDone(object sender, AsyncCompletedEventArgs e)
        {
            // Decimos que ya terminó esta descarga
            networking.downloadQueue.SetResult(true);

            // Si NO quedan archivos pendientes por descargar...
            if (local.ArchivoActual == (networking.fileQueue.Count - 1))
            {
                // Actualizo el VersionInfo.json
                IO.SaveLatestVersionInfo(networking.versionRemotaString);
                
                // Actualizo el label.
                lblDow.Content = "¡Actualización Completada!";
                lblDow.Foreground = new SolidColorBrush(Colors.Yellow);

                // Le digo al programa que ya no estamos actualizando mas nada.
                local.Actualizando = false;
                local.ArchivoActual = 0;
                local.ArchivosDesactualizados = 0;
                //networking.fileQueue.Clear();

                return;
            }

            // Si quedan, actualizamos el label.
            if (local.ArchivoActual < networking.fileQueue.Count)
            {
                local.ArchivoActual++;

                lblDow.Content = "Descargando " + networking.fileQueue[local.ArchivoActual] + ". Archivo " + local.ArchivoActual + " de " + (networking.fileQueue.Count - 1);
                grdPbarLlena.Width = (416 * local.ArchivoActual) / (networking.fileQueue.Count - 1);
                grdPbarLlena.Visibility = Visibility.Visible;
            }
        }

        /**
         * Boton para ir a la web
         */
        private void btnSitio_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://ao20.com.ar");
        }

        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        /**
         * Boton Salir
         */
        private void btnSalir_Click(object sender, RoutedEventArgs e)
        {
            Environment.Exit(0);
        }

        /**
         * Boton 'Jugar'
         * 
         * Si el cliente esta ACTUALIZADO y existe el ejecutable del cliente, lo abrimos.
         * Si el cliente NO esta ACTUALIZADO, descargamos e instalamos las actualizaciones.
         */
        private void btnJugar_Click(object sender, RoutedEventArgs e)
        {
            // Si estamos actualizando el cliente no lo dejo clickear este boton.
            if (local.Actualizando == true) return;

            // Si hay archivos desactualizados, primero los actualizamos.
            if (local.ArchivosDesactualizados > 0)
            {
                Actualizar();
                return;
            }

            // Abrimos el cliente.
            AbrirJuego();
        }

        /**
         * Boton de minimizar
         */
        private void btnMini_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void image_discord_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://discord.gg/e3juVbF");
        }

        private void image_facebook_Click(object sender, RoutedEventArgs e)
        {

            Process.Start("https://www.facebook.com/ao20oficial/"); 
        }

        private void image_instagram_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://www.instagram.com/ao20oficial/?hl=es");
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Environment.Exit(0);
        }

        private static void AbrirJuego()
        {
            string gameExecutable = Directory.GetCurrentDirectory() + "/Cliente/Argentum.exe";
            if (File.Exists(gameExecutable))
            {
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.FileName = gameExecutable;
                startInfo.UseShellExecute = false;

                try
                {
                    // Start the process with the info we specified.
                    Process.Start(startInfo);

                    // Cerramos el launcher.
                    Environment.Exit(0);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
            else
            {
                MessageBox.Show("No se pudo abrir el ejecutable del juego, al parecer no existe!");
            }
        }

        private void btnConfiguracion_Click(object sender, RoutedEventArgs e)
        {
            Configuracion configuracion = new Configuracion();
            configuracion.Show();
        }
    }


    public class ServerStatus
    {
        public bool ok { get; set; }
        public int onlineCount { get; set; }
    }
}
