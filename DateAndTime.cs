using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LCMS_ODO_GPS_GENERATOR
{
    public class DateAndTime
    {
        public TimeSpan hora { get; set; }

        public DateTime dia { get; set; }  // Solo guardo el día.

        public DateAndTime()
        {
        }

        /// <summary>
        /// Lee el siguiente formato 2021/09/16 09:59:34.6740   y lo almacena en la estructura
        /// </summary>
        /// <param name="texto"></param>
        public DateAndTime (string texto)
        {
            if (texto.Contains("."))  // formato 2021/09/16 09:59:34.6740
            {
                string[] spliter = texto.Split(' ')[1].Split(':');

                int horas = Convert.ToInt32(spliter[0]);
                int minutos = Convert.ToInt32(spliter[1]);

                string[] spliterSeg = spliter[2].Split('.');

                int segundos = Convert.ToInt32(spliterSeg[0]);

                int miliSegundos = Convert.ToInt32(spliterSeg[1]);
                if (spliterSeg[1].Length == 4)
                {
                    miliSegundos /= 10; // Si los milisegundos estan en 4 cifras quito la última. Es el caso general.. Meten un 0 al final y equivoca al sistema. 6740
                }

                hora = new TimeSpan(0, horas, minutos, segundos, miliSegundos);

                try
                {
                    string[] valoresFecha = texto.Split(' ')[0].Split('/');
                    dia = new DateTime(Convert.ToInt32(valoresFecha[0]), Convert.ToInt32(valoresFecha[1]), Convert.ToInt32(valoresFecha[2]));
                }
                catch (Exception)  // Fallo en el formato de fecha
                {
                }
                
            }
            else  // Formato 07:59:34:200
            {
                string[] spliter = texto.Split(':');

                int horas = Convert.ToInt32(spliter[0]);
                int minutos = Convert.ToInt32(spliter[1]);
                int segundos = Convert.ToInt32(spliter[2]);
                int miliSegundos = Convert.ToInt32(spliter[3]);
                
                hora = new TimeSpan(0, horas, minutos, segundos, miliSegundos);
            }
            

        }

        public DateAndTime(TimeSpan _hora)
        {
            hora = new TimeSpan(_hora.Days, _hora.Hours, _hora.Minutes, _hora.Seconds, _hora.Milliseconds);
        }

        /// <summary>
        /// devuelve la hora en cadena de texto con formato HH:MM:SS:mmm
        /// </summary>
        /// <param name="ms"></param>
        /// <returns></returns>
        public string getFormatoODO_trac ()
        {
            string salida = hora.Hours.ToString().PadLeft(2,'0') + ":" +
                            hora.Minutes.ToString().PadLeft(2, '0') + ":" +
                            hora.Seconds.ToString().PadLeft(2, '0') + ":" +
                            hora.Milliseconds.ToString().PadLeft(3, '0');
            return salida;
        }

        /// <summary>
        /// devuelve la hora en cadena de texto con formato 075934.20
        /// </summary>
        /// <param name="ms"></param>
        /// <returns></returns>
        public string getFormatoGPS_GPGGA()
        {
            string salida = hora.Hours.ToString().PadLeft(2, '0') +
                            hora.Minutes.ToString().PadLeft(2, '0') +
                            hora.Seconds.ToString().PadLeft(2, '0') + "." + 
                            (hora.Milliseconds / 10).ToString().PadLeft(2, '0');
            return salida;
        }
    }
}
