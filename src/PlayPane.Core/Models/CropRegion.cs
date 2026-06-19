using System;
using System.Runtime.Serialization;

namespace PlayPane.Core.Models
{
    [DataContract]
    public sealed class CropRegion
    {
        public CropRegion()
        {
            Width = 1;
            Height = 1;
        }

        public CropRegion(double x, double y, double width, double height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        [DataMember(Order = 1)]
        public double X { get; set; }

        [DataMember(Order = 2)]
        public double Y { get; set; }

        [DataMember(Order = 3)]
        public double Width { get; set; }

        [DataMember(Order = 4)]
        public double Height { get; set; }

        public static CropRegion Full
        {
            get { return new CropRegion(0, 0, 1, 1); }
        }

        public double AspectRatio
        {
            get
            {
                if (Height <= 0)
                {
                    return 1;
                }

                return Width / Height;
            }
        }

        public double GetAspectRatio(int sourceWidth, int sourceHeight)
        {
            PixelRect pixels = ToPixels(sourceWidth, sourceHeight);
            if (pixels.Height <= 0)
            {
                return 1;
            }

            return (double)pixels.Width / pixels.Height;
        }

        public static CropRegion FromPixels(int x, int y, int width, int height, int sourceWidth, int sourceHeight)
        {
            if (sourceWidth <= 0 || sourceHeight <= 0)
            {
                return Full;
            }

            return new CropRegion(
                (double)x / sourceWidth,
                (double)y / sourceHeight,
                (double)width / sourceWidth,
                (double)height / sourceHeight).Clamp();
        }

        public PixelRect ToPixels(int sourceWidth, int sourceHeight)
        {
            if (sourceWidth <= 0 || sourceHeight <= 0)
            {
                return new PixelRect(0, 0, 1, 1);
            }

            CropRegion clamped = Clamp();
            int x = (int)Math.Round(clamped.X * sourceWidth);
            int y = (int)Math.Round(clamped.Y * sourceHeight);
            int width = Math.Max(1, (int)Math.Round(clamped.Width * sourceWidth));
            int height = Math.Max(1, (int)Math.Round(clamped.Height * sourceHeight));

            if (x + width > sourceWidth)
            {
                width = Math.Max(1, sourceWidth - x);
            }

            if (y + height > sourceHeight)
            {
                height = Math.Max(1, sourceHeight - y);
            }

            return new PixelRect(x, y, width, height);
        }

        public CropRegion Clamp()
        {
            double width = ClampValue(Width, 0.01, 1);
            double height = ClampValue(Height, 0.01, 1);
            double x = ClampValue(X, 0, 1 - width);
            double y = ClampValue(Y, 0, 1 - height);

            return new CropRegion(x, y, width, height);
        }

        private static double ClampValue(double value, double minimum, double maximum)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                return minimum;
            }

            if (maximum < minimum)
            {
                maximum = minimum;
            }

            if (value < minimum)
            {
                return minimum;
            }

            if (value > maximum)
            {
                return maximum;
            }

            return value;
        }
    }
}
