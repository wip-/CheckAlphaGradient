using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;

namespace CheckAlphaGradation
{
    public static class Helpers
    {
        static public int GetBytesPerPixel(PixelFormat pixelFormat)
        {
            switch (pixelFormat)
            {
                case PixelFormat.Format8bppIndexed:
                    return 1;

                case PixelFormat.Format24bppRgb:
                    return 3;

                case PixelFormat.Format32bppArgb:
                    return 4;

                case PixelFormat.Format64bppArgb:
                    return 8;

                default:
                    Debug.Assert(false);
                    return 0;
            }
        }

        static public bool HasAlphaChannel(PixelFormat pixelFormat)
        {
            switch (pixelFormat)
            {
                case PixelFormat.Format8bppIndexed:
                case PixelFormat.Format24bppRgb:
                    return false;

                case PixelFormat.Format32bppArgb:
                case PixelFormat.Format64bppArgb:
                    return true;

                default:
                    Debug.Assert(false);
                    return false;
            }
        }

        static public Color GetColorDiff(Color left, Color right)
        {
            int a = Math.Abs(left.A - right.A);
            int r = Math.Abs(left.R - right.R);
            int g = Math.Abs(left.G - right.G);
            int b = Math.Abs(left.B - right.B);

            return Color.FromArgb(
                Convert.ToByte(a.Clamp0_255()),
                Convert.ToByte(r.Clamp0_255()),
                Convert.ToByte(g.Clamp0_255()),
                Convert.ToByte(b.Clamp0_255()));
        }

        public static int Clamp0_255(this int value)
        {
            return value.Clamp(0, 255);
        }

        public static float Clamp0_255(this float value)
        {
            return value.Clamp(0, 255);
        }

        public static double Clamp0_255(this double value)
        {
            return value.Clamp(0, 255);
        }

        public static int Clamp0_65535(this int value)
        {
            return value.Clamp(0, 65535);
        }

        //http://stackoverflow.com/a/2683487/758666
        public static T Clamp<T>(this T val, T min, T max) where T : IComparable<T>
        {
            if (val.CompareTo(min) < 0) return min;
            else if (val.CompareTo(max) > 0) return max;
            else return val;
        }


        public static double ANormalized(this Color color){ return (double)color.A / 255; }
        public static double RNormalized(this Color color){ return (double)color.R / 255; }
        public static double GNormalized(this Color color){ return (double)color.G / 255; }
        public static double BNormalized(this Color color){ return (double)color.B / 255; }

        /// <summary>
        /// Linearly interpolates over the value x between the points (xMin, yMin) and (xMax, yMax).
        /// </summary>
        public static double Lerp(
            double x,
            double xMin, double xMax,
            double yMin, double yMax)
        {
            double ratio = (x - xMin) / (xMax - xMin);
            return yMin + ratio * (yMax - yMin);
        }
    }
}
