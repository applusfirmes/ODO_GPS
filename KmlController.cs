using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

namespace LCMS_ODO_GPS_GENERATOR
{
    internal class KmlController
    {
        public KmlController() { }

        public List<double> listaLatitud = new List<double>();
        public List<double> listaLongitud = new List<double>();



        public void getCarpetas(string ruta)
        {
            try
            {
                // Obtener todas las subcarpetas y archivos XML de manera recursiva
                foreach (string archivoXML in Directory.GetFiles(ruta, "*.xml", SearchOption.AllDirectories))
                {
                    // Aquí puedes procesar cada archivo XML
                    // Por ejemplo, leer el contenido del archivo
                    string contenidoXML = File.ReadAllText(archivoXML);
                    Console.WriteLine($"Procesando archivo: {archivoXML}");

                    // Añade tu lógica para procesar el contenido XML aquí
                }
            }
            catch (Exception ex)
            {
                // Manejo de errores
                MessageBox.Show($"Ocurrió un error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void leerArchivosXML(string ruta)
        {
            try
            {
                string[] subcarpetas = Directory.GetDirectories(ruta, "*", SearchOption.AllDirectories);
                DirectoryInfo dir = new DirectoryInfo(ruta + "\\ArchivosKML");
                dir.Create();
                string destino = dir.FullName;

                foreach (string subcarpeta in subcarpetas)
                {
                    string nombreCarpeta = subcarpeta.Substring(subcarpeta.Length - 1);

                    // Procesar todos los archivos XML dentro de la subcarpeta actual
                    // Comprobamos si tiene archivos, si no tiene, ignoramos carpeta
                    if (procesarArchivosXML(subcarpeta))
                    {
                        generarArchivoKML(listaLatitud, listaLongitud, nombreCarpeta, destino);
                    }
                    
                    limpiarListas();

                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ocurrió un error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private bool procesarArchivosXML(string rutaSubcarpeta)
        {
            var contieneArchivos = false;

            // Obtener todos los archivos XML de la subcarpeta actual
            string[] archivosXML = Directory.GetFiles(rutaSubcarpeta, "*.xml", SearchOption.TopDirectoryOnly);
            if (archivosXML.Length>0)
            {
                contieneArchivos = true;
                foreach (string archivoXML in archivosXML)
                {
                    leerLongitudLatitud(archivoXML);
                }
            }

            return contieneArchivos;            
        }

        private void leerLongitudLatitud(string rutaArchivo)
        {
            try
            {
                using (XmlReader reader = XmlReader.Create(rutaArchivo))
                {
                    double longitud = 0;
                    double latitud = 0;

                    // Leer el archivo XML secuencialmente
                    while (reader.Read())
                    {
                        // Detectar el inicio del nodo <SectionPosition>
                        if (reader.NodeType == XmlNodeType.Element && reader.Name == "SectionPosition")
                        {
                            // Leer internamente los nodos <Longitude> y <Latitude> dentro de <SectionPosition>
                            while (reader.Read())
                            {
                                if (reader.NodeType == XmlNodeType.Element && reader.Name == "Longitude")
                                {
                                    longitud = Convert.ToDouble(reader.ReadElementContentAsString(), CultureInfo.InvariantCulture);
                                    listaLongitud.Add(longitud);
                                }
                                else if (reader.NodeType == XmlNodeType.Element && reader.Name == "Latitude")
                                {
                                    latitud = Convert.ToDouble(reader.ReadElementContentAsString(), CultureInfo.InvariantCulture);
                                    listaLatitud.Add(latitud);
                                }

                                // Salir del bucle interno si se alcanza el final de <SectionPosition>
                                if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "SectionPosition")
                                {
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Manejo de errores en la lectura del archivo XML
                Console.WriteLine($"Error al procesar el archivo {rutaArchivo}: {ex.Message}");
            }
        }

        private void generarArchivoKML(List<double> listaLatitud, List<double> listaLongitud, string nombreCarpeta, string rutaDestino)
        {
            Microsoft.Win32.SaveFileDialog saveFileDialog = new Microsoft.Win32.SaveFileDialog();
            saveFileDialog.DefaultExt = "kml";
            saveFileDialog.AddExtension = true;
            saveFileDialog.FileName = "KML_"+ nombreCarpeta;

            String archivo_xml = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                "\r\n<kml xmlns=\"http://www.opengis.net/kml/2.2\">\r\n  " +
                "<Document>\r\n<name>Carpeta_" + nombreCarpeta + "</name>\r\n";

            //Estilo de linea
            archivo_xml +="<Style id=\"lineColor1\">\r\n" +
                "<LineStyle>\r\n<color>ffffaa00</color>\r\n<width>2</width>\r\n</LineStyle>\r\n</Style>";

            //Estilo de punto de inicio
            archivo_xml += "<Style id=\"startPointStyle\">\r\n<IconStyle>\r\n<color>ff55ff00</color>\r\n<Icon>\r\n<href>http://www.earthpoint.us/Dots/GoogleEarth/pal4/icon25.png</href>\r\n</Icon>\r\n</IconStyle>\r\n</Style>";

            //Marcamos el inicio
            archivo_xml += "<Placemark>\r\n<name>Inicio</name>\r\n<styleUrl>#startPointStyle</styleUrl>\r\n<Point>\r\n<coordinates>" + listaLongitud[0].ToString().Replace(",", ".") + "," + listaLatitud[0].ToString().Replace(",", ".") + ",0\r\n" + "</coordinates>\r\n</Point>\r\n</Placemark>";

            //Empezamos ruta
            archivo_xml+= "<Placemark>\r\n<name>Ruta" + nombreCarpeta + "</name>\r\n<styleUrl>#lineColor1</styleUrl>\r\n<LineString>\r\n<tessellate>1</tessellate>\r\n<coordinates>";

            int cont = 0;
            string _fileName = rutaDestino+"\\"+saveFileDialog.FileName+".kml";

            using (StreamWriter file = new StreamWriter(_fileName, false, Encoding.GetEncoding("iso-8859-1")))
            {
                
                while (cont<listaLongitud.Count)
                {
                    archivo_xml += listaLongitud[cont].ToString().Replace(",", ".") + "," + listaLatitud[cont].ToString().Replace(",", ".") + ",0\r\n";
                    cont++;
                }

                archivo_xml += "</coordinates>\r\n</LineString>\r\n</Placemark>";

                file.WriteLine(archivo_xml);
                file.WriteLine("</Document>");
                file.WriteLine("</kml>");
                file.Flush();
                file.Dispose();
                file.Close();
            }



        }

        private void limpiarListas()
        {
            listaLatitud.Clear();
            listaLongitud.Clear();
        }

    }
}
