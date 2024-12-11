using DocumentFormat.OpenXml.Bibliography;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LCMS_ODO_GPS_GENERATOR.Recursos
{
    //Las carpetas se ordenan correctamente como 1, 2, 3, 4, 10, 11 en lugar de 1, 10, 11, 2, 3, 4.
    public class NaturalStringComparer : IComparer<string>
    {
        public int Compare(string x, string y)
        {
            if (x == null || y == null)
                return 0;

            var regex = new Regex(@"(\d+)|(\D+)");

            var xMatches = regex.Matches(x);
            var yMatches = regex.Matches(y);

            for (int i = 0; i < Math.Min(xMatches.Count, yMatches.Count); i++)
            {
                var xPart = xMatches[i].Value;
                var yPart = yMatches[i].Value;

                if (int.TryParse(xPart, out int xNum) && int.TryParse(yPart, out int yNum))
                {
                    // Comparar como números
                    int result = xNum.CompareTo(yNum);
                    if (result != 0) return result;
                }
                else
                {
                    // Comparar como cadenas
                    int result = string.Compare(xPart, yPart, StringComparison.Ordinal);
                    if (result != 0) return result;
                }
            }

            // Si todos los componentes son iguales, comparar por longitud o por el resto de la cadena
            return x.Length.CompareTo(y.Length);
        }
    }
}
