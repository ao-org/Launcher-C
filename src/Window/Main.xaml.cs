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
using System.Runtime.InteropServices;
using IniParser.Model;
using IniParser;

namespace Launcher
{
    public partial class Main : Window, IComponentConnector
    {
        private int counterClickSeeTestButton = 0;
        private readonly IO local = new IO();
        private readonly Networking networking = new Networking();
        [DllImport("kernel32")]
        static extern long WritePrivateProfileString(string Section, string Key, string Value, string FilePath);
        
        /**
         * Constructor
         */
        public Main()
        {
            // Inicializamos los componentes de este formulario.
            InitializeComponent();
        }
        private void Window_ContentRendered_1(object sender, EventArgs e)
        {
            getServerStatus();
            getChangelog();
            checkConfiguracion();
            BuscarActualizaciones(false);
        }

        private void checkConfiguracion()
        {
            if(!File.Exists(Configuracion.CONFIG_FILE)){
                btnConfiguracion.Visibility = Visibility.Hidden;
                chkLanzarAutomatico.Visibility = Visibility.Hidden;
            }
            else
            {
                btnConfiguracion.Visibility = Visibility.Visible;

                var parser = new FileIniDataParser();
                IniData file = parser.ReadFile(Configuracion.CONFIG_FILE);

                chkLanzarAutomatico.IsChecked = Convert.ToBoolean(Convert.ToInt32(file["OPCIONES"]["LanzarAutomatico"]));
            }
        }

        private async void BuscarActualizaciones(bool isTestDownload)
        {
            loadingBar.Visibility = Visibility.Visible;

            local.ArchivosDesactualizados = (await networking.CheckOutdatedFiles(isTestDownload)).Count;

            btnJugar.IsEnabled = true;
            
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

            loadingBar.Visibility = Visibility.Hidden;
        }

        /**
         * Inicia el proceso de actualizacion del cliente
         */
        private void Actualizar(bool isTestDownload)
        {
            // ¿Hay archivos desactualizados?
            if (local.ArchivosDesactualizados > 0)
            {
                // Le indico al programa que estamos en medio de una actualización.
                local.Actualizando = true;
                loadingBar.Visibility = Visibility.Visible;

                // Anunciamos el numero de archivo que estamos descargando
                lblDow.Content = "Descargando archivo " + (local.ArchivoActual + 1) + " de " + networking.fileQueue.Count;
                lblDow.Foreground = new SolidColorBrush(Colors.White);

                // Comenzamos la descarga
                DescargarActualizaciones(isTestDownload);
            }
        }

        /**
         * Comienza a descargar los archivos desactualizados.
         */
        private async void DescargarActualizaciones(bool isTestDownload)
        {
            networking.CrearCarpetasRequeridas();
            
            WebClient client = new WebClient();
            client.DownloadFileCompleted += new AsyncCompletedEventHandler(UpdateDone);
            client.DownloadProgressChanged += new DownloadProgressChangedEventHandler(UpdateProgress);
            await networking.IniciarDescarga(client);
        }

        private async void getServerStatus()
        {
            try
            {
                loadingBar.Visibility = Visibility.Visible;

                HttpClient client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(5);
                HttpResponseMessage response = await client.GetAsync(Networking.API_PATH);
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();

                ServerStatus serverStatus = JsonSerializer.Deserialize<ServerStatus>(responseBody);
                //
                if (serverStatus != null)
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
            catch(Exception)
            {
                txtStatus.Content = "ERROR DE RED";
                txtStatus.Foreground = new SolidColorBrush(Colors.Yellow);
            }
            finally
            {
                loadingBar.Visibility = Visibility.Hidden;
            }
        }

        private async void getChangelog()
        {
            try
            {
                string Url = Networking.ROOT_HOST_PATH + "changelog.txt";
                var webRequest = WebRequest.Create(Url);
                webRequest.Timeout = 10000;
                var responseStream = (await webRequest.GetResponseAsync()).GetResponseStream();

                using var streamReader = new StreamReader(responseStream);

                txtChangelog.Text = "";
                txtChangelog.IsEnabled = true;

                // Return next available character or -1 if there are no characters to be read
                while (streamReader.Peek() > -1)
                {
                    txtChangelog.Text += await streamReader.ReadLineAsync() + "\n";
                }
            }
            catch (Exception)
            {
                txtChangelog.Text = "* Error de conexión al obtener el changelog *";
            }
        }
       
        private void UpdateDone(object sender, AsyncCompletedEventArgs e)
        {
            // Decimos que ya terminó esta descarga
            networking.downloadQueue.SetResult(true);

            local.ArchivoActual++;

            // Si quedan, actualizamos el label.
            if (local.ArchivoActual < networking.fileQueue.Count)
            {
                lblDow.Content = "Descargando archivo " + (local.ArchivoActual + 1) + " de " + networking.fileQueue.Count;
                grdPbarLlena.Width = (416 * local.ArchivoActual) / networking.fileQueue.Count;
                grdPbarLlena.Visibility = Visibility.Visible;
            }
            else
            {
                // Actualizo el label.
                lblDow.Content = "¡Actualización Completada!";
                lblDow.HorizontalContentAlignment = HorizontalAlignment.Center;
                lblDow.Foreground = new SolidColorBrush(Colors.Yellow);

                //activo configuración.
                checkConfiguracion();

                //Si está chekeado el check de comenzar automático abro el juego.
                if (chkLanzarAutomatico.IsChecked == true)
                {
                    AbrirJuego();
                }

                // Le digo al programa que ya no estamos actualizando mas nada.
                local.Actualizando = false;
                local.ArchivoActual = 0;
                local.ArchivosDesactualizados = 0;
                loadingBar.Visibility = Visibility.Hidden;

                grdPbarLlena.Width = 416;
            }
        }

        private void UpdateProgress(object sender, DownloadProgressChangedEventArgs e)
        {
            grdPbarLlena.Width = (local.ArchivoActual + e.ProgressPercentage / 100.0) * 416.0 / networking.fileQueue.Count;
        }

        private static void AbrirJuego()
        {
            string gameExecutable = App.ARGENTUM_PATH + "Argentum20\\Cliente\\Argentum.exe";
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


        /**
        * Boton para ver el boton de descarga del boton de test
        */
        private void txtTestClickerButton_Click(object sender, RoutedEventArgs e)
        {
            counterClickSeeTestButton++;

            if (counterClickSeeTestButton >= 5) {
                btnJugarTest.IsEnabled = true;
                btnJugarTest.Visibility = Visibility.Visible;
            }
        }

        /**
         * Boton para ir a la web
         */
        private void btnSitio_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://ao20.com.ar");
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if(e.ChangedButton == MouseButton.Left)
                DragMove();
        }

        /**
         * Boton Salir
         */
        private void btnSalir_Click(object sender, RoutedEventArgs e)
        {
            Environment.Exit(0);
        }

        private void startUpdate(bool isTestDownload)
        {
            // Si estamos actualizando el cliente no lo dejo clickear este boton.
            if (local.Actualizando == true) return;

            // Si hay archivos desactualizados, primero los actualizamos.
            if (local.ArchivosDesactualizados > 0)
            {
                // Cerramos el proceso del juego
                Process[] runingProcess = Process.GetProcesses();
                for (int i = 0; i < runingProcess.Length; i++)
                {
                    if (runingProcess[i].ProcessName == "Argentum")
                    {
                        runingProcess[i].Kill();
                    }

                }

                Actualizar(isTestDownload);
                return;
            }

            // Abrimos el cliente.
            AbrirJuego();
        }

        /**
         * Boton 'Jugar'
         * 
         * Si el cliente esta ACTUALIZADO y existe el ejecutable del cliente, lo abrimos.
         * Si el cliente NO esta ACTUALIZADO, descargamos e instalamos las actualizaciones.
         */
        private void btnJugar_Click(object sender, RoutedEventArgs e)
        {
            startUpdate(false);
        }

        /**
         * Boton 'Jugar'
         * 
         * Si el cliente esta ACTUALIZADO y existe el ejecutable del cliente, lo abrimos.
         * Si el cliente NO esta ACTUALIZADO, descargamos e instalamos las actualizaciones.
         * TODO ESTO ES PARA EL SERVIDOR DE TEST
         */
        private void btnJugarTest_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Este servidor es puramente de TEST. El mismo podria no ser estable, podria reiniciarse seguido debido a que se actualiza automaticamente por cada cambio que hacemos");
            MessageBox.Show("Cuentas y Personajes podrian ser borrados sin previo aviso. Recomendacion de utilizar un email y password diferente al que utilizan en el servidor REAL");

            BuscarActualizaciones(true);
            startUpdate(true);
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
            OpenUrl("https://discord.gg/e3juVbF");
        }

        private void image_facebook_Click(object sender, RoutedEventArgs e)
        {

            OpenUrl("https://www.facebook.com/ao20oficial/"); 
        }

        private void image_instagram_Click(object sender, RoutedEventArgs e)
        {
            OpenUrl("https://www.instagram.com/ao20oficial/?hl=es");
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Environment.Exit(0);
        }

        private void btnConfiguracion_Click(object sender, RoutedEventArgs e)
        {
            Configuracion configuracion = new Configuracion();
            configuracion.Show();
        }

        private void txtChangelog_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {

        }

        private void OpenUrl(string url)
        {
            try
            {
                Process.Start(url);
            }
            catch
            {
                // hack because of this: https://github.com/dotnet/corefx/issues/10361
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    url = url.Replace("&", "^&");
                    Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    Process.Start("xdg-open", url);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    Process.Start("open", url);
                }
                else
                {
                    throw;
                }
            }
        }

        private void chkLanzarAutomatico_Click(object sender, RoutedEventArgs e)
        {
            WritePrivateProfileString("OPCIONES", "LanzarAutomatico", Convert.ToInt32(chkLanzarAutomatico.IsChecked).ToString(), Configuracion.CONFIG_FILE);
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {

        }
    }
    public class ServerStatus
    {
        public bool ok { get; set; }
        public int onlineCount { get; set; }
    }
}
