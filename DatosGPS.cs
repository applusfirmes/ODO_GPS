using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LCMS_ODO_GPS_GENERATOR
{
    public class DatosGPS
    {
        public string Latitude { get; set; }
        public string Longitude { get; set; }
        public string Altitude { get; set; }
        public DateAndTime Time { get; set; }
        public string NumSatelites { get; set; }
        public string SignalQuality { get; set; }

        public string EoW { get; set; }

        public string NoS { get; set; }

        public bool? duplicado { get; set; }  // ayuda para saber si es un dato duplicado añadido artificialmente

        public DatosGPS()
        {
        }

        public DatosGPS(string _Latitude, string _Longitude, string _Altitude, DateAndTime _Time, string _NumSatelites, string _SignalQuality, string _EoW, string _NoS, bool? _duplicado)
        {
            Latitude = _Latitude;
            Longitude = _Longitude;
            Altitude = _Altitude;
            NumSatelites = _NumSatelites;
            SignalQuality = _SignalQuality;
            EoW = _EoW;
            NoS = _NoS;
            duplicado = _duplicado;

            Time = _Time;
        }

        public DatosGPS (DatosGPS _clone)
        {
            Latitude = _clone.Latitude;
            Longitude = _clone.Longitude;
            Altitude = _clone.Altitude;
            NumSatelites = _clone.NumSatelites;
            SignalQuality = _clone.SignalQuality;
            EoW = _clone.EoW;
            NoS = _clone.NoS;
            duplicado = _clone.duplicado;

            Time = new DateAndTime(_clone.Time.hora);

        }
    }
}
