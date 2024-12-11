using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.EMMA;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;
using LCMS_ODO_GPS_GENERATOR.Objetos;
using LCMS_ODO_GPS_GENERATOR.Vistas;
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
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TaskbarClock;
using Path = System.IO.Path;

namespace LCMS_ODO_GPS_GENERATOR
{
    //Clase para procesar datos de rugosidad (roughness)
    internal class Roughness
    {

        public Roughness() { }

        public List<double> listaPromediosRight = new List<double>();
        public List<double> listaPromediosLeft = new List<double>();
        public List<Incidencia> listaIncidencias = new List<Incidencia>();
        public List<double> listaLatitud = new List<double>();
        public List<double> listaLongitud = new List<double>();
        public List<double> listaValoresErdLeft = new List<double>();
        public List<double> listaValoresErdRight = new List<double>();
        public List<String> listaFechas = new List<String>(); //Guardamos todas las fechas de los XML para hacer calculos que tendremos que escribir al generar archivo gpsimp (colum Time)

        public double valorASumar;
        public bool generarFicheroPro;

        private string fecha = "";

        public void procesarCarpetasConfiguradas(string ruta, List<CarpetaConf> lista, bool generarFicheroPro)
        {
            //Creamos carpeta donde almacenaremos todos los archivos Iri que generemos
            DirectoryInfo dir = new DirectoryInfo(ruta + "\\Archivos");
            dir.Create();
            this.generarFicheroPro = generarFicheroPro;
            //Recorremos todas las carpetas del GRID, que estan en la variable 'lista'
            foreach (CarpetaConf carp in lista)
            {
                //Le pasamos el objeto Carpetaconf, que contiene la ruta para que procese los ficheros que contiene
                procesarArchivosSubcarpeta(carp, dir.FullName);

                //Antes de empezar a escribir el siguiente fichero referente a la carpeta siguiente, reiniciamos datos
                limpiarListasYFecha();
            }
        }

        public void recogerSubCarpetas(string ruta)
        {
            try
            {
                var listaActualizada = new List<CarpetaConf>();

                //Recojo todas las subcarpetas
                string[] subcarpetas = Directory.GetDirectories(ruta, "*", SearchOption.AllDirectories);

                //Genero Grid con carpetas
                List<CarpetaConf> listaCarpetasConf = new List<CarpetaConf>();

                foreach (string subcarpeta in subcarpetas)
                {
                    string nombreCarpeta = Path.GetFileName(subcarpeta);
                    CarpetaConf carpetaConf = new CarpetaConf(nombreCarpeta, subcarpeta);

                    listaCarpetasConf.Add(carpetaConf);
                }

                Application.Current.Dispatcher.Invoke(() =>
                {
                    var ventana = new GridCarpetas(listaCarpetasConf);
                    ventana.ShowDialog();

                });



                //Creamos carpeta donde almacenaremos todos los archivos Iri que generemos
                DirectoryInfo dir = new DirectoryInfo(ruta + "\\Archivos");
                dir.Create();

                foreach (CarpetaConf carp in GlobalController.listaCarpetaConfActualizada)
                {
                    //Le pasamos el nombre de la subcarpeta para que procese los ficheros que contiene
                    procesarArchivosSubcarpeta(carp, dir.FullName);

                    //Antes de empezar a escribir el siguiente fichero referente a la carpeta siguiente, reiniciamos datos
                    limpiarListasYFecha();
                }

                //foreach (string subcarpeta in subcarpetas)
                //{
                //    string nombreCarpeta = subcarpeta.Substring(subcarpeta.Length - 1);

                //    //Le pasamos el nombre de la subcarpeta para que procese los ficheros que contiene
                //    procesarArchivosSubcarpeta(subcarpeta, dir.FullName);

                //    //Antes de empezar a escribir el siguiente fichero referente a la carpeta siguiente, reiniciamos datos
                //    limpiarListasYFecha();
                //}
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error función recogerSubCarpetas: " + ex.Message);
            }
        }

        public void procesarArchivosSubcarpeta(CarpetaConf carpConf, string rutaDestinoCarpetaArchivos)
        {
            //Obtenemos todos los archivos XML de esa carpeta concreta
            string[] archivosXML = Directory.GetFiles(carpConf.rutaCarpeta, "*.xml", SearchOption.TopDirectoryOnly);

            string nombreCarpeta = Path.GetFileName(carpConf.rutaCarpeta);


            //Solo generaremos archivos cuando la carpeta, contenga archivos XML
            if (archivosXML.Length > 0)
            {
                foreach (string archivoXML in archivosXML)
                {
                    //Leemos roughness, fecha y eventos
                    leerDatosXml(archivoXML);
                }

                //Una vez hemos finalizado de leer todos los archivos de esa carpeta, generamos archivo.
                generarArchivoIRI(rutaDestinoCarpetaArchivos, nombreCarpeta);
                generarArchivoEvent(rutaDestinoCarpetaArchivos, nombreCarpeta, carpConf.PKInicio, carpConf.sentidoCalzada);
                generarArchivoGpsimp(rutaDestinoCarpetaArchivos, nombreCarpeta);
            }

            //SOLO GENERAREMOS LOS ARCHIVOS PRO SI EL CHECK ESTA MARCADO
            if (this.generarFicheroPro)
            {
                string[] archivosErd = Directory.GetFiles(carpConf.rutaCarpeta, "*.erd", SearchOption.TopDirectoryOnly);

                if (archivosErd.Length > 0)
                {
                    foreach (string archivoErd in archivosErd)
                    {
                        //hola/hg_L.erd
                        bool esLeft;
                        string ultimCaracter = archivoErd.Substring(archivoErd.Length - 5, 1);
                        if (ultimCaracter == "L")
                            esLeft = true;
                        else //Es Right
                            esLeft = false;

                        leerDatosErd(archivoErd, esLeft);
                    }

                    generarArchivoPro(rutaDestinoCarpetaArchivos, nombreCarpeta);

                }
            }
        }

        //Leemos Roiughness, Fecha y Eventos
        public void leerDatosXml(string archivoXML)
        {
            try
            {
                string iri = "";
                string[] listaDatosIri;

                using (XmlReader reader = XmlReader.Create(archivoXML))
                {
                    double longitud = 0;
                    double latitud = 0;
                    var leftLeido = false;
                    string nombreArchivo = System.IO.Path.GetFileName(archivoXML);
                    Incidencia inc = new Incidencia();

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

                        // Detectar el inicio del nodo <SystemTimeAndDate>
                        if (reader.NodeType == XmlNodeType.Element && reader.Name == "SystemTimeAndDate")
                        {
                            fecha = reader.ReadElementContentAsString();

                            //Guardamos la fecha con formato 2024/08/21 14:27:37.3200, posteriormente ya lo procesaremos
                            listaFechas.Add(fecha);

                            //Convierte fecha 2024/08/21 14:27:37.3200 -> 20240821, lo usa para el nombre del archivo
                            fecha = convertirFecha(fecha);
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
                                if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "Roughness")
                                {
                                    break;
                                }
                            }

                        }
                    }
                }
            }
            catch (Exception e)
            {
                GlobalController.TextoErrores += "Error de lectura en el fichero " + archivoXML + " - " + e.Message + Environment.NewLine;

                //MessageBox.Show("Error de lectura en el fichero " + archivoXML + " " + e.Message);
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
                    escritor.WriteLine("IRI 1 => Derecha - IRI 2 => Izquierda");
                    escritor.WriteLine();
                    escritor.WriteLine("Data:");
                    escritor.WriteLine("Distance [m], IRI (1) [m/km], IRI (2) [m/km]");

                    while (listaPromediosLeft.Count > cont)
                    {
                        escritor.WriteLine(metros + ", " + listaPromediosRight[cont].ToString().Replace(",", ".") + ", " + listaPromediosLeft[cont].ToString().Replace(",", "."));
                        cont++;
                        metros += 10;
                    }

                    //Si no tiene datos, significa que el fichero txt iri estará vacío, lo informamos
                    if (listaPromediosLeft.Count == 0)
                    {
                        GlobalController.ListInfoWarnings.Add("El fichero " + nombreArchivo + " NO contiene datos.");
                    }
                    // Puedes agregar más líneas o información al archivo
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error en función generarArchivo: " + ex.Message);
            }

        }

        private void generarArchivoEvent(string rutaDestinoCarpetaArchivos, string nombreCarpeta, int PKInicio, string sentidoCalzada)
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
                    int PK = PKInicio;
                    while (listaIncidencias.Count > cont)
                    {

                        if (listaIncidencias[cont].incidencia == 9 || listaIncidencias[cont].incidencia == 1)
                        {
                            escritor.WriteLine("Event, " + listaIncidencias[cont].incidencia + ", 0, 0, yes, " + listaIncidencias[cont].distanciaCab + ", , , " + "PK" + PK + ", , 0.0, 0.0, 0.0, 0.0, 0.0, 0");

                            if (sentidoCalzada.Equals("+") || sentidoCalzada.Equals("1") || sentidoCalzada.Equals("c") || sentidoCalzada.Equals("C"))
                                PK++;
                            else
                                PK--;
                        }
                        else
                        {
                            escritor.WriteLine("Event, " + listaIncidencias[cont].incidencia + ", 0, 0, yes, " + listaIncidencias[cont].distanciaCab + ", , , " + listaIncidencias[cont].incidencia + "-" + ", , 0.0, 0.0, 0.0, 0.0, 0.0, 0");
                        }
                        cont++;
                    }

                    //Si no tiene datos, significa que el fichero txt .evt estará vacío, lo informamos
                    if (listaIncidencias.Count == 0)
                    {
                        GlobalController.ListInfoWarnings.Add("El fichero " + nombreArchivo + " NO contiene datos.");
                    }

                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al generar archivo event: " + ex.Message);
            }
        }

        private void generarArchivoGpsimp(string rutaDestinoCarpetaArchivos, string nombreCarpeta)
        {
            try
            {
                string nomCarpMod = modificarNomCarp(nombreCarpeta);
                string nombreArchivo = rutaDestinoCarpetaArchivos + "\\" + "LCMS" + fecha + nomCarpMod + ".dist.gpsimp.txt";

                using (StreamWriter escritor = new StreamWriter(nombreArchivo))
                {
                    //Headers:
                    //Re-calibrationfile: 

                    //Parameters:
                    //Distance interval: 10.0000 m
                    //Use time interval: no
                    //Time interval[s]: 1.0000

                    //Data:
                    //Distance [m], Time [s], Latitude [dd.dddd], Longitude [dd.dddd], Height [m], Height of geoid [m], DOP, Satellites, Fixtype, GPStime, Method, Vcar [rad]
                    escritor.WriteLine("Headers:");
                    escritor.WriteLine("Re-calibrationfile:");
                    escritor.WriteLine();
                    escritor.WriteLine("Parameters:");
                    escritor.WriteLine("Distance interval: 10.0000 m");
                    escritor.WriteLine("Use time interval: no");
                    escritor.WriteLine("Time interval [s]: 1.0000");
                    escritor.WriteLine();
                    escritor.WriteLine("Data:");
                    escritor.WriteLine("Distance [m], Time [s], Latitude [dd.dddd], Longitude [dd.dddd], Height [m], Height of geoid [m], DOP, Satellites, Fixtype, GPStime, Method, Vcar [rad]");

                    int cont = 0;
                    double distancia = 0; //representada en metros, va de 10 en 10

                    bool primeraFechaLeida = false;

                    string primerMinutosSegundos = "";
                    string minutosSegundos = "";
                    foreach (string fechaHora in listaFechas)
                    {
                        if (!primeraFechaLeida)
                        {
                            //La primera vuelta pillaremos la primera fecha e igualamos la misma fecha con "minutosSegundos" para que
                            //al operar, la operacion salga 0, que ya es correcto porque es la primera.
                            primerMinutosSegundos = fechaHora.Substring(11);
                            minutosSegundos = primerMinutosSegundos;
                            primeraFechaLeida = true;

                        }
                        else
                        {
                            minutosSegundos = fechaHora.Substring(11);
                        }

                        string diferencia = diferenciaRestarHoras(primerMinutosSegundos, minutosSegundos);
                        diferencia = diferencia.Replace(",", ".");

                        escritor.WriteLine(distancia + ", " + diferencia + ", " + listaLatitud[cont].ToString().Replace(",", ".") + ", " + listaLongitud[cont].ToString().Replace(",", ".") + ", , , 0.0, 0, 0, 0.0, 0,");
                        distancia += 10;
                        cont++;
                    }

                    //while (listaLongitud.Count > cont)
                    //{
                    //    escritor.WriteLine(distancia + ", 0.0, " + listaLatitud[cont].ToString().Replace(",", ".") + ", " + listaLongitud[cont].ToString().Replace(",", ".") + ", , , 0.0, 0, 0, 0.0, 0,");
                    //    distancia += 10;
                    //    cont++;
                    //}
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error en la función generarArchivoGpsimp: " + ex.Message);
            }
        }

        private void leerDatosErd(string archivoXML, bool esLeft)
        {
            bool despuesDeEND = false;
            bool segundaFila = false;

            // Leer todas las líneas del fichero
            string[] lineas = File.ReadAllLines(archivoXML);

            // Recorrer las líneas una por una
            foreach (string linea in lineas)
            {
                string lineaLimpia = linea.Trim();

                // Si encontramos "END", marcamos el inicio de los valores
                if (lineaLimpia == "END")
                {
                    despuesDeEND = true;
                    continue;
                }

                if (lineaLimpia.Contains("ERDFILEV2"))
                {
                    segundaFila = true;
                    continue;
                }

                if (segundaFila)
                {
                    //xxx 
                    //@SM 18/09/2024
                    //Tratar el valorASumar, comentar con Irene 
                    string[] datosSegundaFila = linea.Split(',');
                    int longitud = datosSegundaFila.Length;
                    valorASumar = Convert.ToDouble(datosSegundaFila[longitud - 3], CultureInfo.InvariantCulture);
                    segundaFila = false;
                }

                // Si estamos después de "END", intentamos parsear los valores
                if (despuesDeEND)
                {
                    double valorDou = Convert.ToDouble(linea, CultureInfo.InvariantCulture);
                    if (esLeft)
                        listaValoresErdLeft.Add(valorDou);
                    else
                        listaValoresErdRight.Add(valorDou);

                }
            }
        }

        private void generarArchivoPro(string rutaDestinoCarpetaArchivos, string nombreCarpeta)
        {
            try
            {
                string nomCarpMod = modificarNomCarp(nombreCarpeta);
                string nombreArchivo = rutaDestinoCarpetaArchivos + "\\" + "LCMS" + fecha + nomCarpMod + ".dist.pro.txt";
                double distancia = 0.0;
                using (StreamWriter escritor = new StreamWriter(nombreArchivo))
                {
                    //Headers:
                    //Re - calibrationfile: 

                    //Data:
                    //Distance [m], Laser 1 [mm], Laser 2 [mm]
                    escritor.WriteLine("Headers:");
                    escritor.WriteLine("Re-calibrationfile:");
                    escritor.WriteLine();
                    escritor.WriteLine("Data:");
                    escritor.WriteLine("Distance [m], Laser 1 [mm], Laser 2 [mm]");

                    int cont = 0;

                    while (listaValoresErdRight.Count > cont)
                    {
                        double valorErdRight = listaValoresErdRight[cont] * 1000;
                        double valorErdLeft = listaValoresErdLeft[cont] * 1000;
                        escritor.WriteLine(distancia.ToString().Replace(",", ".") + ", " + valorErdRight.ToString("F6").Replace(",", ".") + ", " + valorErdLeft.ToString("F6").Replace(",", "."));
                        distancia += valorASumar;
                        cont++;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error en la función generarArchivoPro: " + ex.Message);
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

        private void limpiarListasYFecha()
        {
            listaPromediosLeft.Clear();
            listaPromediosRight.Clear();
            listaIncidencias.Clear();
            listaLongitud.Clear();
            listaLatitud.Clear();
            listaFechas.Clear();

            //Para ficheros .erd .pro, modificacion el 11/12/2024
            listaValoresErdRight.Clear();
            listaValoresErdLeft.Clear();
            fecha = string.Empty;
        }

        private string diferenciaRestarHoras(string primerMinutosSegundos, string minutosSegundos)
        {
            try
            {
                TimeSpan ts1 = TimeSpan.ParseExact(primerMinutosSegundos, @"hh\:mm\:ss\.ffff", null);
                TimeSpan ts2 = TimeSpan.ParseExact(minutosSegundos, @"hh\:mm\:ss\.ffff", null);

                TimeSpan diferencia = ts2 - ts1;

                return diferencia.TotalSeconds.ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error en función diferenciaRestarHoras: " + ex.Message);
                return "";
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
