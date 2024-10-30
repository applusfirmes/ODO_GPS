using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace LCMS_ODO_GPS_GENERATOR.Vistas
{
    /// <summary>
    /// Lógica de interacción para VentanaInfo.xaml
    /// </summary>
    public partial class VentanaInfo : Window
    {
        public VentanaInfo()
        {
            InitializeComponent();
            completarWarnings();
        }

        public void completarWarnings()
        {
            foreach (string warning in GlobalController.ListInfoWarnings)
            {
                tbErrores.Text += warning + Environment.NewLine;
            }
        }

        public void btnAceptar_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
