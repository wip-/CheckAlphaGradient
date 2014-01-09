using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
//using System.Windows.Interop;

using System.Windows;
using System.Windows.Documents;
//using System.Drawing;
using System.Windows.Interop;
//using System.Windows.Media;
using System.Windows.Media.Imaging;
//using System.Drawing.Imaging;

namespace CheckAlphaGradation
{
    internal class DataLine
    {
        public BitmapInfo[] imageBitmapInfos;
        public IntPtr[] imageBitmaps;

        public IntPtr[] redCurveBitmaps;
        public IntPtr[] greenCurveBitmaps;

        List<double[]> redCurves;
        List<double[]> greenCurves;

        public DataLine()
        {
            imageBitmaps = new IntPtr[101];
            imageBitmapInfos = new BitmapInfo[101];
            redCurves = new List<double[]>();
            greenCurves = new List<double[]>();      
        }

        public void SetImages(System.Windows.Controls.Image[] images, int colorInfluence)
        {
            images[0].Source =
                Imaging.CreateBitmapSourceFromHBitmap(
                    imageBitmaps[colorInfluence], IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());

            images[1].Source =
                Imaging.CreateBitmapSourceFromHBitmap(
                    redCurveBitmaps[colorInfluence], IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());

            images[2].Source =
                Imaging.CreateBitmapSourceFromHBitmap(
                    greenCurveBitmaps[colorInfluence], IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());

        }



        void ComputeRedGreenCurves()
        {
            List<double> redCurvesMins = new List<double>();
            List<double> greenCurvesMins = new List<double>();
            List<double> redCurvesMaxs = new List<double>();
            List<double> greenCurvesMaxs = new List<double>();
            for (int i = 0; i < 101; ++i)
            {
                double[] redCurve = GetColorLevelCurve(imageBitmapInfos[i], (double)i / 100,
                    (color, a, c) =>
                    {
                        //return a - Math.Pow(a, 1 + 16 * c); // <- GNormalized() equals this (BLACK)
                        //return 1 - Math.Pow(a, 1 + 16 * c); // <- GNormalized() looks like this (WHITE)

                        double r = color.RNormalized();
                        return r;
                    }
                    );
                double[] greenCurve = GetColorLevelCurve(imageBitmapInfos[i], (double)i / 100,
                    (color, a, c) =>
                    {
                        //return Math.Pow(a, 1 + 16*c);   // <- RNormalized() equals this (BLACK)

                        double g = color.GNormalized();
                        return g;

                        double r = color.RNormalized();
                        double g2 = 2 - r - a;  // this proves that g channel is equal to (2-r-a) ON WHITE BACKGROUND (r(0) = 1)
                        double g3 = 0 - r + a;  // TODO CHECK IF THIS ALSO WORKS ON WHITE BACKGROUND                  (r(0) = 0)
                    }
                    );
                redCurves.Add(redCurve);
                greenCurves.Add(greenCurve);
                redCurvesMins.Add(redCurve.Min());
                redCurvesMaxs.Add(redCurve.Max());
                greenCurvesMins.Add(greenCurve.Min());
                greenCurvesMaxs.Add(greenCurve.Max());
            }

            double redCurvesMin = redCurvesMins.Min();
            double redCurvesMax = redCurvesMaxs.Max();
            double greenCurvesMin = greenCurvesMins.Min();
            double greenCurvesMax = greenCurvesMaxs.Max();

            // red and green use same scale
            if (true)
            {
                double rgMin = Math.Min(redCurvesMin, greenCurvesMin);
                double rgMax = Math.Max(redCurvesMax, greenCurvesMax);
                redCurvesMin = rgMin;
                redCurvesMax = rgMax;
                greenCurvesMin = rgMin;
                greenCurvesMax = rgMax;
            }

            redCurveBitmaps = new IntPtr[101];
            greenCurveBitmaps = new IntPtr[101];
            try
            {
                for (int i = 0; i < 101; ++i)
                {
                    //if (i == 17 || i == 18)
                    //    Debugger.Break();
                    BitmapInfo redCurveBitmapInfo = GetColorLevelCurveImage(redCurves[i], redCurvesMin, redCurvesMax);
                    redCurveBitmaps[i] = redCurveBitmapInfo.ToBitmap().GetHbitmap();
                    BitmapInfo greenCurveBitmapInfo = GetColorLevelCurveImage(greenCurves[i], greenCurvesMin, greenCurvesMax);
                    greenCurveBitmaps[i] = greenCurveBitmapInfo.ToBitmap().GetHbitmap();
                }
            }
            catch (System.Exception ex)
            {
                Helpers.MyCatch(ex);
            }
        }


        public void LoadFromFolder(String folderPath)
        {
            for (int i = 0; i < 101; ++i)
            {
                String filepath = String.Format("{0}alphaGradation_{1:D3}.png", folderPath, i);
                Bitmap bitmap = LoadBitmap(filepath);
                BitmapInfo imageBitmapInfo = new BitmapInfo(bitmap);
                imageBitmapInfos[i] = imageBitmapInfo;
                imageBitmaps[i] = imageBitmapInfo.ToBitmap().GetHbitmap();
            }

            ComputeRedGreenCurves();
        }




        public void BuildFromFormula()
        {
            for (int i = 0; i < 101; ++i)
            {
                imageBitmapInfos[i] = GetRebuiltImage(/*imageBitmapInfos[0],*/ (double)i / 100);
                imageBitmaps[i] = imageBitmapInfos[i].ToBitmap().GetHbitmap();
            }

            ComputeRedGreenCurves();
        }








        private BitmapInfo GetRebuiltImage(/*BitmapInfo source,*/ double colorInfluence)
        {
            BitmapInfo dest = new BitmapInfo(512, 512, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            var backgroundColor = System.Drawing.Color.FromArgb(255, 255, 255, 255);
            //var backgroundColor = System.Drawing.Color.FromArgb(255, 0, 0, 0);

            var shadowColor = System.Drawing.Color.FromArgb(255, 255, 0, 0);
            var layerColor = System.Drawing.Color.FromArgb(255, 0, 255, 0);

            //if (colorInfluence == 1)
            //    Debugger.Break();

            for (int x = 0; x < dest.Width; x++)
            {
                double layerAlpha = Helpers.Lerp(x, 0, 511, 0, 1);   // hard coded value for alpha

                double glassEdgeRatio = layerAlpha - Math.Pow(layerAlpha, 1 + 16 * colorInfluence);
                var finalShadowColor = Helpers.Lerp(shadowColor, layerColor, glassEdgeRatio);
                finalShadowColor = System.Drawing.Color.FromArgb(
                    Convert.ToByte((255 * layerAlpha).Clamp0_255()), finalShadowColor.R, finalShadowColor.G, finalShadowColor.B);

                var finalColor = Helpers.Lerp(backgroundColor, finalShadowColor, layerAlpha);


                // TODO blend with bkg

                for (int y = 0; y < dest.Height; ++y)
                {
                    dest.SetPixelColor(x, y, finalColor);
                }
            }

            return dest;
        }










        public delegate double ColorComponentExtractor(System.Drawing.Color color, double a, double i);

        private double[] GetColorLevelCurve(BitmapInfo source, double colorInfluence, ColorComponentExtractor extractor)
        {
            // vertical Y value where image is sampled
            // TODO make it a parameter
            int Ysample = source.Height / 2;

            double[] CurveValues = new double[source.Width];
            for (int x = 0; x < source.Width; x++)
            {
                var colorSource = source.GetPixelColor(x, Ysample);
                double alpha = (double)x / (source.Width - 1);
                double yVal = extractor(colorSource, alpha, colorInfluence);
                CurveValues[x] = yVal;
            }

            return CurveValues;
        }

        private BitmapInfo GetColorLevelCurveImage(double[] curveValues, double curvesMin, double curvesMax)
        {
            int[] quantizedValues = new int[curveValues.Length];
            for (int i = 0; i < curveValues.Length; i++)
            {
                double scaledValue = Helpers.Lerp(
                    curveValues[i], curvesMin, curvesMax,
                    0, 511);
                quantizedValues[i] = Convert.ToInt32(scaledValue.Clamp(0, 511));
            }


            BitmapInfo dest = new BitmapInfo(512, 512, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            for (int x = 0; x < dest.Width; x++)
            {
                int yVal = quantizedValues[x];

                // in the original image y is higher at the bottom
                // we invert y so that output curves are easier to understand
                for (int y = 0; y < dest.Height; ++y)
                {
                    if (y == yVal)
                        dest.SetPixelColor(x, (dest.Height - 1) - y, System.Drawing.Color.Black);
                    else
                        dest.SetPixelColor(x, (dest.Height - 1) - y, System.Drawing.Color.White);
                }
            }

            return dest;
        }

        private static Bitmap LoadBitmap(String filename)
        {
            FileStream fs = null;
            try
            {
                fs = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.None);
            }
            catch (IOException)
            {
                if (fs != null)
                    fs.Close();
                return null;
            }

            Bitmap bitmap;
            bitmap = new Bitmap(fs);
            return bitmap;
        }

    }
}
