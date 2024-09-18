﻿using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.EMMA;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml;

namespace LCMS_ODO_GPS_GENERATOR
{
    //Clase para procesar datos de rugosidad (roughness)
    internal class Roughness
    {

        public Roughness() { }

        public List<double> listaPromediosRight = new List<double>();
        public List<double> listaPromediosLeft = new List<double>();
        public List<Incidencia> listaIncidencias = new List<Incidencia>();
        public string fecha;


        public void recogerSubCarpetas(string ruta)
        {
            try
            {
                //Recojo todas las subcarpetas
                string[] subcarpetas = Directory.GetDirectories(ruta, "*", SearchOption.AllDirectories);

                //Creamos carpeta donde almacenaremos todos los archivos Iri que generemos
                DirectoryInfo dir = new DirectoryInfo(ruta + "\\Archivos");
                dir.Create();

                foreach (string subcarpeta in subcarpetas)
                {
                    string nombreCarpeta = subcarpeta.Substring(subcarpeta.Length - 1);

                    //Le pasamos el nombre de la subcarpeta para que procese los ficheros que contiene
                    procesarArchivosSubcarpeta(subcarpeta, dir.FullName);

                    limpiarListaYFecha();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error función recogerSubCarpetas: " + ex.Message);
            }
        }

        private void limpiarListaYFecha()
        {
            listaPromediosLeft.Clear();
            listaPromediosRight.Clear();
            fecha = string.Empty;
        }

        public void procesarArchivosSubcarpeta(string rutaSubcarpeta, string rutaDestinoCarpetaArchivos)
        {
            string[] archivosXML = Directory.GetFiles(rutaSubcarpeta, "*.xml", SearchOption.TopDirectoryOnly);
            string nombreCarpeta = rutaSubcarpeta.Substring(rutaSubcarpeta.Length - 1);

            foreach (string archivoXML in archivosXML)
            {
                leerRoughnessYFecha(archivoXML);
            }

            //Una vez hemos finalizado de leer todos los archivos de esa carpeta, generamos archivo.
            generarArchivoIRI(rutaDestinoCarpetaArchivos, nombreCarpeta);
            generarArchivoEvent(rutaDestinoCarpetaArchivos, nombreCarpeta);
        }

        private void generarArchivoEvent(string rutaDestinoCarpetaArchivos, string nombreCarpeta)
        {
            string nomCarpMod = modificarNomCarp(nombreCarpeta);
            string nombreArchivo = rutaDestinoCarpetaArchivos + "\\" + "LCMS" + fecha + nomCarpMod + ".dist.evt.txt";

            try
            {
                using (StreamWriter escritor = new StreamWriter(nombreArchivo))
                {
                    //Headers:
                    //Re - calibrationfile: 

                    //Parameters:
                    //Raw time: no
                    //Raw distance: no

                    //Data:
                    //Name, Type, Code, Index, Active, Distance[m], Distance toward[m], Distance after[m], Text, Lasers, Latitude, Longitude, Height, GeoHeight, DOP, Satellites

                    escritor.WriteLine("Headers:");
                    escritor.WriteLine("Re - calibrationfile:");
                    escritor.WriteLine();
                    escritor.WriteLine("Parameters:");
                    escritor.WriteLine("Raw time: no");
                    escritor.WriteLine("Raw distance: no");
                    escritor.WriteLine();
                    escritor.WriteLine("Data:");
                    escritor.WriteLine("Name, Type, Code, Index, Active, Distance[m], Distance toward[m], Distance after[m], Text, Lasers, Latitude, Longitude, Height, GeoHeight, DOP, Satellites");

                    int cont = 0;
                    int PK = 1;
                    while (listaIncidencias.Count > cont)
                    {
                        escritor.WriteLine("Event, " + listaIncidencias[cont].incidencia + ", 0, 0, yes, " + listaIncidencias[cont].distanciaCab + ", , , " + "PK" + PK + ", , 0.0, 0.0, 0.0, 0.0, 0.0, 0");
                        cont++;
                        PK++;
                    }

                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al generar archivo event: " + ex.Message);
            }
        }

        private void generarArchivoIRI(string ruta, string nombreCarpeta)
        {
            string nomCarpMod = modificarNomCarp(nombreCarpeta);

            string nombreArchivo = ruta + "\\" + "LCMS" + fecha + nomCarpMod + ".dist.iri.txt";
            try
            {
                using (StreamWriter escritor = new StreamWriter(nombreArchivo))
                {
                    int cont = 0;
                    int metros = 0;
                    //Headers:
                    //Re - calibrationfile: 

                    //Parameters:
                    //Lasers: 1,2
                    //Graphical interval: 100.0000 m
                    //Velocity: 80.0000 km / h
                    //Suspension: 63.3000
                    //Tyre: 653.0000
                    //Damping: 6.0000
                    //Unsprung mass: 0.1500
                    //Include vertical beam displacement: yes
                    //Include beam rotation: yes
                    //Result interval: 10.0000 m

                    //Data:
                    //Distance[m], IRI(1)[m / km], IRI(2)[m / km]

                    escritor.WriteLine("Headers:");
                    escritor.WriteLine("Re - calibrationfile:");
                    escritor.WriteLine();
                    escritor.WriteLine("Parameters:");
                    escritor.WriteLine("Lasers: 1,2");
                    escritor.WriteLine("Graphical interval: 100.0000 m");
                    escritor.WriteLine("Velocity: 80.0000 km / h");
                    escritor.WriteLine("Suspension: 63.3000");
                    escritor.WriteLine("Tyre: 653.0000");
                    escritor.WriteLine("Damping: 6.0000");
                    escritor.WriteLine("Unsprung mass: 0.1500");
                    escritor.WriteLine("Include vertical beam displacement: yes");
                    escritor.WriteLine("Include beam rotation: yes");
                    escritor.WriteLine("Result interval: 10.0000 m:");
                    escritor.WriteLine();
                    escritor.WriteLine("Data:");
                    escritor.WriteLine("Distance [m], IRI (1) [m/km], IRI (2) [m/km]");

                    while (listaPromediosLeft.Count > cont)
                    {
                        escritor.WriteLine(metros + ", " + listaPromediosRight[cont].ToString().Replace(",", ".") + ", " + listaPromediosLeft[cont].ToString().Replace(",", "."));
                        cont++;
                        metros += 10;
                    }
                    // Puedes agregar más líneas o información al archivo
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error en función generarArchivo: " + ex.Message);
            }

        }

        //Funcion para agregar 0 al nombre de la carpeta, que se usará para crear el nombre del archivo
        private string modificarNomCarp(string nombreCarpeta)
        {
            string nomCarpMod;
            switch (nombreCarpeta.Length)
            {
                case 1:
                    nomCarpMod = "000" + nombreCarpeta;
                    break;
                case 2:
                    nomCarpMod = "00" + nombreCarpeta;
                    break;
                case 3:
                    nomCarpMod = "0" + nombreCarpeta;
                    break;
                case 4:
                    nomCarpMod = nombreCarpeta;
                    break;
                default:
                    nomCarpMod = nombreCarpeta;
                    break;
            }

            return nomCarpMod;
        }

        public void leerRoughnessYFecha(string archivoXML)
        {
            try
            {
                string iri = "";
                string[] listaDatosIri;

                using (XmlReader reader = XmlReader.Create(archivoXML))
                {
                    var leftLeido = false;
                    string nombreArchivo = Path.GetFileName(archivoXML);
                    Incidencia inc = new Incidencia();

                    // Leer el archivo XML secuencialmente
                    while (reader.Read())
                    {

                        // Detectar el inicio del nodo <DateAndTime>
                        if (reader.NodeType == XmlNodeType.Element && reader.Name == "DateAndTime")
                        {
                            fecha = reader.ReadElementContentAsString();
                            fecha = convertirFecha(fecha);
                        }

                        // Detectar el inicio del nodo <UserEventInformation>
                        if (reader.NodeType == XmlNodeType.Element && reader.Name == "UserEventInformation")
                        {
                            string valorIncidencia = reader.ReadElementContentAsString();
                            valorIncidencia = valorIncidencia.Substring(valorIncidencia.Length - 1);

                            int dist = Convert.ToInt32(nombreArchivo.Substring(11, 6));
                            inc.distanciaCab = dist * 10;
                            inc.incidencia = Convert.ToInt32(valorIncidencia);
                            listaIncidencias.Add(inc);
                        }

                        // Detectar el inicio del nodo <RoughnessInformation>
                        if (reader.NodeType == XmlNodeType.Element && reader.Name == "Roughness")
                        {
                            // Leer internamente los nodos <Longitude> y <Latitude> dentro de <SectionPosition>
                            while (reader.Read())
                            {
                                //Gracias a la variable leftLeido, podemos añadir el promedio a la lista de promedio de la derecha o a la de la izquierda
                                //El menor (es decir el del lado izquierdo) siempre será el primero que leamos, por lo tanto una vez que leemos el primer IRI, sabemos
                                //que es el de la izquierda, y el siguiente será el de la derecha
                                if (reader.NodeType == XmlNodeType.Element && reader.Name == "IRI" && !leftLeido)
                                {
                                    iri = reader.ReadElementContentAsString();
                                    listaDatosIri = iri.Split(' ');
                                    double promedioDatosIri = getPromedioDatosIRI(listaDatosIri);
                                    listaPromediosLeft.Add(promedioDatosIri);
                                    leftLeido = true;

                                }
                                else if (reader.NodeType == XmlNodeType.Element && reader.Name == "IRI" && leftLeido)
                                {
                                    iri = reader.ReadElementContentAsString();
                                    listaDatosIri = iri.Split(' ');
                                    double promedioDatosIri = getPromedioDatosIRI(listaDatosIri);
                                    listaPromediosRight.Add(promedioDatosIri);
                                    leftLeido = false;
                                }

                                // Salir del bucle interno si se alcanza el final de <Roughness>
                                //if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "Roughness")
                                //{
                                //    break;
                                //}
                            }

                        }
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("Error de lectura en el fichero " + archivoXML + " " + e.Message);
            }
        }

        private string convertirFecha(string fecha)
        {
            string fechaModificada = fecha.Substring(0, 10);
            fechaModificada = fechaModificada.Replace("/", "");
            return fechaModificada;
        }

        private double getPromedioDatosIRI(string[] listaDatosIri)
        {
            List<double> listaDatosDouble = new List<double>();
            foreach (string dato in listaDatosIri)
            {
                if (!String.IsNullOrEmpty(dato))
                {
                    double valor = Convert.ToDouble(dato, CultureInfo.InvariantCulture);
                    listaDatosDouble.Add(valor);
                }

            }

            double promedio = listaDatosDouble.Average();

            return promedio;
        }
    }
}