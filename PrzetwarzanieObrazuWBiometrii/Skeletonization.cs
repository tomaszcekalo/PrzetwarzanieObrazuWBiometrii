using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Media3D;

namespace PrzetwarzanieObrazuWBiometrii
{
    public class Skeletonization
    {
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
        int[] A0 = { 3, 6, 7, 12, 14, 15, 24, 28, 30, 31, 48, 56, 60, 62, 63, 96, 112, 120, 124, 126, 127, 129, 131, 135, 143, 159, 191, 192, 193, 195, 199, 207, 223, 224, 225, 227, 231, 239, 240, 241, 243, 247, 248, 249, 251, 252, 253, 254 };
        int[] A1 = { 7, 14, 28, 56, 112, 131, 193, 224 };
        int[] A2 = { 7, 14, 15, 24, 28, 30, 48, 56, 60, 112, 120, 131, 135, 192, 193, 195, 224, 225, 240 };
        int[] A3 = { 7, 14, 15, 28, 30, 31, 56, 60, 62, 112, 120, 124, 131, 135, 143, 193, 195, 199, 224, 225, 227, 240, 241, 248 };
        int[] A4 = { 7, 14, 15, 28, 30, 31, 56, 60, 62, 63, 112, 120, 124, 126, 131, 135, 143, 159, 193, 195, 199, 207, 224, 225, 227, 231, 240, 241, 243, 248, 249, 252 };
        int[] A5 = { 7, 14, 15, 28, 30, 31, 56, 60, 62, 63, 112, 120, 124, 126, 131, 135, 143, 159, 191, 193, 195, 199, 207, 224, 225, 227, 231, 239, 240, 241, 243, 248, 249, 251, 252, 254 };


        int[][] A; 
        public Skeletonization()
        {
            A= [A0, A1, A2, A3, A4, A5];
        }

        int[] A1pix = { 2, 5, 13, 20, 21, 22, 32, 48, 52, 54, 65, 67, 69, 80, 81, 84, 88, 97, 99, 128, 133, 141, 208, 216 };

        public Image<Rgba32> K3M(Image<Rgba32> input)
        {
            var output=Copy(input);
            output.Mutate(x=>x.Invert());
            bool pixelChanged;
            BorderWeight[,] border = new BorderWeight[input.Width, input.Height];

            do
            {
                List<(int x, int y)> toChange = new List<(int x, int y)>();

                pixelChanged = false;
                K3Mphase0(output, border);
                K3Mphase0a(output, border);

                for (int phase = 1; phase <= 5; ++phase)
                {
                    (var changed, var btd) = K3Mphase(phase, output, border);
                    if (btd.Count > 0 || changed)
                        pixelChanged = true;

                }

                for (int x = 0; x < input.Width; ++x)
                {
                    for (int y = 0; y < input.Height; ++y)
                    {
                        if (border[x, y].border == 1)
                        {
                            border[x, y].border = 0;
                            border[x, y].deletion = 0;
                        }

                        //binaryImage[x, y] = 2;
                    }
                }

            } while (pixelChanged);


            output.Mutate(x => x.Invert());
            return output;
        }
        public struct BorderWeight
        {
            public int border;
            public int weight;
            public int deletion;

        }
        private void K3Mphase0(in Image<Rgba32> image, in BorderWeight[,] border)
        {
            for (int y = 0; y < image.Height; ++y)
            {
                for (int x = 0; x < image.Width; ++x)
                {
                    if (image[x, y].R == 255)
                    {
                        int cw = CalcWeight(image, x, y, border);
                        if (A0.Contains(cw))
                        {
                            if (cw == 193)
                            {
                                if (x - 1 >= 0 && y - 1 >= 0)
                                {
                                    if (image[x - 1, y - 1].R == 255)
                                    {
                                        border[x - 1, y - 1].weight = CalcWeight(image, x - 1, y - 1, border);
                                        border[x - 1, y - 1].border = 1;
                                    }
                                }
                            }
                            border[x, y].weight = CalcWeight(image, x, y, border);
                            border[x, y].border = 1;
                        }
                        else if (checkCondition1(image, x, y, cw))
                        {
                            border[x, y].weight = CalcWeight(image, x, y, border);
                            border[x, y].border = 1;
                        }
                    }
                }
            }
        }
        private void K3Mphase0a(in Image<Rgba32> image, in BorderWeight[,] border)
        {
            for (int y = 0; y < image.Height; ++y)
            {
                for (int x = 0; x < image.Width; ++x)
                {
                    if (image[x, y].R == 255 && border[x, y].border == 0)
                    {
                        int bw = CalcWeight(image, x, y, border);
                        if (bw == 31 || bw == 124)
                        {
                            border[x, y].weight = bw;
                            border[x, y].border = 1;
                        }
                    }
                }
            }
        }
        private (bool, List<(int x, int y)>) K3Mphase(int phase, Image<Rgba32> image, BorderWeight[,] border)
        {
            List<(int x, int y)> values = new List<(int x, int y)>();
            bool pixelsChanged = false;
            for (int y = 0; y < image.Height; ++y)
            {
                for (int x = 0; x < image.Width; ++x)
                {
                    if (border[x, y].border == 1 || border[x, y].deletion == 1)
                    {
                        int wb = CalcWeight(image, x, y, border);
                        if (A[phase].Contains(wb) || border[x, y].deletion == 1)
                        {
                            if (checkCondition2(image, x, y, wb, border))
                            {
                                int nb = CalcWeight(image, x, y + 1, border);
                                if (A[phase].Contains(nb))
                                {
                                    image[x, y + 1] = Color.Black;
                                    pixelsChanged = true;
                                    values.Add((x, y + 1));
                                }
                                image[x, y] = Color.Black;
                                values.Add((x,y));
                                pixelsChanged = true;
                            }
                            else if (checkCondition3(image, x, y, wb, border))
                            {
                                int nb = CalcWeight(image, x + 1, y - 1, border);
                                if (A[phase].Contains(nb))
                                {
                                    border[x + 1, y - 1].deletion = 1;
                                    pixelsChanged = true;
                                }
                                image[x, y] = Color.Black;
                                values.Add((x, y));
                                pixelsChanged = true;
                            }
                            else
                            {
                                image[x, y] = Color.Black;
                                values.Add((x, y));
                                pixelsChanged = true;
                            }
                        }
                    }
                }
            }

            return (pixelsChanged, values);
        }

        private int CalcWeight(Image<Rgba32> image, int x, int y, BorderWeight[,] border)
        {
            return iir(image, x, y - 1, border) +
                (2 * iir(image, x + 1, y - 1, border)) +
                (64 * iir(image, x - 1, y, border)) +
                (4 * iir(image, x + 1, y, border)) +
                (32 * iir(image, x - 1, y + 1, border)) +
                (16 * iir(image, x, y + 1, border)) +
                (8 * iir(image, x + 1, y + 1, border)) +
                (128 * iir(image, x - 1, y - 1, border));
        }
        public int iir(Image<Rgba32> image, int x, int y, BorderWeight[,] border)
        {
            if (x >= 0 && x < image.Width && y >= 0 && y < image.Height)
            {
                return image[x, y].R == 255 ? 1 : 0; 
            }
            else
            {
                return 0;
            }
        }
        private bool checkCondition3(Image<Rgba32> image, int x, int y, int weight, BorderWeight[,] border)
        {
            if (x + 1 < image.Width && y - 1 >= 0)
            {
                return (weight == 195 || weight == 227) && border[x + 1, y - 1].border == 1;
            }
            else
            {
                return false;
            }
        }

        private bool checkCondition2(Image<Rgba32> image, int x, int y, int weight, BorderWeight[,] border)
        {
            if (y + 2 < image.Height && x + 1 < image.Width)
            {
                return weight == 241 && border[x, y + 1].border == 1
                    && image[x, y + 2].R == 0 && image[x + 1, y + 2].R == 0;
            }
            else
            {
                return false;
            }
        }
        private bool checkCondition1(Image<Rgba32> image, int x, int y, int weight)
        {
            switch (weight)
            {
                case 95:
                    return checkWhite(image, x - 2, y);
                case 125:
                    return checkWhite(image, x, y - 2);
                case 215:
                    return checkWhite(image, x, y + 2);
                case 245:
                    return checkWhite(image, x + 2, y);
                default:
                    return false;
            }
        }
        private bool checkWhite(Image<Rgba32> image, int x, int y)
        {
            if (x >= 0 && x < image.Width
               && y >= 0 && y < image.Height)
            {
                return image[x, y].R == 0;
            }
            return false;
        }

    }
}
