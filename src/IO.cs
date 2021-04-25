using System;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using System.Windows;

namespace Launcher.src
{
    class IO
    {
        public bool Actualizando = false;
        public int ArchivosDesactualizados = 0;
        public int ArchivoActual = 0;
        /**
         * Calcula el checksum MD5
         */
        public static string checkMD5(string filename)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(filename))
                {
                    var hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLower();
                }
            }
        }

    }
}
