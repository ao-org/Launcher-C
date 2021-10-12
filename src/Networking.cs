using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;

namespace Launcher.src
{
    class Networking
    {
        public static string ROOT_HOST_PATH = "http://parches.ao20.com.ar/files/";
        public static string ROOT_HOST_PATH_TEST = "http://parches.ao20.com.ar/files-test/";
        private readonly string VERSION_JSON_PATH = ROOT_HOST_PATH + "Version.json";
        private readonly string VERSION_JSON_PATH_TEST = ROOT_HOST_PATH_TEST + "Version.json";


        public static string API_PATH = "http://api.ao20.com.ar/";
        private readonly List<string> EXCEPCIONES = new List<string>() {
            "Argentum20\\Recursos\\OUTPUT\\Configuracion.ini",
            "Argentum20\\Recursos\\OUTPUT\\Teclas.ini",
            "Argentum20-test\\Recursos\\OUTPUT\\Configuracion.ini",
            "Argentum20-test\\Recursos\\OUTPUT\\Teclas.ini"
        };

        // Acá está la info. del VersionInfo.json
        public VersionInformation versionRemota; // Acá como objeto de-serializado.
        public string versionRemotaString;      // Acá como texto plano.

        public List<string> fileQueue = new List<string>();
        public TaskCompletionSource<bool> downloadQueue;

        /**
         * Comprueba la ultima version disponible
         */
        public async Task<List<string>> CheckOutdatedFiles(bool isTestDownload = false)
        {
            try
            {
                fileQueue.Clear();

                // Obtenemos los datos necesarios del servidor.
                VersionInformation versionRemota = await Get_RemoteVersion(isTestDownload);
 

                if (versionRemota == null) versionRemota = new VersionInformation();
                // Itero la lista de archivos del servidor y lo comparo con lo que tengo en local.
                foreach (string filename in versionRemota.Files.Keys)
                {
                        if (filename.Contains("LauncherAO20.dl_") || filename.Contains("LauncherAO20.ex_") || File.Exists(App.ARGENTUM_PATH + filename))
                        {


                            // Si NO coinciden los hashes, ...
                            if (!EXCEPCIONES.Contains(filename))
                            {
                                if (filename.Contains("LauncherAO20.dl_"))
                                {
#if DEBUG
                                    if (IO.checkMD5(App.ARGENTUM_PATH + "netcoreapp3.1\\LauncherAO20.dll").ToLower() != versionRemota.Files["Launcher\\LauncherAO20.dl_"].ToLower())
                                    {
                                        fileQueue.Add(filename);
                                        fileQueue.Add("netcoreapp3.1\\LauncherAO20.ex_");
                                    }
#endif
#if !DEBUG
                            if (IO.checkMD5(App.ARGENTUM_PATH + "Launcher\\LauncherAO20.dll").ToLower() != versionRemota.Files["Launcher\\LauncherAO20.dl_"].ToLower())
                            {
                                fileQueue.Add(filename);
                                fileQueue.Add("Launcher\\LauncherAO20.ex_");
                            }
#endif
                                }
                                else if (filename.Contains("LauncherAO20.ex_"))
                                {

                                }
                                else if (IO.checkMD5(App.ARGENTUM_PATH + filename).ToLower() != versionRemota.Files[filename].ToLower())
                                {
                                    fileQueue.Add(filename);
                                }
                            }
                        }
                        else // Si no existe el archivo ...
                        {
                            // ... lo agrego a la lista de archivos a descargar.
                            fileQueue.Add(filename);
                        }
                   
                }

                // Guardo en un field el objeto de-serializado de la info. remota.
                this.versionRemota = versionRemota;
                return fileQueue;
            }
            catch (Exception)
            {
                return new List<string>();
            }

        }

        public async Task<VersionInformation> Get_RemoteVersion(bool isTestDownload = false)
        {
            WebClient webClient = new WebClient();

            try
            {
                // Envio un GET al servidor con el JSON de el archivo de versionado.
                if (isTestDownload)
                {
                    versionRemotaString = await webClient.DownloadStringTaskAsync(VERSION_JSON_PATH_TEST);
                    versionRemotaString = versionRemotaString.Replace("Argentum20", "Argentum20-test");
                }
                else
                {
                    versionRemotaString = await webClient.DownloadStringTaskAsync(VERSION_JSON_PATH);
                }

                // Me fijo que la response NO ESTÉ vacía.
                if (versionRemotaString == null)
                {
                    MessageBox.Show("Hemos recibido una respuesta vacía del servidor. Contacta con un administrador :'(");
                    Environment.Exit(0);
                }

                // Deserializamos el Version.json remoto
                return JsonSerializer.Deserialize<VersionInformation>(versionRemotaString);
            }
            catch (WebException error)
            {
                if (error.Status == WebExceptionStatus.ProtocolError)
                {
                    MessageBox.Show("No se pudo actualizar el launcher, intente más tarde");
                }
            }
            catch (JsonException)
            {
                MessageBox.Show("Has recibido una respuesta invalida por parte del servidor.");
            }
            finally
            {
                webClient.Dispose();
            }

            return new VersionInformation();
        }

        public void CrearCarpetasRequeridas()
        {
            foreach (string folder in versionRemota.Folders)
            {
                string currentFolder = App.ARGENTUM_PATH + "\\" + folder;

                if (!Directory.Exists(currentFolder))
                {
                    Directory.CreateDirectory(currentFolder);
                }
            }
        }

        /**
         * ADVERTENCIA: Esto es parte de el método DescargarActualizaciones() en MainWindow.xaml.cs
         *              NO EJECUTAR DIRECTAMENTE, HACERLO A TRAVÉS DE ESE METODO!
         *              
         * Fuente: https://stackoverflow.com/questions/39552021/multiple-asynchronous-download-with-progress-bar
         */
        public async Task IniciarDescarga(WebClient webClient, bool isTestDownload = false)
        {
            Uri uriDescarga;
            //files contains all URL links
            foreach (string file in fileQueue)
            {
                downloadQueue = new TaskCompletionSource<bool>();

                string host = ROOT_HOST_PATH;
                string fileOk = file;
                if (isTestDownload)
                {
                    host = ROOT_HOST_PATH_TEST;
                    fileOk = file.Replace("Argentum20-test", "Argentum20");
                }

                uriDescarga = new Uri(host + fileOk);

                webClient.DownloadFileAsync(uriDescarga, App.ARGENTUM_PATH + file);

                await downloadQueue.Task;
            }
            downloadQueue = null;
        }
    }
}
