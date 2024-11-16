using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Schema;

namespace PrzetwarzanieObrazuWBiometrii
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Tresholding _tresholding = new Tresholding();
        private NiblackSauvolaPhansalkar _niblack = new NiblackSauvolaPhansalkar();

        public Image<Rgba32> SourceImage { get; private set; }
        public Image<Rgba32> BinarizedImage { get; private set; }
        public List<(int x, int y, Rgba32 rgba)> Pixels { get; private set; }

        public Boolean fill;


        public MainWindow()
        {
            InitializeComponent();
            fill = false;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog();

            bool? result = dialog.ShowDialog();

            if (result == true)
            {
                string fileName = dialog.FileName;
                var bitmap = new BitmapImage(new Uri(fileName));
                using (var input = File.OpenRead(fileName))
                {
                    SourceImage = SixLabors.ImageSharp.Image.Load<Rgba32>(input);
                    Pixels = new List<(int x, int y, Rgba32 rgba)>();
                    for (int x = 0; x < SourceImage.Width; x++)
                    {
                        for (int y = 0; y < SourceImage.Height; y++)
                        {
                            Pixels.Add((x, y, SourceImage[x, y]));
                        }
                    }
                }
                ImageDisplay.Source = bitmap;
            }
        }

        private void ImageDisplay_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            System.Windows.Controls.Image image = sender as System.Windows.Controls.Image;

            if (image != null)
            {
                System.Windows.Point clickPosition = e.GetPosition(image);

                BitmapSource bitmapSource = image.Source as BitmapSource;
                if (bitmapSource != null)
                {
                    double originalWidth = bitmapSource.PixelWidth;
                    double originalHeight = bitmapSource.PixelHeight;


                    double controlWidth = image.ActualWidth;
                    double controlHeight = image.ActualHeight;

                    double x = clickPosition.X * (originalWidth / controlWidth);
                    double y = clickPosition.Y * (originalHeight / controlHeight);

                    if (fill)
                    {
                        wypelnijProsty((int)x, (int)y);
                    }
                }
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            _tresholding.MainWindow = this;
            _tresholding.Show();
        }

        public void Tresholding(int Treshold)
        {
            var black = new Rgba32(0, 0, 0);
            var white = new Rgba32(255, 255, 255);
            ;
            var image = new Image<Rgba32>(SourceImage.Width, SourceImage.Height);
            for (int x = 0; x < image.Width; x++)
            {
                for (int y = 0; y < image.Height; y++)
                {
                    if (SourceImage[x, y].R < Treshold)
                    {
                        image[x, y] = black;
                    }
                    else
                    {
                        image[x, y] = white;
                    }
                }
            }
            SaveImage(image);
        }
        public void SaveImage(Image<Rgba32> image, System.Windows.Controls.Image imageDisplay)
        {
            var fileName = @"./" + Guid.NewGuid().ToString() + ".png";
            image.Save(fileName);
            var bmp = ReadBitmapImageFromFileName(fileName);
            imageDisplay.Source = bmp;
            BinarizedImage = image;
        }
        public void SaveImage(Image<Rgba32> image)
        {
            SaveImage(image, ImageDisplay_Copy);
        }

        public BitmapImage ReadBitmapImageFromFileName(string fileName)
        {
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.StreamSource = File.OpenRead(fileName);
            bmp.EndInit();
            return bmp;
        }

        public void Phansalkar(int boundary, double bias, double dynamicRange, double magnitude, double q)
        {
            var black = new Rgba32(0, 0, 0);
            var white = new Rgba32(255, 255, 255);
            var image = new Image<Rgba32>(SourceImage.Width, SourceImage.Height);
            for (int x = 0; x < SourceImage.Width - 1; x++)
            {
                for (int y = 0; y < SourceImage.Height - 1; y++)
                {
                    float sum = 0;
                    var count = 0;
                    for (int i = Math.Max(0, x - boundary); i < Math.Min(image.Width, x + boundary); i++)
                    {
                        for (int j = Math.Max(0, y - boundary); j < Math.Min(image.Height, y + boundary); j++)
                        {
                            if (x != i && y != j)
                            {
                                count++;
                                sum += SourceImage[i, j].R;
                            }
                        }
                    }
                    var mean = sum / count;
                    sum = 0;

                    for (int i = Math.Max(0, x - boundary); i < Math.Min(image.Width, x + boundary); i++)
                    {
                        for (int j = Math.Max(0, y - boundary); j < Math.Min(image.Height, y + boundary); j++)
                        {
                            if (x != i && y != j)
                                sum += (SourceImage[i, j].R - mean) * (SourceImage[i, j].R - mean);
                        }
                    }
                    var stdDev = Math.Sqrt(sum / count);

                    var ph = magnitude * Math.Exp(-q * mean);
                    var treshold = mean * (1 + ph + bias * ((stdDev / dynamicRange) - 1));
                    if (SourceImage[x, y].R < treshold)
                    {
                        image[x, y] = black;
                    }
                    else
                    {
                        image[x, y] = white;
                    }
                }
            }
            SaveImage(image);
        }

        public void Sauvola(int boundary, double bias, float dynamicRange)
        {
            var black = new Rgba32(0, 0, 0);
            var white = new Rgba32(255, 255, 255);
            var image = new Image<Rgba32>(SourceImage.Width, SourceImage.Height);
            for (int x = 0; x < SourceImage.Width - 1; x++)
            {
                for (int y = 0; y < SourceImage.Height - 1; y++)
                {
                    float sum = 0;
                    var count = 0;
                    for (int i = Math.Max(0, x - boundary); i < Math.Min(image.Width, x + boundary); i++)
                    {
                        for (int j = Math.Max(0, y - boundary); j < Math.Min(image.Height, y + boundary); j++)
                        {
                            if (x != i && y != j)
                            {
                                count++;
                                sum += SourceImage[i, j].R;
                            }
                        }
                    }
                    var mean = sum / count;
                    sum = 0;

                    for (int i = Math.Max(0, x - boundary); i < Math.Min(image.Width, x + boundary); i++)
                    {
                        for (int j = Math.Max(0, y - boundary); j < Math.Min(image.Height, y + boundary); j++)
                        {
                            if (x != i && y != j)
                                sum += (SourceImage[i, j].R - mean) * (SourceImage[i, j].R - mean);
                        }
                    }
                    var stdDev = Math.Sqrt(sum / count);

                    var treshold = mean * (1 + bias * ((stdDev / dynamicRange) - 1));
                    if (SourceImage[x, y].R < treshold)
                    {
                        image[x, y] = black;
                    }
                    else
                    {
                        image[x, y] = white;
                    }
                }
            }
            SaveImage(image);
        }

        public void Niblack(int boundary, double bias)
        {
            var black = new Rgba32(0, 0, 0);
            var white = new Rgba32(255, 255, 255);
            var image = new Image<Rgba32>(SourceImage.Width, SourceImage.Height);
            for (int x = 0; x < SourceImage.Width - 1; x++)
            {
                for (int y = 0; y < SourceImage.Height - 1; y++)
                {
                    float sum = 0;
                    var count = 0;
                    for (int i = Math.Max(0, x - boundary); i < Math.Min(image.Width, x + boundary); i++)
                    {
                        for (int j = Math.Max(0, y - boundary); j < Math.Min(image.Height, y + boundary); j++)
                        {
                            if (x != i && y != j)
                            {
                                count++;
                                sum += SourceImage[i, j].R;
                            }
                        }
                    }
                    var mean = sum / count;
                    sum = 0;

                    for (int i = Math.Max(0, x - boundary); i < Math.Min(image.Width, x + boundary); i++)
                    {
                        for (int j = Math.Max(0, y - boundary); j < Math.Min(image.Height, y + boundary); j++)
                        {
                            if (x != i && y != j)
                                sum += (SourceImage[i, j].R - mean) * (SourceImage[i, j].R - mean);
                        }
                    }
                    var stdDev = Math.Sqrt(sum / count);

                    var treshold = mean + bias * stdDev;
                    if (SourceImage[x, y].R < treshold)
                    {
                        image[x, y] = black;
                    }
                    else
                    {
                        image[x, y] = white;
                    }
                }
            }
            SaveImage(image);
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            _niblack.MainWindow = this;
            _niblack.Show();
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            // Calculate the histogram
            var histogram = new int[256];
            foreach (var pixel in Pixels)
            {
                histogram[pixel.rgba.R]++;
            }
            // Calculate the cumulative histogram
            var cumulativeHistogram = new int[256];
            cumulativeHistogram[0] = histogram[0];
            for (int i = 1; i < 256; i++)
            {
                cumulativeHistogram[i] = cumulativeHistogram[i - 1] + histogram[i];
            }
            // Calculate the cumulative histogram normalized
            var cumulativeHistogramNormalized = new double[256];
            for (int i = 0; i < 256; i++)
            {
                cumulativeHistogramNormalized[i] = (double)cumulativeHistogram[i] / Pixels.Count;
            }
            // Compute the Mean Grayscale Intensity Value of the Image
            var mean = Pixels.Average(x => x.rgba.R);

            // Compute the Between-Class Variance for Each Possible Threshold Value
            var variance = new double[256];
            for (int i = 0; i < 256; i++)
            {
                var p0 = cumulativeHistogramNormalized[i];
                var p1 = 1 - p0;
                var m0 = 0.0;
                for (int j = 0; j < i; j++)
                    m0 += j * histogram[j];
                m0 = m0 / (p0 * Pixels.Count);

                var m1 = 0.0;
                for (int j = i; j < 256; j++)
                    m1 += j * histogram[j];
                m1 = m1 / (p1 * Pixels.Count);
                variance[i] = p0 * p1 * (m0 - m1) * (m0 - m1);
            }
            var threshold = variance.ToList().IndexOf(variance.Max());
            Tresholding(threshold);
        }

        private void Button_Click_4(object sender, RoutedEventArgs e)
        {
            double[] _averageRows = new double[SourceImage.Height];
            double[] _averageColumns = new double[SourceImage.Width];
            for (int x = 0; x < SourceImage.Width; x++)
            {
                for (int y = 0; y < SourceImage.Height; y++)
                {
                    _averageRows[y] += SourceImage[x, y].R;
                    _averageColumns[x] += SourceImage[x, y].R;
                }
            }
            for (int i = 0; i < _averageColumns.Length; i++)
            {
                _averageColumns[i] /= SourceImage.Height;
            }
            for (int i = 0; i < _averageRows.Length; i++)
            {
                _averageRows[i] /= SourceImage.Width;
            }
            var image = new Image<Rgba32>(SourceImage.Width, SourceImage.Height);
            var black = new Rgba32(0, 0, 0);
            var white = new Rgba32(255, 255, 255);
            for (int x = 0; x < SourceImage.Width; x++)
            {
                for (int y = 0; y < SourceImage.Height; y++)
                {
                    if (SourceImage[x, y].R < _averageColumns[x] && SourceImage[x, y].R < _averageRows[y])
                    {
                        image[x, y] = black;
                    }
                    else if (SourceImage[x, y].R > _averageColumns[x] && SourceImage[x, y].R > _averageRows[y])
                    {
                        image[x, y] = white;
                    }
                    else
                    {
                        image[x, y] = SourceImage[x, y];
                    }
                }
            }
            SaveImage(image);
        }

        private void ZhangSuenButton_Click(object sender, RoutedEventArgs e)
        {
            var image = new Image<Rgba32>(BinarizedImage.Width, BinarizedImage.Height);
            int changed = 0;

            for (int x = 0; x < BinarizedImage.Width; x++)
            {
                for (int y = 0; y < BinarizedImage.Height; y++)
                {
                    image[x, y] = BinarizedImage[x, y];
                }
            }
            do
            {
                List<(int x, int y)> toChange = new List<(int x, int y)>();
                changed = 0;
                // Step 1
                for (int x = 1; x < image.Width - 1; x++)
                {
                    for (int y = 1; y < image.Height - 1; y++)
                    {
                        if (image[x, y].R != 0)
                        {
                            continue;
                        }
                        int[] neighbors = new int[8];
                        // top
                        neighbors[0] = image[x, y - 1].R;
                        // top right 
                        neighbors[1] = image[x + 1, y - 1].R;
                        // right
                        neighbors[2] = image[x + 1, y].R;
                        // bottom right
                        neighbors[3] = image[x + 1, y + 1].R;
                        // bottom
                        neighbors[4] = image[x, y + 1].R;
                        // bottom left
                        neighbors[5] = image[x - 1, y + 1].R;
                        // left
                        neighbors[6] = image[x - 1, y].R;
                        // top left
                        neighbors[7] = image[x - 1, y - 1].R;

                        int transitions = 0;
                        for (int i = 0; i < 8; i++)
                        {
                            if (neighbors[i] == 0 && neighbors[(i + 1) % 8] == 255)
                            {
                                transitions++;
                            }
                        }
                        int blackNeighbors = neighbors.Count(x => x == 0);
                        if (transitions == 1 && blackNeighbors >= 2 && blackNeighbors <= 6)
                        {
                            if (neighbors[0] + neighbors[2] + neighbors[4] > 0 && neighbors[2] + neighbors[4] + neighbors[6] > 0)
                            {
                                toChange.Add((x, y));
                            }
                        }
                    }
                }

                foreach (var pixel in toChange)
                {
                    image[pixel.x, pixel.y] = new Rgba32(255, 255, 255);
                    changed++;
                }
                toChange.Clear();
                // Step 2
                for (int x = 1; x < image.Width - 1; x++)
                {
                    for (int y = 1; y < image.Height - 1; y++)
                    {
                        if (image[x, y].R != 0)
                        {
                            continue;
                        }
                        int[] neighbors = new int[8];
                        // top
                        neighbors[0] = image[x, y - 1].R;
                        // top right 
                        neighbors[1] = image[x + 1, y - 1].R;
                        // right
                        neighbors[2] = image[x + 1, y].R;
                        // bottom right
                        neighbors[3] = image[x + 1, y + 1].R;
                        // bottom
                        neighbors[4] = image[x, y + 1].R;
                        // bottom left
                        neighbors[5] = image[x - 1, y + 1].R;
                        // left
                        neighbors[6] = image[x - 1, y].R;
                        // top left
                        neighbors[7] = image[x - 1, y - 1].R;

                        int transitions = 0;
                        for (int i = 0; i < 8; i++)
                        {
                            if (neighbors[i] == 0 && neighbors[(i + 1) % 8] == 255)
                            {
                                transitions++;
                            }
                        }
                        int blackNeighbors = neighbors.Count(x => x == 0);
                        if (transitions == 1 && blackNeighbors >= 2 && blackNeighbors <= 6)
                        {
                            if (neighbors[0] + neighbors[2] + neighbors[6] > 0 && neighbors[0] + neighbors[4] + neighbors[6] > 0)
                            {
                                toChange.Add((x, y));
                            }
                        }
                    }
                }
                foreach (var pixel in toChange)
                {
                    image[pixel.x, pixel.y] = new Rgba32(255, 255, 255);
                    changed++;
                }
            }
            while (changed > 0);
            SaveImage(image, SkeletonizationImage);
        }

        private void Button_Click_5(object sender, RoutedEventArgs e)
        {
        }

        private void Button_Wypełnij(object sender, RoutedEventArgs e)
        {
            fill = !fill;

            if (fill)
            {
                MessageBox.Show("Wypełniam");
            }
            else
            {
                MessageBox.Show("nie wypełniam");
            }


        }

        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            if (textBox != null)
            {
                if (int.TryParse(textBox.Text, out int value))
                {
                    if (value < 0)
                    {
                        textBox.Text = "0";
                    }
                    else if (value > 255)
                    {
                        textBox.Text = "255";
                    }
                }
                else
                {
                    textBox.Text = "0";
                }
            }
            Int32.TryParse(Red.Text, out int redC);
            Int32.TryParse(Green.Text, out int greenC);
            Int32.TryParse(Blue.Text, out int blueC);

            PickedColor.Fill = new SolidColorBrush(System.Windows.Media.Color.FromRgb(((byte)redC), (byte)greenC, (byte)blueC));
        }

        private bool IsTextNumeric(string text)
        {
            // Używamy wyrażenia regularnego, aby sprawdzić, czy tekst zawiera tylko cyfry
            Regex regex = new Regex("^[0-9]+$");
            return regex.IsMatch(text);
        }

        private void TextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Umożliwiamy używanie klawiszy takich jak Backspace, Delete, Tab, Left, Right
            if (e.Key == Key.Back || e.Key == Key.Delete || e.Key == Key.Tab || e.Key == Key.Left || e.Key == Key.Right)
            {
                e.Handled = false;
            }
            // Zablokuj spację
            else if (e.Key == Key.Space)
            {
                e.Handled = true;
            }
        }

        private void textBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsTextNumeric(e.Text);
        }

        private void wypelnijProsty(int x, int y)
        {
            MessageBox.Show($"Kliknięto na pikselu: X={x}, Y={y}");

            MessageBox.Show($"Obraz ma: X={SourceImage.Size.Width}, Y={SourceImage.Size.Height}");
        }


    }
}