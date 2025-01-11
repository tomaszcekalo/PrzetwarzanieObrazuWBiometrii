using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;
using System.Windows;
using static PrzetwarzanieObrazuWBiometrii.FeatureExtraction;
using System.Windows.Media.Imaging;
using System.IO;
using SixLabors.ImageSharp.Processing;
using SixLabors.Shapes;

namespace PrzetwarzanieObrazuWBiometrii
{
    /// <summary>
    /// Interaction logic for Zadanie6.xaml
    /// </summary>
    public partial class Zadanie6 : Window
    {
        private readonly FeatureExtraction _featureExtraction;
        private readonly Image<Rgba32> _sourceImage;
        private readonly Image<Rgba32> _fakeImage;

        public Zadanie6(FeatureExtraction featureExtraction, Image<Rgba32> sourceImage)
        {
            InitializeComponent();
            this._featureExtraction = featureExtraction;
            this._sourceImage = sourceImage;

            _fakeImage=GenerateFake(_sourceImage);
            
            SaveImage(_fakeImage, FakeImage);

        }
        public Image<Rgba32> GenerateFake(Image<Rgba32> source)
        {
            int x, y;
            var fake= new Image<Rgba32>(source.Width, source.Height);
            for( x= 0; x < source.Width; x++)
            {
                for ( y = 0; y < source.Height; y++)
                {
                    fake[x, y] = Color.White;
                }
            }
            //tworzenie ramki
            for ( x=1; x<source.Width-1; x++)
            {
                fake[x, 1] = Color.Black;   
                fake[x, source.Height - 2] = Color.Black;
            }
            for( y= 1; y < source.Height - 1; y++)
            {
                fake[1, y] = Color.Black;
                fake[source.Width - 2, y] = Color.Black;
            }
            //liczenie minucji
            var minutia=_featureExtraction.CrossingNumber(source);
            var minutiaByType = minutia.GroupBy(x => x.Type)
                .Select(x => new { Type = x.Key, Count = x.Count() })
                .ToDictionary(x=>x.Type, x => x.Count);
            //dodawanie "ikonek"
            x = 4;
            y = 4;
            var complex = minutiaByType.ContainsKey(CrossType.Complex) ? minutiaByType[CrossType.Complex] : 0;
            var starts = minutiaByType.ContainsKey(CrossType.Start) ? minutiaByType[CrossType.Start] : 0;
            var bifurcations = minutiaByType.ContainsKey(CrossType.Bifurcation) ? minutiaByType[CrossType.Bifurcation] : 0;
            
            // rysujemy X
            for (int i = 0; i < complex; i++)
            {
                fake[x, y] = Color.Black;
                fake[x + 1, y + 1] = Color.Black;
                fake[x + 2, y] = Color.Black;
                fake[x, y + 2] = Color.Black;
                fake[x + 2, y + 2] = Color.Black;

                starts -= 4;
                x += 4;
            }
            // rysujemy Y
            for(int i=0; i<bifurcations; i+=2)
            {
                fake[x, y] = Color.Black;
                fake[x + 1, y + 1] = Color.Black;
                fake[x + 2, y] = Color.Black;
                fake[x + 1, y + 2] = Color.Black;
                fake[x + 3, y] = Color.Black;
                fake[x, y + 3] = Color.Black;

                fake[x + 2, y + 3] = Color.Black;
                fake[x + 3, y + 3] = Color.Black;

                x += 4;
            }
            starts -= 4;
            // rysujemy minusy
            for (int i=0; i <= starts; i+=2)
            {
                fake[x, y] = Color.Black;
                fake[x + 1, y] = Color.Black;
                fake[x + 2, y] = Color.Black;

                x += 4;
            }
            return fake;
        }
        public Image<Rgba32> Copy(Image<Rgba32> input)
        {
            var output = new Image<Rgba32>(input.Width, input.Height);
            for (int x = 0; x < input.Width; x++)
            {
                for (int y = 0; y < input.Height; y++)
                {
                    output[x, y] = input[x, y];
                }
            }
            return output;
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
        public void SaveImage(Image<Rgba32> image, System.Windows.Controls.Image imageDisplay)
        {
            var fileName = @"./" + Guid.NewGuid().ToString() + ".png";
            image.Save(fileName);
            var bmp = ReadBitmapImageFromFileName(fileName);
            imageDisplay.Source = bmp;

        }

        public double MeasureDifferenceMinution(List<Minution> first, List<Minution> second)
        {
            var firstTable = first
                .GroupBy(x => x.Type)
                .Select(x => new { Type = x.Key, Count = x.Count() });

            var secondTable = second
                .GroupBy(x => x.Type)
                .Select(x => new { Type = x.Key, Count = x.Count() });

            var difference = 0.0;

            var firstStart=firstTable.FirstOrDefault(x => x.Type == CrossType.Start);
            var firstStartCount = firstStart?.Count ?? 0;
            var secondStart = secondTable.FirstOrDefault(x => x.Type == CrossType.Start);
            var secondStartCount = secondStart?.Count ?? 0;
            difference += Math.Pow(firstStartCount - secondStartCount, 2);
            var firstBifurcation = firstTable.FirstOrDefault(x => x.Type == CrossType.Bifurcation);
            var firstBifurcationCount = firstBifurcation?.Count ?? 0;
            var secondBifurcation = secondTable.FirstOrDefault(x => x.Type == CrossType.Bifurcation);
            var secondBifurcationCount = secondBifurcation?.Count ?? 0;
            difference += Math.Pow(firstBifurcationCount - secondBifurcationCount, 2);
            var firstComplex = firstTable.FirstOrDefault(x => x.Type == CrossType.Complex);
            var firstComplexCount = firstComplex?.Count ?? 0;
            var secondComplex = secondTable.FirstOrDefault(x => x.Type == CrossType.Complex);
            var secondComplexCount = secondComplex?.Count ?? 0;
            difference += Math.Pow(firstComplexCount - secondComplexCount, 2);

            return Math.Sqrt(difference);



        }

        private void MinutiaInOriginal_Click(object sender, RoutedEventArgs e)
        {
            var minutia=_featureExtraction.CrossingNumber(_sourceImage);
            string message = "Minutia in real image. ";
            message+=MinutiaMessage(minutia);
            MessageBox.Show(message);
        }
        private string MinutiaMessage(List<Minution> minutia)
        {
            var message = "";
            message += $" Starts: {minutia.Count(x => x.Type == CrossType.Start)}";
            message += $" Bifurcations: {minutia.Count(x => x.Type == CrossType.Bifurcation)}";
            message += $" Complex: {minutia.Count(x => x.Type == CrossType.Complex)}";
            return message;
        }

        private void MinutiaInFake_Click(object sender, RoutedEventArgs e)
        {
            var minutia=_featureExtraction.CrossingNumber(_fakeImage);
            string message = "Minutia in fake image ";
            message += MinutiaMessage(minutia); 
            MessageBox.Show(message);
        }

        private void MinutiaDifference_Click(object sender, RoutedEventArgs e)
        {
            var minutiaInFake = _featureExtraction.CrossingNumber(_fakeImage);
            var minutiaInOriginal = _featureExtraction.CrossingNumber(_sourceImage);
            var difference = MeasureDifferenceMinution(minutiaInOriginal, minutiaInFake);
            MessageBox.Show($"Difference: {difference}");
        }
    }
}
