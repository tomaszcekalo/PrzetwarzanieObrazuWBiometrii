using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;

namespace PrzetwarzanieObrazuWBiometrii
{
    public class FeatureExtraction
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
        public bool ShouldSkipLeft(in Image<Rgba32> bitmap, int x, int y)
        {
            bool shouldSkip = true;
            // if all pixels on the left are white then it should be skipped
            for (int i = x - 1; i > 0; --i)
            {
                if (bitmap[i, y].R == 0)
                {
                    shouldSkip = false;
                }
            }
            return shouldSkip;
        }
        public bool ShouldSkipRight(in Image<Rgba32> bitmap, int x, int y)
        {
            bool shouldSkip = true;
            // if all pixels on the right are white then it should be skipped
            for (int i = x + 1; i < bitmap.Width; ++i)
            {
                if (bitmap[i, y].R == 0)
                {
                    shouldSkip = false;
                }
            }
            return shouldSkip;
        }
        public bool ShouldSkipTop(in Image<Rgba32> bitmap, int x, int y)
        {
            bool shouldSkip = true;
            // if all pixels on the top are white then it should be skipped
            for (int i = y - 1; i > 0; --i)
            {
                if (bitmap[x, i].R == 0)
                {
                    shouldSkip = false;
                }
            }
            return shouldSkip;
        }
        public bool ShouldSkipBottom(in Image<Rgba32> bitmap, int x, int y)
        {
            bool shouldSkip = true;
            // if all pixels on the bottom are white then it should be skipped
            for (int i = y + 1; i < bitmap.Height; ++i)
            {
                if (bitmap[x, i].R == 0)
                {
                    shouldSkip = false;
                }
            }
            return shouldSkip;
        }
        public bool ShoulAddMinutia(in Image<Rgba32> bitmap, int x, int y)
        {
            if(ShouldSkipLeft(bitmap, x, y) || ShouldSkipRight(bitmap, x, y) || ShouldSkipTop(bitmap, x, y) || ShouldSkipBottom(bitmap, x, y))
            {
                return false;
            }
            return true;
        }
        public Image<Rgba32> CrossingNumber(in Image<Rgba32> bitmap)
        {
            var output = Copy(bitmap);
            List<Minution> minution = new List<Minution>();

            for (int x = 1; x < bitmap.Width - 1; ++x)
            {
                for (int y = 1; y < bitmap.Height - 1; ++y)
                {
                    if (bitmap[x, y].R == 0)
                    {
                        Minution min = Classify(bitmap, x, y);
                        if (min.type == CrossType.Start ||
                            min.type == CrossType.Bifurcation ||
                            min.type == CrossType.Complex)
                        {
                            if(ShoulAddMinutia(bitmap, x, y))
                                minution.Add(min);
                        }
                    }
                }
            }

            foreach (var min in minution)
            {
                output[min.x, min.y] = TypeToColor(min.type);
            }
            return output;
        }

        private Color TypeToColor(CrossType type)
        {
            switch (type)
            {
                case CrossType.Inside: return Color.Teal;
                case CrossType.Start: return Color.Blue;
                case CrossType.Line: return Color.Pink;
                case CrossType.Bifurcation: return Color.Red;
                case CrossType.Complex: return Color.Orange;
                default: return Color.Green;
            }
        }

        public enum CrossType
        {
            Inside = 0,
            Start = 1,
            Line = 2,
            Bifurcation = 3,
            Complex = 4
        }
        public struct Minution
        {
            public int x;
            public int y;
            public CrossType type;
        }
        public Minution Classify(Image<Rgba32> binaryImage, int x, int y)
        {
            int count = 0;

            int[] neighbours = [ binaryImage[x-1, y-1].R, binaryImage[x, y - 1].R, binaryImage[x + 1, y - 1].R,
                binaryImage[x + 1, y].R, binaryImage[x + 1, y + 1].R, binaryImage[x, y + 1].R,
                binaryImage[x - 1, y + 1].R, binaryImage[x - 1, y].R, binaryImage[x-1, y-1].R ]; //last and first the same for cycle close

            int before = neighbours[0];

            foreach (var neigh in neighbours)
            {
                count += before != neigh ? 1 : 0;
                before = neigh;
            }
            count /= 2;
            CrossType type;
            switch (count)
            {
                case 0: type = CrossType.Inside; break;
                case 1: type = CrossType.Start; break;
                case 2: type = CrossType.Line; break;
                case 3: type = CrossType.Bifurcation; break;
                case 4: type = CrossType.Complex; break;
                default: throw new Exception("Not possible"); break;
            }

            return new Minution { type = type, x = x, y = y };

        }
    }
}
