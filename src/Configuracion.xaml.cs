using IniParser;
using IniParser.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Launcher.src
{
    /// <summary>
    /// Lógica de interacción para Configuracion.xaml
    /// </summary>
    public partial class Configuracion : Window
    {
        string path = "VER PATH";
        AOCfg AOCfg = new AOCfg();
        public Configuracion()
        {
            InitializeComponent();
            cargarConfiguraciones();
            actualizarCheckboxs();
        }

        private void cargarConfiguraciones()
        {
            var parser = new FileIniDataParser();
            //VER BIEN LA RUTA Y USAR LA VARIABLE PATH DEFINIDA ARRIBA
            IniData file = parser.ReadFile(Directory.GetCurrentDirectory() + "/configuracion.ini");
            AOCfg.PantallaCompleta = Convert.ToBoolean(Convert.ToInt32(file["VIDEO"]["PantallaCompleta"]));
            AOCfg.Musica = Convert.ToBoolean(Convert.ToInt32(file["AUDIO"]["Musica"]));
            AOCfg.PrecargaGrafica = Convert.ToBoolean(Convert.ToInt32(file["VIDEO"]["UtilizarPreCarga"]));
            AOCfg.PunterosGraficos = Convert.ToBoolean(Convert.ToInt32(file["VIDEO"]["CursoresGraficos"]));
            AOCfg.VSync = Convert.ToBoolean(Convert.ToInt32(file["VIDEO"]["VSync"]));
            AOCfg.Efectos = Convert.ToBoolean(Convert.ToInt32(file["AUDIO"]["Fx"]));
        }

        private void actualizarCheckboxs()
        {
            chkPantallaCompleta.IsChecked = AOCfg.PantallaCompleta;
            chkEfectos.IsChecked = AOCfg.Efectos;
            chkMusica.IsChecked = AOCfg.Musica;
            chkPrecargaGrafica.IsChecked = AOCfg.PrecargaGrafica;
            chkPunterosGraficcos.IsChecked = AOCfg.PunterosGraficos;
            chkSincronizacionVertical.IsChecked = AOCfg.VSync;
        }

        private void btnAceptar_click(object sender, RoutedEventArgs e)
        {
            var parser = new FileIniDataParser();
            //VER BIEN LA RUTA Y USAR LA VARIABLE PATH DEFINIDA ARRIBA
            IniData file = parser.ReadFile(Directory.GetCurrentDirectory() + "/configuracion.ini");

            file["VIDEO"]["PantallaCompleta"] = Convert.ToInt32(chkPantallaCompleta.IsChecked).ToString();
            file["AUDIO"]["Musica"] = Convert.ToInt32(chkMusica.IsChecked).ToString();
            file["VIDEO"]["UtilizarPreCarga"] = Convert.ToInt32(chkPrecargaGrafica.IsChecked).ToString();
            file["VIDEO"]["CursoresGraficos"] = Convert.ToInt32(chkPunterosGraficcos.IsChecked).ToString();
            file["VIDEO"]["VSync"] = Convert.ToInt32(chkSincronizacionVertical.IsChecked).ToString();
            file["AUDIO"]["Fx"] = Convert.ToInt32(chkEfectos.IsChecked).ToString();

            parser.WriteFile(Directory.GetCurrentDirectory() + "/configuracion.ini", file);

            this.Close();

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }

    public class AOCfg
    {
        public bool PantallaCompleta { get; set; }
        public bool Efectos { get; set; }
        public bool Musica { get; set; }
        public bool PunterosGraficos { get; set; }
        public bool PrecargaGrafica { get; set; }
        public bool VSync { get; set; }
    }
}
