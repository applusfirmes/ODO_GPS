﻿using ClosedXML.Excel;
using Irony;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

namespace LCMS_ODO_GPS_GENERATOR
{
    public class Incidencia
    {
        public int distanciaCab;
        public int pk;
        public int incidencia;
        public double latitude;
        public double longitude;

    }

    internal class IncidenciaController
    {

        public IncidenciaController() { }
        public List<Incidencia> listaIncidencias = new List<Incidencia>();

        //public Dictionary<string, string> map = new Dictionary<string, string>();

        public void leerArchivosXML(string ruta)
        {
            try
            {
                //Recojo todas las subcarpetas
                string[] subcarpetas = Directory.GetDirectories(ruta, "*", SearchOption.AllDirectories);
                DirectoryInfo dir = new DirectoryInfo(ruta + "\\ArchivosIncidencias");
                dir.Create();
                string destino = dir.FullName;

                foreach (string subcarpeta in subcarpetas)
                {
                    //Recogemos nombre de la carpeta
                    string nombreCarpeta = Path.GetFileName(subcarpeta);

                    // Procesar todos los archivos XML dentro de la subcarpeta actual
                    // Comprobamos si tiene archivos, si no tiene, ignoramos carpeta
                    if (procesarArchivosXML(subcarpeta))
                    {
                        generarArchivoExcel(nombreCarpeta, destino);
                    }

                    limpiarLista();

                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ocurrió un error función leerArchivosXML, IncidenciaController: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private bool procesarArchivosXML(string rutaSubcarpeta)
        {
            var contieneArchivos = false;

            try
            {
                // Obtener todos los archivos XML de la subcarpeta actual
                string[] archivosXML = Directory.GetFiles(rutaSubcarpeta, "*.xml", SearchOption.TopDirectoryOnly);
                if (archivosXML.Length > 0)
                {
                    contieneArchivos = true;
                    foreach (string archivoXML in archivosXML)
                    {
                        leerIncidenciaYLatLog(archivoXML);
                    }
                }

                return contieneArchivos;
            }
            catch (Exception ex)
            {
                MessageBox.Show("ERROR en función ProcesarArchivosXML: " + ex.Message);
                return false;
            }
            
        }

        private void generarArchivoExcel(string nombreCarpeta, string destino)
        {
            try
            {
                XLWorkbook libro = new XLWorkbook();

                List<List<string>> estructuraBase = new List<List<string>>();

                ClosedXML.Excel.IXLWorksheet hoja = libro.Worksheets.Add("Lista de incidencias");

                //dicEstilos.Add(TITULO1, new estilo() { fontSize = 10, fontName = "Arial", bold = true, colorContenido = XLColor.Black, borde = XLBorderStyleValues.None, bordeColor = XLColor.NoColor, bordeInterno = XLBorderStyleValues.None, bordeColorInterno = XLColor.NoColor });

                hoja.Cell(1, 2).Value = "Distancia cabecera";
                hoja.Cell(1, 2).Style.Fill.SetBackgroundColor(XLColor.PeachOrange);
                hoja.Cell(1, 2).Style.Font.Bold = true;
                hoja.Cell(1, 2).Style.Alignment.WrapText = true;


                hoja.Cell(1, 3).Value = "Longitud";
                hoja.Cell(1, 3).Style.Fill.SetBackgroundColor(XLColor.PeachOrange);
                hoja.Cell(1, 3).Style.Font.Bold = true;

                hoja.Cell(1, 4).Value = "PK";
                hoja.Cell(1, 4).Style.Fill.SetBackgroundColor(XLColor.PeachOrange);
                hoja.Cell(1, 4).Style.Font.Bold = true;

                hoja.Cell(1, 5).Value = "Latitude";
                hoja.Cell(1, 5).Style.Fill.SetBackgroundColor(XLColor.PeachOrange);
                hoja.Cell(1, 5).Style.Font.Bold = true;

                hoja.Cell(1, 6).Value = "Longitude";
                hoja.Cell(1, 6).Style.Fill.SetBackgroundColor(XLColor.PeachOrange);
                hoja.Cell(1, 6).Style.Font.Bold = true;

                hoja.Cell(1, 7).Value = "Incidencia";
                hoja.Cell(1, 7).Style.Fill.SetBackgroundColor(XLColor.PeachOrange);
                hoja.Cell(1, 7).Style.Font.Bold = true;

                int row = 2;
                int pk = 0;

                for (int i = 0; listaIncidencias.Count > i; i++)
                {
                    Incidencia inc = listaIncidencias[i];
                    hoja.Cell(row, 2).Value = inc.distanciaCab;

                    if (i == 0)
                        hoja.Cell(row, 3).Value = inc.distanciaCab;//Longitud
                    else
                        hoja.Cell(row, 3).Value = inc.distanciaCab - listaIncidencias[i - 1].distanciaCab; //Longitud , resta de distanciaCab[i] y distanciaCab[i-1]

                    hoja.Cell(row, 4).Value = pk;
                    hoja.Cell(row, 5).Value = inc.latitude;
                    hoja.Cell(row, 6).Value = inc.longitude;
                    hoja.Cell(row, 7).Value = inc.incidencia; //Devolvera el num incidencia
                    row++;

                    //Solo sumamos PK si se trata de incidencias 9 o 1
                    if (inc.incidencia == 9 || inc.incidencia == 1) pk++;

                }

                string _nombreArchivo = destino + "\\" + nombreCarpeta + ".xlsx";
                libro.SaveAs(_nombreArchivo);
            }
            catch (Exception ex)
            {
                MessageBox.Show("ERROR en función generarArchivoExcel: " + ex.Message);
            }
            
        }

        private void leerIncidenciaYLatLog(string rutaArchivoXML)
        {
            try
            {
                using (XmlReader reader = XmlReader.Create(rutaArchivoXML))
                {
                    double longitud = 0;
                    double latitud = 0;

                    string nombreArchivo = Path.GetFileName(rutaArchivoXML);

                    // Leer el archivo XML secuencialmente
                    Incidencia inc = new Incidencia();

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
                                    //inc.longitude = longitud;
                                }
                                else if (reader.NodeType == XmlNodeType.Element && reader.Name == "Latitude")
                                {
                                    latitud = Convert.ToDouble(reader.ReadElementContentAsString(), CultureInfo.InvariantCulture);
                                    //inc.latitude = latitud;
                                }

                                // Salir del bucle interno si se alcanza el final de <SectionPosition>
                                if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "SectionPosition")
                                {
                                    break;
                                }
                            }
                        }

                        // Detectar el inicio del nodo <UserEventInformation>
                        if (reader.NodeType == XmlNodeType.Element && reader.Name == "UserEventInformation")
                        {
                            inc.longitude = longitud;
                            inc.latitude = latitud;


                            string valorIncidencia = reader.ReadElementContentAsString();
                            valorIncidencia = valorIncidencia.Substring(valorIncidencia.Length - 1);

                            int dist = Convert.ToInt32(nombreArchivo.Substring(11, 6));
                            inc.distanciaCab = dist * 10;
                            inc.incidencia = Convert.ToInt32(valorIncidencia);
                            listaIncidencias.Add(inc);

                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Función leerIncidenciaYLatLog, ERROR: " + ex.Message);
            }
        }

        private void limpiarLista()
        {
            listaIncidencias.Clear();
        }

    }
}
