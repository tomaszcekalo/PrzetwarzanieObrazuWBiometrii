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

namespace PrzetwarzanieObrazuWBiometrii
{
    /// <summary>
    /// Interaction logic for NiblackSauvolaPhansalkar.xaml
    /// </summary>
    public partial class NiblackSauvolaPhansalkar : Window
    {
        public MainWindow MainWindow { get; set; }
        public NiblackSauvolaPhansalkar()
        {
            InitializeComponent();
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.Niblack((int)boundarySlider.Value, biasSlider.Value);
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            MainWindow.Sauvola((int)boundarySlider.Value, biasSlider.Value, (int)DynamicRange.Value);
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            MainWindow.Phansalkar((int)boundarySlider.Value, biasSlider.Value, DynamicRange.Value, Magnitude.Value, Q.Value);
        }
    }
}
