//using DocumentFormat.OpenXml.Bibliography;
//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using System.Xml;

//namespace LCMS_ODO_GPS_GENERATOR
//{
//    internal class ProcesarController
//    {

//        FileInfo[] archivos;
//        List<DateAndTime> List_odo;
//        List<String> List_odoEventos;
//        List<string> List_odoFinal;
//        List<DatosGPS> List_DatosGPS;
//        XmlDocument xmldoc;
//        XmlElement xGPSCoorValido;   // Ultima Etiqueta GPSCoordinate del XML valida.



//        private void procesarArchivos(FileInfo[] archivos)
//        {
//            List<string> lMensajes;

//            for (int i = 0; i < archivos.Count(); i++)
//            {
//                string nombreArxivo = archivos[i].Name;
//                //Console.WriteLine(nombreArxivo);
//                xmldoc.Load(archivos[i].FullName);

//                bool bFallo = GenerarGPS(nombreArxivo, out lMensajes, i == 0);
//                lMensajes = null;
//                if (lMensajes != null && lMensajes.Count > 0)
//                {
//                    foreach (string mens in lMensajes)
//                    {
//                        tbMensajesSistema.Dispatcher.Invoke(() =>
//                        {
//                            tbMensajesSistema.Text += mens + Environment.NewLine;
//                        });
//                    }
//                }

//                almacenaTìemposODO();

//                ProgressBar.Dispatcher.Invoke(() =>
//                {
//                    ProgressBar.Value = i + 1;
//                });
//            }

//            procesaTiemposOdo();

//            if (List_DatosGPS != null && List_DatosGPS.Count > 0)
//            {
//                introducirRegistrosGPScada200ms();
//                interpolarGPS();
//            }
//            else
//            {

//                //MessageBoxResult resultado = MessageBox.Show(this,"No hay ningún dato GPS, desea crear virtualmente datos virtuales para estos valores", "Faltan datos GPS", MessageBoxButton.YesNo, MessageBoxImage.Question);

//                if (true)
//                {
//                    tbMensajesSistema.Dispatcher.Invoke(() =>
//                    {
//                        tbMensajesSistema.Text += "No existen coordenadas GPS válidas en los archivos XMLs. Se han creado coordenadas virtuales inválidas." + Environment.NewLine;
//                    });

//                    DateAndTime dtIni = new DateAndTime(List_odo.First().hora);
//                    DateAndTime dtFin = new DateAndTime(List_odo.Last().hora);

//                    // Redondeos a modulo 200 milisegunds para INI
//                    int redondeoIni = dtIni.hora.Milliseconds % 200;
//                    int redondeoFin = dtFin.hora.Milliseconds % 200;

//                    int horasRetraso = 0;
//                    if (TimeZoneInfo.Local.IsDaylightSavingTime(List_odo[0].dia))
//                    {
//                        horasRetraso = 2;       // En horario de verano se retrasan 2 horas
//                    }
//                    else
//                    {
//                        horasRetraso = 1;       // En horario de verano se retrasan 1 hora
//                    }

//                    // Redondeos a modulo 200 milisegunds para INI

//                    dtIni.hora = dtIni.hora - new TimeSpan(0, horasRetraso, 0, 0, redondeoIni);
//                    dtFin.hora = dtFin.hora - new TimeSpan(0, horasRetraso, 0, 0, redondeoFin);

//                    List_DatosGPS.Add(new DatosGPS("0010.0000000", "00010.0000000", "0.00000000", dtIni, "0", "1", "W", "N", false));
//                    List_DatosGPS.Add(new DatosGPS("0010.0000000", "00010.0000000", "0.00000000", dtFin, "0", "1", "W", "N", false));

//                    introducirRegistrosGPScada200ms();
//                    interpolarGPS();
//                }
//            }
//        }

//        /// <summary>
//        /// Almacena en la variable List_odo y listEvents, los datos de los archivos Tiempo y eventos
//        /// </summary>
//        private void almacenaTìemposODO()
//        {
//            XmlNodeList xGeneral = xmldoc.GetElementsByTagName("LcmsAnalyserResults");
//            XmlNodeList xG2 = ((XmlElement)xGeneral[0]).GetElementsByTagName("SystemData");
//            XmlNodeList xG3 = ((XmlElement)xG2[0]).GetElementsByTagName("SystemStatus");
//            XmlNodeList xDateAndTime = ((XmlElement)xG3[0]).GetElementsByTagName("SystemTimeAndDate");
//            //XmlNode xEventos = xmldoc.LastChild;

//            XmlNodeList xEventos = ((XmlElement)xGeneral[0]).GetElementsByTagName("UserEventInformation");

//            //if (xEventos.LastChild.Name == "UserEventInformation")
//            //{
//            //    string numevento = xEventos.LastChild.InnerText.Substring(xEventos.LastChild.InnerText.Length - 1, 1);
//            //    List_odoEventos.Add(numevento);
//            //}
//            if (xEventos != null && xEventos.Count > 0)
//            {
//                string numevento = xEventos[0].InnerText.Substring(xEventos[0].InnerText.Length - 1, 1);
//                List_odoEventos.Add(numevento);
//            }
//            else
//            {
//                List_odoEventos.Add("");
//            }

//            foreach (XmlElement nodo in xDateAndTime)
//            {
//                List_odo.Add(new DateAndTime(nodo.InnerText));
//            }
//        }

//        /// <summary>
//        /// Se procesan todos los tiempos introduciendo 9 registros interpolados en el tiempo
//        /// </summary>
//        private void procesaTiemposOdo()
//        {
//            List_odoFinal = new List<string>();

//            String horaFinal;
//            for (int i = 0; i < List_odo.Count; i++)
//            {
//                horaFinal = List_odo.ElementAt(i).getFormatoODO_trac();

//                if (List_odoEventos.ElementAt(i) != "")
//                {
//                    string aux = "E>" + List_odoEventos.ElementAt(i) + "*" + horaFinal;
//                    List_odoFinal.Add(aux);  // Se introduce un elemento especial si hay evento
//                }
//                //else  // En caso de haber evento no se duplica 
//                //{
//                //    List_odoFinal.Add(horaFinal);
//                //}

//                // En caso de haber evento se duplica 
//                List_odoFinal.Add(horaFinal);

//                if (i != List_odo.Count - 1)
//                {
//                    DateAndTime aux = new DateAndTime(List_odo.ElementAt(i).hora);
//                    TimeSpan intervalo = TimeSpan.FromTicks((List_odo.ElementAt(i + 1).hora - List_odo.ElementAt(i).hora).Ticks / 10);

//                    for (int k = 1; k <= 9; k++)
//                    {
//                        aux.hora += intervalo;
//                        List_odoFinal.Add(aux.getFormatoODO_trac());
//                    }

//                }
//            }

//            // Duplico el primer registro porque se usa para la sincronización de archivos. En el GID este primer registro del Odometro no se exporta y es solo para la sincronizacion
//            List_odoFinal.Insert(0, List_odoFinal[0]);
//        }

//        /// <summary>
//        /// Crea el archivo GPS con las coordenadas
//        /// Ultima modificación. Añado un registro por cada 200ms. Si dispongo de el en la lista me lo invento interpolando entre los que tengo.
//        /// </summary>
//        private void CrearArchivoGPS()
//        {

//            String NombreArchivo = NombreDirectorioSave + "_GPS.txt";
//            //Console.WriteLine(NombreArchivo);

//            using (StreamWriter file = new StreamWriter(NombreArchivo, false, Encoding.GetEncoding("iso-8859-1")))
//            {
//                DateAndTime dtAux = new DateAndTime();


//                if (List_DatosGPS != null && List_DatosGPS.Count > 0)
//                {
//                    //dtAux = new DateTime ( List_DatosGPS[0].Time);
//                }

//                for (int i = 0; i < List_DatosGPS.Count; i++)
//                {
//                    addPuntoGPSalArchivo(file, List_DatosGPS.ElementAt(i));
//                }
//            }
//        }

//        /// <summary>
//        /// Añade un punto GPS al archivo
//        /// </summary>
//        /// <param name="file"></param>
//        /// <param name="datosGPS"></param>
//        private void addPuntoGPSalArchivo(StreamWriter _file, DatosGPS datosGPS)
//        {
//            _file.Write("$GPGGA");
//            _file.Write(",");
//            _file.Write(datosGPS.Time.getFormatoGPS_GPGGA());
//            _file.Write(",");
//            _file.Write(datosGPS.Latitude);
//            _file.Write(",");
//            _file.Write(datosGPS.NoS);
//            _file.Write(",");
//            _file.Write(datosGPS.Longitude);
//            _file.Write(",");
//            _file.Write(datosGPS.EoW);
//            _file.Write(",");
//            _file.Write(datosGPS.SignalQuality);
//            _file.Write(",");
//            _file.Write(datosGPS.NumSatelites);
//            _file.Write(",");
//            _file.Write("0");
//            _file.Write(",");
//            _file.Write(datosGPS.Altitude + ",M");
//            _file.Write(",");
//            _file.Write("0,M");
//            _file.Write(",");
//            _file.Write(",");
//            _file.Write("*75");
//            _file.WriteLine();
//        }

//        private void CrearArchivoODO()
//        {

//            String NombreArchivo = NombreDirectorioSave + "_ODO.trac";
//            //Console.WriteLine(NombreArchivo);

//            using (StreamWriter file = new StreamWriter(NombreArchivo, false, Encoding.GetEncoding("iso-8859-1")))
//            {
//                for (int i = 0; i < List_odoFinal.Count; i++)
//                {
//                    file.WriteLine(List_odoFinal.ElementAt(i));
//                }
//            }
//        }

//        private String Seleccionardirectorio()
//        {

//            string auxFecha = List_odoFinal.ElementAt(0);
//            if (auxFecha.Contains("*"))
//            {
//                auxFecha = auxFecha.Split('*')[1];
//            }
//            auxFecha = auxFecha.Split(':')[0] + "_" + auxFecha.Split(':')[1] + "_" + auxFecha.Split(':')[2];

//            SaveFileDialog saveFileDialog = new SaveFileDialog();
//            saveFileDialog.FileName = auxFecha;
//            saveFileDialog.ShowDialog();
//            directorio1 = new DirectoryInfo(saveFileDialog.FileName);

//            NombreDirectorioSave = directorio1.Parent.FullName + "\\" + directorio.Name + "_odo_gps";
//            //Console.WriteLine(NombreDirectorioSave);

//            if (!Directory.Exists(NombreDirectorioSave))
//            {
//                Directory.CreateDirectory(NombreDirectorioSave);
//            }

//            NombreDirectorioSave = directorio1.Parent.FullName + "\\" + directorio.Name + "_odo_gps\\" + auxFecha;

//            return NombreDirectorioSave;
//        }

//        private bool GenerarGPS(string _nombreArchivo, out List<string> _lMensajes, bool _bPrimero)
//        {
//            bool bError = false;

//            _lMensajes = new List<string>();

//            XmlNodeList xGeneral = xmldoc.GetElementsByTagName("LcmsAnalyserResults");
//            if (xGeneral == null || xGeneral.Count == 0)
//            {
//                _lMensajes.Add("ERROR en " + _nombreArchivo + ": No existe la etiqueta LcmsAnalyserResults. No pueden procesarse los datos GPS del archivo");

//                bError = true;
//                return bError;
//            }

//            if (_bPrimero)
//            {
//                // Para el primer archivo solo quiere guardarse el ultimo punto GPS que es el que nos proporcionará la sincronización entre tiempo GPS y ODOMETRO. Estas dos primeras lineas del .txt y .track de salida son importantísimas
//                XmlElement xG3Primero = dameCoordenada((XmlElement)xGeneral[0], _nombreArchivo, out _lMensajes);
//                if (xG3Primero == null)
//                {
//                    bError = true;
//                    return bError;
//                }

//                insertarNodoGPS(xG3Primero);
//            }
//            else
//            {
//                // Ahora se vuelcan todos los datos válidos
//                List<XmlElement> xG3s = dameCoordenadas((XmlElement)xGeneral[0], _nombreArchivo, out _lMensajes);
//                if (xG3s == null)
//                {
//                    bError = true;
//                    return bError;
//                }

//                foreach (XmlElement xG3 in xG3s)
//                {
//                    insertarNodoGPS(xG3);
//                }
//            }


//            return bError;
//        }

//        /// <summary>
//        /// Devuelve una coordenada del archivo xml. La ultima <GPSCoordinate> 
//        /// </summary>
//        /// <param name="xmlGPSInformation">etiqueta GPSInformation</param>
//        /// <returns>una etiqueta GPSCoordinate</returns>
//        private XmlElement dameCoordenada(XmlElement _xmlLcmsAnalyserResults, string _nombreArchivo, out List<string> _lMensajes)
//        {
//            _lMensajes = new List<string>();

//            XmlNodeList xGPSInformation = _xmlLcmsAnalyserResults.GetElementsByTagName("GPSInformation");
//            XmlElement xGPSCoor = null;  // El valor de salida
//            if (xGPSInformation == null || xGPSInformation.Count == 0)  // no existe la etiqueta, cojo la del anterior archivo valido
//            {
//                if (xGPSCoorValido == null) // si no habia etiqueta valida anterior es un fallo
//                {
//                    _lMensajes.Add("ERROR en " + _nombreArchivo + " : No existe la etiqueta GPSInformation ni una etiqueta del mismo tipo en archivos anteriores. No pueden procesarse los datos GPS del archivo");

//                    return null;
//                }
//                else
//                {
//                    _lMensajes.Add("WARNING en " + _nombreArchivo + " : No existe la etiqueta GPSInformation. Se procesa con el dato del archivo " + nombreArchivoValido);

//                    xGPSCoor = xGPSCoorValido;
//                }
//            }
//            else
//            {
//                XmlNodeList xNuevoGPSCoor = ((XmlElement)xGPSInformation[0]).GetElementsByTagName("GPSCoordinate");
//                if (xNuevoGPSCoor.Count > 0) // si no coordenadas
//                {
//                    // se busca la ultima coordenada con la etiqueta SignalQuality = 1

//                    bool bSalir = false;


//                    for (int i = xNuevoGPSCoor.Count - 1; i >= 0; i--)
//                    {
//                        if (bSalir)
//                        {
//                            break;
//                        }

//                        XmlElement coord = (XmlElement)xNuevoGPSCoor.Item(i);

//                        foreach (XmlNode nodoElements in coord.ChildNodes)
//                        {
//                            if (nodoElements.Name == "SignalQuality")
//                            {
//                                if (nodoElements.InnerText != "0")
//                                {
//                                    nombreArchivoValido = _nombreArchivo;
//                                    xGPSCoorValido = coord;

//                                    xGPSCoor = coord;
//                                    bSalir = true;
//                                    break;
//                                }
//                            }
//                        }
//                    }
//                }

//                if (xGPSCoor == null) // Todavia no he encontrado una coordenada valida
//                {
//                    if (xGPSCoorValido == null) // si no habia etiqueta valida anterior es un fallo
//                    {
//                        _lMensajes.Add("ERROR en " + _nombreArchivo + " : No existe la etiqueta GPSCoordinate ni una etiqueta del mismo tipo en archivos anteriores. No pueden procesarse los datos GPS del archivo");

//                        return null;
//                    }
//                    else
//                    {
//                        _lMensajes.Add("WARNING en " + _nombreArchivo + " : No existe la etiqueta GPSCoordinate. Se procesa con el dato del archivo " + nombreArchivoValido);

//                        xGPSCoor = xGPSCoorValido;
//                    }
//                }
//            }

//            return xGPSCoor;
//        }
//    }
//}
