using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Xml;
using WForm = System.Windows.Forms;

namespace LCMS_ODO_GPS_GENERATOR
{

    //CONTROL DE VERSIONES

    // 12/09/2024 - VERSION 1.0.0
    // - Compilamos versión, añadimos boton para generar archivos KML y CSVs de incidencias. Creamos 2 clases nuevas IncidenciaController y KmlController
    //   añadimos nuevo progressabar. 



    /// <summary>
    /// Lógica de interacción para MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        DirectoryInfo directorio;
        DirectoryInfo directorio1;
        String NombreDirectorioSave;
        FileInfo[] archivos;
        List<DateAndTime> List_odo;
        List<String> List_odoEventos;
        List<string> List_odoFinal;
        List<DatosGPS> List_DatosGPS;
        XmlDocument xmldoc;
        public static string VERSION = "1.0.1";
        XmlElement xGPSCoorValido;   // Ultima Etiqueta GPSCoordinate del XML valida.
        string nombreArchivoValido;

        public static ProgressBar progressBarSm;

        public MainWindow()
        {
            InitializeComponent();
            this.Title = "LCMS - Version " + VERSION;
            ProgressBar.Visibility = Visibility.Hidden;
            ProgressBarKmlIncidencias.Visibility = Visibility.Hidden;
        }

        private void Btn_Procesar_Click(object sender, RoutedEventArgs e)
        {
            // Para seleccionar carpeta
            //WForm.FolderBrowserDialog folderBrowserDialog = new WForm.FolderBrowserDialog();
            //folderBrowserDialog.Description = "Selecciona la ruta de los ficheros XML.";

            //if (folderBrowserDialog.ShowDialog() == WForm.DialogResult.OK)
            //{
            //    directorio = new DirectoryInfo(folderBrowserDialog.SelectedPath)


            // Para seleccionar un archivo
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Title = "Selecciona la ruta de los ficheros XML.";

            if (openFileDialog.ShowDialog() == true)
            {
                directorio = new DirectoryInfo(System.IO.Path.GetDirectoryName(openFileDialog.FileName));

                archivos = directorio.GetFiles("*.XML");
                //Console.WriteLine(archivos.Count());
                xmldoc = new XmlDocument();
                List_odo = new List<DateAndTime>();
                List_odoEventos = new List<string>();
                List_DatosGPS = new List<DatosGPS>();

                ProgressBar.Maximum = archivos.Count();
                ProgressBar.Minimum = 0;
                ProgressBar.Value = 0;

                Btn_Procesar.IsEnabled = false;
                ProgressBar.Visibility = Visibility.Visible;

                tbMensajesSistema.Text = "Procesando nuevos archivos de la carpeta " + openFileDialog.FileName + Environment.NewLine + Environment.NewLine;
                nombreArchivoValido = "";
                xGPSCoorValido = null;

                Task task = Task.Run(() =>
                {
                    procesarArchivos(archivos);
                    
                    NombreDirectorioSave = Seleccionardirectorio();
                   
                    CrearArchivoGPS();
                    CrearArchivoODO();

                    MessageBox.Show("Operación realizada correctamente.");

                    this.Dispatcher.Invoke(() =>
                    {
                        Btn_Procesar.IsEnabled = true;
                        ProgressBar.Visibility = Visibility.Hidden;

                    });
                });   
            }
        }

        private void Btn_GenerarKML_Incidencias_Click(object sender, RoutedEventArgs e)
        {
            KmlController kmlController = new KmlController();
            IncidenciaController incidenciaController = new IncidenciaController();

            System.Windows.Forms.FolderBrowserDialog selCarpeta = new System.Windows.Forms.FolderBrowserDialog();

            selCarpeta.ShowDialog();

            if (!string.IsNullOrEmpty(selCarpeta.SelectedPath))
            {

                Btn_GenerarKML_Incidencias.IsEnabled = false;
                ProgressBarKmlIncidencias.Visibility = Visibility.Visible;

                Task task = Task.Run(() =>
                {
                    DirectoryInfo di = new DirectoryInfo(selCarpeta.SelectedPath);
                    kmlController.leerArchivosXML(di.FullName);
                    incidenciaController.leerArchivosXML(di.FullName);

                    this.Dispatcher.Invoke(() =>
                    {
                        Btn_GenerarKML_Incidencias.IsEnabled = true;
                        ProgressBarKmlIncidencias.Visibility = Visibility.Hidden;

                    });
                    MessageBox.Show("Operación realizada correctamente.");
                });

            }
        }

        //private void Btn_GenerarIncidencias_Click(object sender, RoutedEventArgs e)
        //{
        //    //@SM , completamos diccionario
        //    IncidenciaController incidenciaController = new IncidenciaController();

        //    System.Windows.Forms.FolderBrowserDialog selCarpeta = new System.Windows.Forms.FolderBrowserDialog();

        //    selCarpeta.ShowDialog();

        //    if (!string.IsNullOrEmpty(selCarpeta.SelectedPath))
        //    {
        //        DirectoryInfo di = new DirectoryInfo(selCarpeta.SelectedPath);
        //        incidenciaController.leerArchivosXML(di.FullName);
        //    }
        //}
        



        private void procesarArchivos(FileInfo[] archivos)
        {
            List<string> lMensajes;

            for (int i = 0; i < archivos.Count(); i++)
            {
                string nombreArxivo = archivos[i].Name;
                //Console.WriteLine(nombreArxivo);
                xmldoc.Load(archivos[i].FullName);

                bool bFallo = GenerarGPS(nombreArxivo, out lMensajes, i == 0);
                lMensajes = null;
                if (lMensajes != null && lMensajes.Count > 0)
                {
                    foreach (string mens in lMensajes)
                    {
                        tbMensajesSistema.Dispatcher.Invoke(() => {
                            tbMensajesSistema.Text += mens + Environment.NewLine;
                        });
                    }
                }

                almacenaTìemposODO();

                ProgressBar.Dispatcher.Invoke(() => {
                    ProgressBar.Value = i + 1;
                });
            }

            procesaTiemposOdo();

            if (List_DatosGPS != null && List_DatosGPS.Count > 0)
            {
                introducirRegistrosGPScada200ms();
                interpolarGPS();
            }
            else
            {
                
                //MessageBoxResult resultado = MessageBox.Show(this,"No hay ningún dato GPS, desea crear virtualmente datos virtuales para estos valores", "Faltan datos GPS", MessageBoxButton.YesNo, MessageBoxImage.Question);
                
                if (true)
                {
                    tbMensajesSistema.Dispatcher.Invoke(() => {
                        tbMensajesSistema.Text += "No existen coordenadas GPS válidas en los archivos XMLs. Se han creado coordenadas virtuales inválidas." + Environment.NewLine;
                    });

                    DateAndTime dtIni = new DateAndTime(List_odo.First().hora);
                    DateAndTime dtFin = new DateAndTime(List_odo.Last().hora);

                    // Redondeos a modulo 200 milisegunds para INI
                    int redondeoIni = dtIni.hora.Milliseconds % 200;
                    int redondeoFin = dtFin.hora.Milliseconds % 200;

                    int horasRetraso = 0;
                    if (TimeZoneInfo.Local.IsDaylightSavingTime(List_odo[0].dia))
                    {
                        horasRetraso = 2;       // En horario de verano se retrasan 2 horas
                    }
                    else
                    {
                        horasRetraso = 1;       // En horario de verano se retrasan 1 hora
                    }

                    // Redondeos a modulo 200 milisegunds para INI

                    dtIni.hora = dtIni.hora - new TimeSpan (0, horasRetraso, 0,0, redondeoIni);
                    dtFin.hora = dtFin.hora - new TimeSpan(0, horasRetraso, 0, 0, redondeoFin);

                    List_DatosGPS.Add(new DatosGPS("0010.0000000", "00010.0000000", "0.00000000", dtIni, "0", "1", "W", "N", false));
                    List_DatosGPS.Add(new DatosGPS("0010.0000000", "00010.0000000", "0.00000000", dtFin, "0", "1", "W", "N", false));

                    introducirRegistrosGPScada200ms();
                    interpolarGPS();
                }
            }
        }

        private bool GenerarGPS (string _nombreArchivo, out List<string> _lMensajes, bool _bPrimero)
        {
            bool bError = false;

            _lMensajes = new List<string>();

            XmlNodeList xGeneral = xmldoc.GetElementsByTagName("LcmsAnalyserResults");
            if (xGeneral == null || xGeneral.Count == 0)
            {
                _lMensajes.Add("ERROR en " + _nombreArchivo + ": No existe la etiqueta LcmsAnalyserResults. No pueden procesarse los datos GPS del archivo");

                bError = true;
                return bError;
            }

            if (_bPrimero)
            {
                // Para el primer archivo solo quiere guardarse el ultimo punto GPS que es el que nos proporcionará la sincronización entre tiempo GPS y ODOMETRO. Estas dos primeras lineas del .txt y .track de salida son importantísimas
                XmlElement xG3Primero = dameCoordenada((XmlElement)xGeneral[0], _nombreArchivo, out _lMensajes);
                if (xG3Primero == null)
                {
                    bError = true;
                    return bError;
                }

                insertarNodoGPS(xG3Primero);
            }
            else
            {
                // Ahora se vuelcan todos los datos válidos
                List<XmlElement> xG3s = dameCoordenadas((XmlElement)xGeneral[0], _nombreArchivo, out _lMensajes);
                if (xG3s == null)
                {
                    bError = true;
                    return bError;
                }

                foreach (XmlElement xG3 in xG3s)
                {
                    insertarNodoGPS(xG3);
                }
            }
            

            return bError;
        }

        /// <summary>
        /// Inserta una estructura de tipo <GPSCoordinate> del XML en la lista List_DatosGPS en memoria para poder tratarla
        /// </summary>
        /// <param name="_xG3"></param>
        private void insertarNodoGPS(XmlElement _xG3)
        {
            DatosGPS datosGPS = new DatosGPS();

            string aux;

            // Ojo... antes se procesaba 1 de cada cuatro coordenadas GPSCoordinate No entiendo muy bien por que
            foreach (XmlNode nodoElements in _xG3.ChildNodes)
            {
                //Console.WriteLine(nodoElements.Name);
                switch (nodoElements.Name)
                {
                    case "Latitude":
                        aux = nodoElements.InnerText;
                        double valorPOS = Convert.ToDouble(aux.Replace('.', ','));
                        if (valorPOS > 0)
                        {
                            datosGPS.NoS = "N";
                        }
                        else
                        {
                            datosGPS.NoS = "S";
                            aux = aux.Substring(1);
                        }

                        // IM lo comentado es lo antiguo. A cambio he llamada a una función para usarlo tanto en longitud como en latitud
                        double[] coorGradosMinutos = getCoorGradosMin(aux);

                        // Se vuelca la latitud de coordenada decimal con formato $GPGGA == GGGMM.MM           G = GRADOS
                        //                                                                                      M = MINUTOS

                        datosGPS.Latitude = coorGradosMinutos[0].ToString("F0").PadLeft(2, '0') // parte entera
                                                + coorGradosMinutos[1].ToString("F7", CultureInfo.InvariantCulture).PadLeft(10, '0'); // parte decimal

                        break;
                    case "Longitude":
                        aux = nodoElements.InnerText;
                        double valorPOS1 = Convert.ToDouble(aux.Replace('.', ','));

                        if (valorPOS1 > 0)
                        {
                            datosGPS.EoW = "E";
                        }
                        else
                        {
                            datosGPS.EoW = "W";
                            aux = aux.Substring(1);
                        }

                        // IM lo comentado es lo antiguo. A cambio he llamada a una función para usarlo tanto en longitud como en latitud
                        coorGradosMinutos = getCoorGradosMin(aux);

                        // Se vuelca la longitud de coordenada decimal con formato $GPGGA == GGGMM.MM           G = GRADOS
                        //                                                                                      M = MINUTOS



                        datosGPS.Longitude = coorGradosMinutos[0].ToString("F0").PadLeft(3).Replace(" ", "0") // parte entera Grados 
                                                + coorGradosMinutos[1].ToString("F7", CultureInfo.InvariantCulture).PadLeft(10, '0'); // parte decimal

                        break;
                    case "Altitude":
                        // IM aqui no se transforma la altitud. Habría que hacer la transformacion que se quisiera como en Longitude. Lo antiguo lo dejo comentado
                        aux = nodoElements.InnerText;
                        aux = aux.Substring(0, 6);
                        datosGPS.Altitude = aux;

                        break;
                    case "Time":
                        aux = nodoElements.InnerText;

                        ////string HHMMSS = aux.Substring(0, aux.LastIndexOf(':'));
                        ////DateTime dateAndTime = Convert.ToDateTime(HHMMSS);
                        ////dateAndTime = dateAndTime.AddMinutes(0);
                        ////aux = dateAndTime.TimeOfDay + aux.Substring(aux.LastIndexOf(':'));

                        ////aux = aux.Replace(":", "");

                        ////aux = aux.Substring(0, 8);
                        ////aux = aux.Insert(6, ".");

                        datosGPS.Time = new DateAndTime(nodoElements.InnerText);
                        break;
                    case "NbrOfSatellites":
                        datosGPS.NumSatelites = nodoElements.InnerText;
                        break;
                    case "SignalQuality":
                        datosGPS.SignalQuality = nodoElements.InnerText;
                        break;
                }
            }

            if (List_DatosGPS.Count > 0 && datosGPS.Time.hora == List_DatosGPS.Last().Time.hora)
            {
                datosGPS.duplicado = true;
            }
            else
            {
                datosGPS.duplicado = false;
            }

            List_DatosGPS.Add(datosGPS);
        }

        /// <summary>
        /// 
        /// </summary>
        private void introducirRegistrosGPScada200ms()
        {
            List<DatosGPS> lGPS_Aux = new List<DatosGPS>();

            TimeSpan intervalo = TimeSpan.FromMilliseconds(200);
            
            // recorro todos los datos GPS que tengo. Introduzco 1 dato repetido para luego interpolar por cada 200ms que exista entre cada posicion de List_DatosGPS
            for (int i = 0; i < List_DatosGPS.Count - 1; i++)
            {
                if ((bool)List_DatosGPS[i].duplicado)  // Si viene duplicado es una hora antigua. Cojo la ultima válida
                {
                    List_DatosGPS[i].Time.hora = List_DatosGPS[i - 1].Time.hora + intervalo;
                    lGPS_Aux.Add(new DatosGPS(List_DatosGPS[i]));
                }
                else
                {
                    lGPS_Aux.Add(new DatosGPS(List_DatosGPS[i]));
                }

                List_DatosGPS[i].Time.hora += intervalo;

                while ((i < List_DatosGPS.Count - 1) && (List_DatosGPS[i].Time.hora < List_DatosGPS[i+1].Time.hora))
                {
                    lGPS_Aux.Add(new DatosGPS(List_DatosGPS[i]));
                    lGPS_Aux.Last().duplicado = true;

                    List_DatosGPS[i].Time.hora += intervalo;
                }
            }

            lGPS_Aux.Add(List_DatosGPS.Last());

            List_DatosGPS = lGPS_Aux;
        }

        /// <summary>
        /// Interpola datos de tiempo y coordenadas geométricas contenidas en List_DatosGPS en los lugares en los que se repiten coordenadas GPGGA
        /// </summary>
        private void interpolarGPS()
        {
            // La primera la doy por valida
            DateAndTime ultimoTiempoValido = List_DatosGPS[0].Time;
            int indUltimaCoorValida = 0;
            bool bProcesandoInterpolacion = false;

            for (int i = 1; i < List_DatosGPS.Count; i++)
            {
                if (List_DatosGPS[i].Time != ultimoTiempoValido && (List_DatosGPS[i].duplicado == null || List_DatosGPS[i].duplicado == false))  // Diferente, es correcto
                {
                    if (bProcesandoInterpolacion)
                    {
                        // si estabamos en proceso de interpolación, lo resolvemos con la ultima coordenada valida
                        interpolarSecuencia(indUltimaCoorValida, i);

                        bProcesandoInterpolacion = false;
                    }

                    // Guardamos los datos como ultimos validos
                    indUltimaCoorValida = i;
                    ultimoTiempoValido = List_DatosGPS[i].Time;
                }
                else
                {
                    bProcesandoInterpolacion = true;
                }
            }    
        }

        /// <summary>
        /// Proceso de interpolación en una seccion de List_DatosGPS. Recalculo con valores interpolados las posiciones Interpolo los valor
        /// </summary>
        /// <param name="indUltimaCoorValida"></param>
        /// <param name="i"></param>
        private void interpolarSecuencia(int _indInicio, int _indFinal)
        {
            int numeroPasos = _indFinal - _indInicio;
            double long1 = Convert.ToDouble(List_DatosGPS[_indInicio].Longitude.Replace('.', ','));
            double long2 = Convert.ToDouble(List_DatosGPS[_indFinal].Longitude.Replace('.', ','));

            double lat1 = Convert.ToDouble(List_DatosGPS[_indInicio].Latitude.Replace('.', ','));
            double lat2 = Convert.ToDouble(List_DatosGPS[_indFinal].Latitude.Replace('.', ','));

            //string[] fecha1 = List_DatosGPS[_indInicio].Time.Split('.');
            //string[] fecha2 = List_DatosGPS[_indFinal].Time.Split('.');

            //TimeSpan tSpan1 = new TimeSpan(0, int.Parse(fecha1[0].Substring (0,2)) , int.Parse(fecha1[0].Substring(2, 2)), int.Parse(fecha1[0].Substring(4, 2)), int.Parse(fecha1[1]) * 10);
            //TimeSpan tSpan2 = new TimeSpan(0, int.Parse(fecha2[0].Substring(0, 2)), int.Parse(fecha2[0].Substring(2, 2)), int.Parse(fecha2[0].Substring(4, 2)), int.Parse(fecha2[1]) * 10);

            TimeSpan difTotal = List_DatosGPS[_indFinal].Time.hora - List_DatosGPS[_indInicio].Time.hora;
            
            // estos son los incrementales a utilizar por paso
            TimeSpan difPasoTiempo = new TimeSpan (difTotal.Ticks / numeroPasos);
            double difPasoLong = (long2 - long1) / numeroPasos;
            double difPasoLat = (lat2 - lat1) / numeroPasos;

            int contadorAvance = 1;

            for (int i = _indInicio + 1; i < _indFinal; i++)
            {
                // Calculo de las interpolaciones
                double longDato = long1 + difPasoLong * contadorAvance;
                double latDato = lat1 + difPasoLat * contadorAvance;
                
                // Añadimos los datos
                List_DatosGPS[i].Longitude = longDato.ToString("00000.0000000", CultureInfo.InvariantCulture);
                List_DatosGPS[i].Latitude = latDato.ToString("0000.0000000", CultureInfo.InvariantCulture);
                List_DatosGPS[i].Time = new DateAndTime( new TimeSpan(List_DatosGPS[_indInicio].Time.hora.Ticks + (difPasoTiempo.Ticks * contadorAvance)));

                //TimeSpan tSpanDato = new TimeSpan (List_DatosGPS[_indInicio].Time.hora.Ticks + (difPasoTiempo.Ticks * contadorAvance));
                //List_DatosGPS[i].Time = tSpanDato.ToString("hhmmss") + "." + (tSpanDato.Milliseconds / 10).ToString("00");

                contadorAvance++;
            }
        }

        /// <summary>
        /// Proceso de interpolación en una seccion de List_DatosGPS. Recalculo con valores interpolados las posiciones Interpolo los valor
        /// </summary>
        /// <param name="indUltimaCoorValida"></param>
        /// <param name="i"></param>
        //private void interpolarSecuenciaAntigua (int _indInicio, int _indFinal)
        //{
        //    int numeroPasos = _indFinal - _indInicio;
        //    double long1 = Convert.ToDouble(List_DatosGPS[_indInicio].Longitude.Replace('.', ','));
        //    double long2 = Convert.ToDouble(List_DatosGPS[_indFinal].Longitude.Replace('.', ','));

        //    double lat1 = Convert.ToDouble(List_DatosGPS[_indInicio].Latitude.Replace('.', ','));
        //    double lat2 = Convert.ToDouble(List_DatosGPS[_indFinal].Latitude.Replace('.', ','));

        //    string[] fecha1 = List_DatosGPS[_indInicio].Time.Split('.');
        //    string[] fecha2 = List_DatosGPS[_indFinal].Time.Split('.');

        //    TimeSpan tSpan1 = new TimeSpan(0, int.Parse(fecha1[0].Substring(0, 2)), int.Parse(fecha1[0].Substring(2, 2)), int.Parse(fecha1[0].Substring(4, 2)), int.Parse(fecha1[1]) * 10);
        //    TimeSpan tSpan2 = new TimeSpan(0, int.Parse(fecha2[0].Substring(0, 2)), int.Parse(fecha2[0].Substring(2, 2)), int.Parse(fecha2[0].Substring(4, 2)), int.Parse(fecha2[1]) * 10);

        //    TimeSpan difTotal = tSpan2 - tSpan1;

        //    // estos son los incrementales a utilizar por paso
        //    TimeSpan difPasoTiempo = new TimeSpan(difTotal.Ticks / numeroPasos);
        //    double difPasoLong = (long2 - long1) / numeroPasos;
        //    double difPasoLat = (lat2 - lat1) / numeroPasos;

        //    int contadorAvance = 1;

        //    for (int i = _indInicio + 1; i < _indFinal; i++)
        //    {
        //        // Calculo de las interpolaciones
        //        double longDato = long1 + difPasoLong * contadorAvance;
        //        double latDato = lat1 + difPasoLat * contadorAvance;
        //        TimeSpan tSpanDato = new TimeSpan(tSpan1.Ticks + (difPasoTiempo.Ticks * contadorAvance));

        //        // Añadimos los datos
        //        List_DatosGPS[i].Longitude = longDato.ToString("00000.0000000", CultureInfo.InvariantCulture);
        //        List_DatosGPS[i].Latitude = latDato.ToString("0000.0000000", CultureInfo.InvariantCulture);
        //        List_DatosGPS[i].Time = tSpanDato.ToString("hhmmss") + "." + (tSpanDato.Milliseconds / 10).ToString("00");

        //        contadorAvance++;
        //    }
        //}

        /// <summary>
        /// Devuelve una coordenada del archivo xml. La ultima <GPSCoordinate> 
        /// </summary>
        /// <param name="xmlGPSInformation">etiqueta GPSInformation</param>
        /// <returns>una etiqueta GPSCoordinate</returns>
        private XmlElement dameCoordenada (XmlElement _xmlLcmsAnalyserResults, string _nombreArchivo, out List<string> _lMensajes)
        {
            _lMensajes = new List<string>();

            XmlNodeList xGPSInformation = _xmlLcmsAnalyserResults.GetElementsByTagName("GPSInformation");
            XmlElement xGPSCoor = null;  // El valor de salida
            if (xGPSInformation == null || xGPSInformation.Count == 0)  // no existe la etiqueta, cojo la del anterior archivo valido
            {
                if (xGPSCoorValido == null) // si no habia etiqueta valida anterior es un fallo
                {
                    _lMensajes.Add("ERROR en " + _nombreArchivo + " : No existe la etiqueta GPSInformation ni una etiqueta del mismo tipo en archivos anteriores. No pueden procesarse los datos GPS del archivo");

                    return null;
                }
                else
                {
                    _lMensajes.Add("WARNING en " + _nombreArchivo + " : No existe la etiqueta GPSInformation. Se procesa con el dato del archivo " + nombreArchivoValido);

                    xGPSCoor = xGPSCoorValido;
                }
            }
            else
            {
                XmlNodeList xNuevoGPSCoor = ((XmlElement)xGPSInformation[0]).GetElementsByTagName("GPSCoordinate");
                if (xNuevoGPSCoor.Count > 0) // si no coordenadas
                {
                    // se busca la ultima coordenada con la etiqueta SignalQuality = 1

                    bool bSalir = false;

                    
                    for (int i = xNuevoGPSCoor.Count - 1; i >= 0; i--)
                    {
                        if (bSalir)
                        {
                            break;
                        }

                        XmlElement coord = (XmlElement)xNuevoGPSCoor.Item(i);

                        foreach (XmlNode nodoElements in coord.ChildNodes)
                        {
                            if (nodoElements.Name == "SignalQuality")
                            {
                                if (nodoElements.InnerText != "0")
                                {
                                    nombreArchivoValido = _nombreArchivo;
                                    xGPSCoorValido = coord;

                                    xGPSCoor = coord;
                                    bSalir = true;
                                    break;
                                }
                            }
                        }
                    }
                }

                if (xGPSCoor == null) // Todavia no he encontrado una coordenada valida
                {
                    if (xGPSCoorValido == null) // si no habia etiqueta valida anterior es un fallo
                    {
                        _lMensajes.Add("ERROR en " + _nombreArchivo + " : No existe la etiqueta GPSCoordinate ni una etiqueta del mismo tipo en archivos anteriores. No pueden procesarse los datos GPS del archivo");

                        return null;
                    }
                    else
                    {
                        _lMensajes.Add("WARNING en " + _nombreArchivo + " : No existe la etiqueta GPSCoordinate. Se procesa con el dato del archivo " + nombreArchivoValido);

                        xGPSCoor = xGPSCoorValido;
                    }
                }
            }
            
            return xGPSCoor;
        }

        /// <summary>
        /// Devuelve todas las coordenadas válidas del archivo xml.  <GPSCoordinate> 
        /// </summary>
        /// <param name="xmlGPSInformation">etiqueta GPSInformation</param>
        /// <returns>Lista de coordenadas GPSCoordinate validas</returns>
        private List<XmlElement> dameCoordenadas (XmlElement _xmlLcmsAnalyserResults, string _nombreArchivo, out List<string> _lMensajes)
        {
            _lMensajes = new List<string>();

            XmlNodeList xGPSInformation = _xmlLcmsAnalyserResults.GetElementsByTagName("GPSInformation");
            List<XmlElement> xGPSCoors = new List<XmlElement>();  // El valor de salida
            
            if (xGPSInformation == null || xGPSInformation.Count == 0)  // no existe la etiqueta, cojo la del anterior archivo valido
            {
                if (xGPSCoorValido == null) // si no habia etiqueta valida anterior es un fallo
                {
                    _lMensajes.Add("ERROR en " + _nombreArchivo + " : No existe la etiqueta GPSInformation ni una etiqueta del mismo tipo en archivos anteriores. No pueden procesarse los datos GPS del archivo");

                    return null;
                }
                else
                {
                    _lMensajes.Add("WARNING en " + _nombreArchivo + " : No existe la etiqueta GPSInformation. Se procesa con el dato del archivo " + nombreArchivoValido);

                    xGPSCoors.Add(xGPSCoorValido);
                }
            }
            else
            {
                XmlNodeList xNuevoGPSCoor = ((XmlElement)xGPSInformation[0]).GetElementsByTagName("GPSCoordinate");
                if (xNuevoGPSCoor.Count > 0) // si hay coordenadas
                {
                    // se añaden las coordenadas validas (etiqueta SignalQuality = 1)

                    foreach (XmlElement coord in xNuevoGPSCoor)
                    {
                        foreach (XmlNode nodoElements in coord.ChildNodes)
                        {
                            if (nodoElements.Name == "SignalQuality")
                            {
                                if (nodoElements.InnerText != "0")
                                {
                                    nombreArchivoValido = _nombreArchivo;
                                    xGPSCoorValido = coord;

                                    xGPSCoors.Add (coord);
                                }
                            }
                        }
                    }
                }

                if (xGPSCoors.Count == 0) // Todavia no he encontrado una coordenada valida
                {
                    if (xGPSCoorValido == null) // si no habia etiqueta valida anterior es un fallo
                    {
                        _lMensajes.Add("ERROR en " + _nombreArchivo + " : No existe la etiqueta GPSCoordinate ni una etiqueta del mismo tipo en archivos anteriores. No pueden procesarse los datos GPS del archivo");

                        return null;
                    }
                    else
                    {
                        _lMensajes.Add("WARNING en " + _nombreArchivo + " : No existe la etiqueta GPSCoordinate. Se procesa con el dato del archivo " + nombreArchivoValido);

                        xGPSCoors.Add (xGPSCoorValido);
                    }
                }
            }

            return xGPSCoors;
        }

        /// <summary>
        /// IM. Funcion que recibe una coordenada geometrica decimal y devuelve en cadenas de texto separados los grados enteros y el resto en minutos. Ejemplo Entrada 40.714 devuelve un array de 2 doubles ["40", "42.84"]  Apunte 42.84 = 0.714 * 60
        /// </summary>
        /// 
        /// <param name="coordDecimal">coordenada geometrica decimal</param>
        /// <returns>Array con dos doubles. El primero con los grados enteros de la entrada, el segundo con el resto expresado en minutos</returns>
        private double [] getCoorGradosMin(double coordDecimal)
        {
            double[] coorGradosMin = new double [2];

            double parteEntera = Math.Truncate(coordDecimal);
            
            coorGradosMin[0] = parteEntera;
            coorGradosMin[1] = (coordDecimal - parteEntera) * 60.0;
                        
            return coorGradosMin;
        }

        /// <summary>
        /// misma funcion que la homonima que recibe un double, pero con la entrada como cadena de texto
        /// </summary>
        /// <param name="coordDecimal">coordenada geometrica decimal</param>
        /// <returns>Array con dos doubles. El primero con los grados enteros de la entrada, el segundo con el resto expresado en minutos</returns>
        private double[] getCoorGradosMin(string strCoordDecimal)
        {
            return getCoorGradosMin(Convert.ToDouble(strCoordDecimal,CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Almacena en la variable List_odo y listEvents, los datos de los archivos Tiempo y eventos
        /// </summary>
        private void almacenaTìemposODO()
        {
            XmlNodeList xGeneral = xmldoc.GetElementsByTagName("LcmsAnalyserResults");
            XmlNodeList xG2 = ((XmlElement)xGeneral[0]).GetElementsByTagName("SystemData");
            XmlNodeList xG3 = ((XmlElement)xG2[0]).GetElementsByTagName("SystemStatus");
            XmlNodeList xDateAndTime = ((XmlElement)xG3[0]).GetElementsByTagName("SystemTimeAndDate");
            //XmlNode xEventos = xmldoc.LastChild;

            XmlNodeList xEventos = ((XmlElement)xGeneral[0]).GetElementsByTagName("UserEventInformation");

            //if (xEventos.LastChild.Name == "UserEventInformation")
            //{
            //    string numevento = xEventos.LastChild.InnerText.Substring(xEventos.LastChild.InnerText.Length - 1, 1);
            //    List_odoEventos.Add(numevento);
            //}
            if (xEventos != null && xEventos.Count > 0)
            {
                string numevento = xEventos[0].InnerText.Substring(xEventos[0].InnerText.Length - 1, 1);
                List_odoEventos.Add(numevento);
            }
            else
            {
                List_odoEventos.Add("");
            }

            foreach (XmlElement nodo in xDateAndTime)
            {
                List_odo.Add(new DateAndTime (nodo.InnerText));
            }
        }

        /// <summary>
        /// Se procesan todos los tiempos introduciendo 9 registros interpolados en el tiempo
        /// </summary>
        private void procesaTiemposOdo ()
        {
            List_odoFinal = new List<string>();

            String horaFinal;
            for (int i = 0; i < List_odo.Count; i++)
            {
                horaFinal = List_odo.ElementAt(i).getFormatoODO_trac();

                if (List_odoEventos.ElementAt(i) != "")
                {
                    string aux = "E>" + List_odoEventos.ElementAt(i) + "*" + horaFinal;
                    List_odoFinal.Add(aux);  // Se introduce un elemento especial si hay evento
                }
                //else  // En caso de haber evento no se duplica 
                //{
                //    List_odoFinal.Add(horaFinal);
                //}

                // En caso de haber evento se duplica 
                List_odoFinal.Add(horaFinal);

                if (i != List_odo.Count - 1)
                {
                    DateAndTime aux = new DateAndTime(List_odo.ElementAt(i).hora);
                    TimeSpan intervalo = TimeSpan.FromTicks((List_odo.ElementAt(i + 1).hora - List_odo.ElementAt(i).hora).Ticks / 10);

                    for (int k = 1; k <= 9; k++)
                    {
                        aux.hora += intervalo;
                        List_odoFinal.Add(aux.getFormatoODO_trac());
                    }

                }
            }

            // Duplico el primer registro porque se usa para la sincronización de archivos. En el GID este primer registro del Odometro no se exporta y es solo para la sincronizacion
            List_odoFinal.Insert(0, List_odoFinal[0]);
        }
        
        /// <summary>
        /// Crea el archivo GPS con las coordenadas
        /// Ultima modificación. Añado un registro por cada 200ms. Si dispongo de el en la lista me lo invento interpolando entre los que tengo.
        /// </summary>
        private void CrearArchivoGPS()
        {

            String NombreArchivo = NombreDirectorioSave + "_GPS.txt";
            //Console.WriteLine(NombreArchivo);

            using (StreamWriter file = new StreamWriter(NombreArchivo, false, Encoding.GetEncoding("iso-8859-1")))
            {
                DateAndTime dtAux = new DateAndTime();


                if (List_DatosGPS != null && List_DatosGPS.Count > 0)
                {
                    //dtAux = new DateTime ( List_DatosGPS[0].Time);
                }

                for (int i = 0; i < List_DatosGPS.Count; i++)
                {
                    addPuntoGPSalArchivo(file, List_DatosGPS.ElementAt(i));
                }
            }
        }

        /// <summary>
        /// Añade un punto GPS al archivo
        /// </summary>
        /// <param name="file"></param>
        /// <param name="datosGPS"></param>
        private void addPuntoGPSalArchivo(StreamWriter _file, DatosGPS datosGPS)
        {
            _file.Write("$GPGGA");
            _file.Write(",");
            _file.Write(datosGPS.Time.getFormatoGPS_GPGGA());
            _file.Write(",");
            _file.Write(datosGPS.Latitude);
            _file.Write(",");
            _file.Write(datosGPS.NoS);
            _file.Write(",");
            _file.Write(datosGPS.Longitude);
            _file.Write(",");
            _file.Write(datosGPS.EoW);
            _file.Write(",");
            _file.Write(datosGPS.SignalQuality);
            _file.Write(",");
            _file.Write(datosGPS.NumSatelites);
            _file.Write(",");
            _file.Write("0");
            _file.Write(",");
            _file.Write(datosGPS.Altitude + ",M");
            _file.Write(",");
            _file.Write("0,M");
            _file.Write(",");
            _file.Write(",");
            _file.Write("*75");
            _file.WriteLine();
        }

        private void CrearArchivoODO()
        {
                       
            String NombreArchivo = NombreDirectorioSave +"_ODO.trac";
            //Console.WriteLine(NombreArchivo);

            using (StreamWriter file = new StreamWriter(NombreArchivo, false, Encoding.GetEncoding("iso-8859-1")))
            {
                for (int i = 0; i < List_odoFinal.Count; i++)
                {
                    file.WriteLine(List_odoFinal.ElementAt(i));
                }
            }
        }

        private String Seleccionardirectorio() {

            string auxFecha = List_odoFinal.ElementAt(0);
            if (auxFecha.Contains("*"))
            {
                auxFecha = auxFecha.Split('*')[1];
            }
            auxFecha = auxFecha.Split(':')[0] + "_" + auxFecha.Split(':')[1] + "_" + auxFecha.Split(':')[2];

            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.FileName = auxFecha;
            saveFileDialog.ShowDialog();
            directorio1 = new DirectoryInfo(saveFileDialog.FileName);

            NombreDirectorioSave = directorio1.Parent.FullName + "\\" + directorio.Name + "_odo_gps";
            //Console.WriteLine(NombreDirectorioSave);

            if (!Directory.Exists(NombreDirectorioSave))
            {
                Directory.CreateDirectory(NombreDirectorioSave);
            }

            NombreDirectorioSave = directorio1.Parent.FullName + "\\" + directorio.Name + "_odo_gps\\" + auxFecha;

            return NombreDirectorioSave;
        }


    }
}