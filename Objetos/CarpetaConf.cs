using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LCMS_ODO_GPS_GENERATOR.Objetos
{
    public partial class CarpetaConf
    {
        public string rutaCarpeta { get; set; }
        public string nombreCarpeta { get; set; }
        public string sentidoCalzada { get; set; }
        public int PKInicio { get; set; }

        public CarpetaConf(string nombreCarpeta, string rutaCarpeta, string sentidoCalzada = "+", int pkInicio = 0)
        {
            this.nombreCarpeta = nombreCarpeta;
            this.rutaCarpeta = rutaCarpeta;
            this.sentidoCalzada = sentidoCalzada;
            this.PKInicio = pkInicio;
        }


    }
}
