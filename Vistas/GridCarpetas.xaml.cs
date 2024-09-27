using LCMS_ODO_GPS_GENERATOR.Objetos;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Runtime.Remoting.Channels;
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
    /// Lógica de interacción para GridCarpetas.xaml
    /// </summary>
    public partial class GridCarpetas : Window
    {
        public List<CarpetaConf> listaCarpetasConf = new List<CarpetaConf>();
        public GridCarpetas()
        {
            InitializeComponent();
        }

        void confMyDataGrid()
        {
            DataTable dt = new DataTable();
            DataColumn rutaCarpeta = new DataColumn("Ruta Carpeta", typeof(string));
            dt.Columns.Add(rutaCarpeta);
            dt.Columns[0].ReadOnly = true;

            DataColumn nombre = new DataColumn("Nombre Carpeta", typeof(string));
            dt.Columns.Add(nombre);
            dt.Columns[1].ReadOnly = true;

            DataColumn sentidoCalzada = new DataColumn("Sentido Calzada", typeof(string));
            sentidoCalzada.MaxLength = 1;
            dt.Columns.Add(sentidoCalzada);

            DataColumn pkInicio = new DataColumn("PK Inicio", typeof(int));
            pkInicio.DataType = typeof(int);
            dt.Columns.Add(pkInicio);

            foreach (CarpetaConf c in listaCarpetasConf)
            {
                DataRow row = dt.NewRow();
                row[0] = c.rutaCarpeta;
                row[1] = c.nombreCarpeta;
                row[2] = c.sentidoCalzada;
                row[3] = c.PKInicio;
                
                dt.Rows.Add(row);
            }

            myDataGrid.ItemsSource = dt.DefaultView;
            myDataGrid.SelectionMode = DataGridSelectionMode.Single;
            myDataGrid.SelectionUnit = DataGridSelectionUnit.Cell;

            var columnaRutaCarpeta = myDataGrid.Columns.FirstOrDefault(c => c.Header.ToString() == "Ruta Carpeta");
            if (columnaRutaCarpeta != null)
            {
                columnaRutaCarpeta.Visibility = Visibility.Collapsed;
            }
        }

        public GridCarpetas(List<CarpetaConf> listaCarpetasConf)
        {
            InitializeComponent();
            this.listaCarpetasConf = listaCarpetasConf;
            //dgCarpetas.ItemsSource = listaCarpetasConf;
        }

        private void btnAceptarCarpetaConf_Click(object sender, RoutedEventArgs e)
        {
            //Igualamos la lista actualizada para posteriormente recorrerla y validarla

            DataView dataView = myDataGrid.ItemsSource as DataView;

            listaCarpetasConf.Clear();
            if (dataView != null)
            {
                foreach (DataRowView rowView in dataView)
                {
                    DataRow row = rowView.Row;
                    string rutaCarpeta = row["Ruta Carpeta"].ToString();
                    string nom = row["Nombre Carpeta"].ToString();
                    string sentidoCalzada = row["Sentido Calzada"].ToString();
                    int PKInicio = Convert.ToInt32(row["PK Inicio"]);

                    CarpetaConf carpeta = new CarpetaConf(nom, rutaCarpeta, sentidoCalzada, PKInicio);

                    listaCarpetasConf.Add(carpeta);
                }
            }

            //listaCarpetasConf = (List<CarpetaConf>)myDataGrid.ItemsSource;

            var error = false;
            //Comprobamos que en sentido calzada solo hayan valores  + o -
            foreach (CarpetaConf c in listaCarpetasConf)
            {
                if (!c.sentidoCalzada.Equals("+") && !c.sentidoCalzada.Equals("-") && !c.sentidoCalzada.Equals("1") && !c.sentidoCalzada.Equals("2")
                    && !c.sentidoCalzada.Equals("C") && !c.sentidoCalzada.Equals("D") && !c.sentidoCalzada.Equals("c") && !c.sentidoCalzada.Equals("d"))
                {
                    //MessageBox.Show("El sentido de la calzada solo admite valores '+' o '-'");
                    MessageBox.Show("El valor en el sentido de la calzada es incorrecto", "ERROR");
                    error = true;
                    return;
                }
            }

            if (!error)
            {
                this.DialogResult = true;
                this.Close();
            }

        }

        private DataGridCell GetCellUnderMouse(DataGrid dataGrid, MouseButtonEventArgs e)
        {
            // Obtener el elemento original bajo el puntero
            DependencyObject dep = (DependencyObject)e.OriginalSource;

            // Subir en la jerarquía visual hasta que se encuentre una celda de DataGrid
            while (dep != null && !(dep is DataGridCell))
            {
                dep = VisualTreeHelper.GetParent(dep);
            }

            return dep as DataGridCell;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            confMyDataGrid();
        }
    }
}
