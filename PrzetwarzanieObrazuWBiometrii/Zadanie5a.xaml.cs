using Microsoft.Win32;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SourceAFIS;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace PrzetwarzanieObrazuWBiometrii
{
    /// <summary>
    /// Interaction logic for Zadanie5a.xaml
    /// </summary>
    public partial class Zadanie5a : Window
    {
        string fingerPrint1Path;
        string fingerPrint2Path;
        public Zadanie5a()
        {
            InitializeComponent();
        }

        private void Compare()
        {
            if (fingerPrint1Path is not null && fingerPrint2Path is not null)
            {
                double similarity = CompareFingerprint();

                double threshold = 40;
                bool matches = similarity >= threshold;
                if (matches)
                {
                    MessageBox.Show($"Odciski palców są zgodne, stopien: {similarity}", "Poprawne");
                }
                else
                {
                    MessageBox.Show($"Brak zgodności w odciskach palca, stopien: {similarity}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void WczytajLewy_Click(object sender, RoutedEventArgs e)
        {
            fingerPrint1Path = Wczytaj(ImageLeft);
            Compare();
        }

        private void WczytajPrawy_Click(object sender, RoutedEventArgs e)
        {
            fingerPrint2Path = Wczytaj(ImageRight);
            Compare();
        }

        public double CompareFingerprint()
        {
            var probe = new FingerprintTemplate(new FingerprintImage(File.ReadAllBytes(fingerPrint1Path)));

            var candidate = new FingerprintTemplate(new FingerprintImage(File.ReadAllBytes(fingerPrint2Path)));

            var matcher = new FingerprintMatcher(probe);
            double similarity = matcher.Match(candidate);
            return similarity;
        }

        private string Wczytaj(System.Windows.Controls.Image wpfImage)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog();

            bool? result = dialog.ShowDialog();

            if (result == true)
            {
                string fileName = dialog.FileName;
                var bitmap = new BitmapImage(new Uri(fileName));
                wpfImage.Source = bitmap;
                return fileName;
            }
            return null;
        }
    }
}
