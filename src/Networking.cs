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
        public static string HOST = "http://autoupdate.ao20.com.ar/";
        private readonly string VERSIONFILE_URI = HOST + "\\Version.json";

        private readonly List<string> EXCEPCIONES = new List<string>() {
            "/Recursos/Configuracion.ini",
            "/Recursos/Teclas.ini",
            "/Recursos/Version.json",
        };

        // Acá está la info. del VersionInfo.json
        public VersionInformation versionRemota; // Acá como objeto de-serializado.
        public string versionRemotaString;      // Acá como texto plano.

        public List<string> fileQueue = new List<string>();
        public TaskCompletionSource<bool> downloadQueue;

        /**
         * Comprueba la ultima version disponible
         */
        public List<string> CheckOutdatedFiles()
        {
            // Obtenemos los datos necesarios del servidor.
            VersionInformation versionRemota = Get_RemoteVersion();

            // Si no existe VersionInfo.json en la carpeta Init, ...
            VersionInformation versionLocal;
            if (!File.Exists(IO.VERSIONFILE_PATH))
            {
                // ... parseamos el string que obtuvimos del servidor.
                versionLocal = IO.Get_LocalVersion(versionRemotaString);
            }
            else // Si existe, ...
            {
                // ... buscamos y parseamos el que está en la carpeta Init.
                versionLocal = IO.Get_LocalVersion(null);
            }

            VersionInformation.File archivoLocal, archivoRemoto;

            // Itero la lista de archivos del servidor y lo comparo con lo que tengo en local.
            for (int i = 0; i < versionRemota.Files.Count; i++)
            {
                archivoLocal = versionLocal.Files[i];
                archivoRemoto = versionRemota.Files[i];

                // Si existe el archivo, comparamos el MD5..
                if (File.Exists(App.ARGENTUM_FILES + archivoRemoto.name))
                {
                    // Si NO coinciden los hashes, ...
                    if (!EXCEPCIONES.Contains(archivoRemoto.name) && 
                        IO.checkMD5(archivoLocal.name) != archivoRemoto.checksum)
                    {
                        // ... lo agrego a la lista de archivos a descargar.
                        fileQueue.Add(archivoRemoto.name);
                    }
                }
                else // Si existe el archivo, ...
                {
                    // ... lo agrego a la lista de archivos a descargar.
                    fileQueue.Add(archivoRemoto.name);
                }
            }

            // Guardo en un field el objeto de-serializado de la info. remota.
            this.versionRemota = versionRemota;

            return fileQueue;
        }

        public VersionInformation Get_RemoteVersion()
        {
            // Envio un GET al servidor con el JSON de el archivo de versionado.
            WebClient webClient = new WebClient();
            try
            {
                versionRemotaString = webClient.DownloadString(VERSIONFILE_URI);
            }
            catch (WebException error)
            {
                MessageBox.Show(error.Message);
            }
            finally
            {
                webClient.Dispose();
            }

            // Me fijo que la response NO ESTÉ vacía.
            if (versionRemotaString == null)
            {
                MessageBox.Show("Hemos recibido una respuesta vacía del servidor. Contacta con un administrador :'(");
                Environment.Exit(0);
            }

            // Deserializamos el Version.json remoto
            VersionInformation versionRemota = null;
            try
            {
                versionRemota = JsonSerializer.Deserialize<VersionInformation>(versionRemotaString);
            }
            catch (JsonException)
            {
                MessageBox.Show("Error al de-serializar: El Version.json del servidor tiene un formato inválido.");
            }

            return versionRemota;
        }

        public void CrearCarpetasRequeridas()
        {
            foreach(string folder in versionRemota.Folders)
            {
                string currentFolder = App.ARGENTUM_FILES + folder;

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
        public async Task IniciarDescarga(WebClient webClient)
        {
            //files contains all URL links
            foreach (string file in fileQueue)
            {
                downloadQueue = new TaskCompletionSource<bool>();

                webClient.DownloadFileAsync(new Uri(HOST + "/updater2/" + file), App.ARGENTUM_FILES + file);

                await downloadQueue.Task;
            }
            downloadQueue = null;
        }
    }
}
