using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace CheckAlphaGradation
{
    internal class DataLine
    {
        public BitmapInfo[] imageBitmapInfos;

        public IntPtr[] imageBitmaps;
        public IntPtr[] redCurveBitmaps;
        public IntPtr[] greenCurveBitmaps;

        static int samplesCount = 26; // MAX = 101 (all samples)
        static List<int> sampleIds; // these IDs match with the value of influence in the underlying exported file


        static DataLine()
        {
            // build sample ids table
            sampleIds = new List<int>();
            for (int i = 0; i < samplesCount; ++i)
            {
                double influence_d = Helpers.Lerp(
                    i, 0, samplesCount - 1,
                    0, 100);
                int influence_i = Convert.ToInt32(Math.Round(influence_d));
                sampleIds.Add(influence_i);
            }
        }

        /// <summary>
        /// Finds index of sample whose influence value is closest to the one passed in parameter
        /// </summary>
        /// <param name="influence">[0,1] value (from UI)</param>
        public static int GetSampleIndex(double influence)
        {
            // Aggregate: process consecutive list items one by one (with accumulator)
            int closest = sampleIds.Aggregate(
                (x,y) => Math.Abs(x-influence) < Math.Abs(y-influence) ? x : y);
            return sampleIds.IndexOf(closest);
        }

        public DataLine()
        {
            imageBitmaps = new IntPtr[samplesCount];
            imageBitmapInfos = new BitmapInfo[samplesCount];    
        }

        public void SetImages(System.Windows.Controls.Image[] images, int colorInfluenceIndex)
        {
            images[0].Source =
                Imaging.CreateBitmapSourceFromHBitmap(
                    imageBitmaps[colorInfluenceIndex], IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());

            images[1].Source =
                Imaging.CreateBitmapSourceFromHBitmap(
                    redCurveBitmaps[colorInfluenceIndex], IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());

            images[2].Source =
                Imaging.CreateBitmapSourceFromHBitmap(
                    greenCurveBitmaps[colorInfluenceIndex], IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());

        }



        void ComputeRedGreenCurves()
        {
            List<double[]> redCurves = new List<double[]>();
            List<double[]> greenCurves = new List<double[]>();  
            List<double> redCurvesMins = new List<double>();
            List<double> greenCurvesMins = new List<double>();
            List<double> redCurvesMaxs = new List<double>();
            List<double> greenCurvesMaxs = new List<double>();

            for (int i = 0; i < samplesCount; ++i)
            {
                double influence_d = Helpers.Lerp(
                    i, 0, samplesCount - 1,
                    0, 1);

                double[] redCurve = GetColorLevelCurve(imageBitmapInfos[i], influence_d,
                    (color, a, c) =>
                    {
                        //return a - Math.Pow(a, 1 + 16 * c); // <- GNormalized() equals this (BLACK)
                        //return 1 - Math.Pow(a, 1 + 16 * c); // <- GNormalized() looks like this (WHITE)

                        double r = color.RNormalized();
                        return r;
                    }
                    );
                double[] greenCurve = GetColorLevelCurve(imageBitmapInfos[i], influence_d,
                    (color, a, c) =>
                    {
                        //return Math.Pow(a, 1 + 16*c);   // <- RNormalized() equals this (BLACK)
                        // ?????      <- RNormalized() equals this (WHITE)

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

            redCurveBitmaps = new IntPtr[samplesCount];
            greenCurveBitmaps = new IntPtr[samplesCount];
            try
            {
                for (int i = 0; i < samplesCount; ++i)
                {
                    BitmapInfo redCurveBitmapInfo = GetColorLevelCurveImage(redCurves[i], redCurvesMin, redCurvesMax);
                    redCurveBitmaps[i] = ConvertBitmapInfo(redCurveBitmapInfo);
                    BitmapInfo greenCurveBitmapInfo = GetColorLevelCurveImage(greenCurves[i], greenCurvesMin, greenCurvesMax);
                    greenCurveBitmaps[i] = ConvertBitmapInfo(greenCurveBitmapInfo);
                }
            }
            catch (System.Exception ex)
            {
                Helpers.MyCatch(ex);
            }
        }

        void ConvertImageBitmapInfos()
        {
            for (int i = 0; i < samplesCount; ++i)
            {
                imageBitmaps[i] = ConvertBitmapInfo(imageBitmapInfos[i]);
            }
        }

        IntPtr ConvertBitmapInfo(BitmapInfo bitmapInfo)
        {
            return bitmapInfo.ToBitmap().GetHbitmap();
        }


        public void LoadFromFolder(String folderPath)
        {
            for (int i = 0; i < samplesCount; ++i)
            {
                String filepath = String.Format("{0}alphaGradation_{1:D3}.png", folderPath, sampleIds[i]);
                Bitmap bitmap = LoadBitmap(filepath);
                imageBitmapInfos[i] = new BitmapInfo(bitmap);
            }
            ConvertImageBitmapInfos();
            ComputeRedGreenCurves();
        }




        public void BuildFromFormulaBlack()
        {
            for (int i = 0; i < samplesCount; ++i)
            {
                imageBitmapInfos[i] = GetRebuiltImageBlack(/*imageBitmapInfos[0],*/ (double)i / 100);
            }
            ConvertImageBitmapInfos();
            ComputeRedGreenCurves();
        }



        public void BuildFromFormulaWhite()
        {
            for (int i = 0; i < samplesCount; ++i)
            {
                imageBitmapInfos[i] = GetRebuiltImageWhite(/*imageBitmapInfos[0],*/ (double)i / 100);
            }
            ConvertImageBitmapInfos();
            ComputeRedGreenCurves();
        }




        private BitmapInfo GetRebuiltImageBlack(/*BitmapInfo source,*/ double colorInfluence)
        {
            BitmapInfo dest = new BitmapInfo(512, 512, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            var backgroundColorWhite = System.Drawing.Color.FromArgb(255, 255, 255, 255);
            var backgroundColorBlack = System.Drawing.Color.FromArgb(255, 0, 0, 0);

            var shadowColor = System.Drawing.Color.FromArgb(255, 255, 0, 0);
            var layerColor = System.Drawing.Color.FromArgb(255, 0, 255, 0);

            // CASE-1: BLACK BACKGROUND
            for (int x = 0; x < dest.Width; x++)
            {
                double layerAlpha = Helpers.Lerp(x, 0, 511, 0, 1);   // hard coded value for alpha
                double mysticRatio = Math.Pow(layerAlpha, 1 + 16 * colorInfluence);

                double r = mysticRatio;
                double g = layerAlpha - mysticRatio;
                double b = 0;

                Color finalColor = Color.FromArgb(255, r.ScaleToByte(), g.ScaleToByte(), b.ScaleToByte());

                for (int y = 0; y < dest.Height; ++y)
                {
                    dest.SetPixelColor(x, y, finalColor);
                }
            }

            return dest;
        }

        private BitmapInfo GetRebuiltImageWhite(/*BitmapInfo source,*/ double colorInfluence)
        {
            BitmapInfo dest = new BitmapInfo(512, 512, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            var backgroundColorWhite = System.Drawing.Color.FromArgb(255, 255, 255, 255);
            var backgroundColorBlack = System.Drawing.Color.FromArgb(255, 0, 0, 0);

            var shadowColor = System.Drawing.Color.FromArgb(255, 255, 0, 0);
            var layerColor = System.Drawing.Color.FromArgb(255, 0, 255, 0);

            // CASE-2: WHITE BACKGROUND
            for (int x = 0; x < dest.Width; x++)
            {
                double layerAlpha = Helpers.Lerp(x, 0, 511, 0, 1);   // hard coded value for alpha
                double mysticRatio = Math.Pow(layerAlpha, 1 + 16 * colorInfluence);

                //double r = layerColorRatio;
                double r = 1 - (layerAlpha - mysticRatio);
                double g = 1 - mysticRatio;
                double b = 1;

                Color finalColor = Color.FromArgb(255, r.ScaleToByte(), g.ScaleToByte(), b.ScaleToByte());

                for (int y = 0; y < dest.Height; ++y)
                {
                    dest.SetPixelColor(x, y, finalColor);
                }
            }

            return dest;
        }


        private BitmapInfo GetRebuiltImageFinal(/*BitmapInfo source,*/ double colorInfluence)
        {
            BitmapInfo dest = new BitmapInfo(512, 512, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            var backgroundColorWhite = System.Drawing.Color.FromArgb(255, 255, 255, 255);
            var backgroundColorBlack = System.Drawing.Color.FromArgb(255, 0, 0, 0);

            var shadowColor = System.Drawing.Color.FromArgb(255, 255, 0, 0);
            var layerColor = System.Drawing.Color.FromArgb(255, 0, 255, 0);



            //// CASE-1: BLACK BACKGROUND
            //for (int x = 0; x < dest.Width; x++)
            //{
            //    double layerAlpha = Helpers.Lerp(x, 0, 511, 0, 1);   // hard coded value for alpha

            //    double solidColorRatio = Math.Pow(layerAlpha, 1 + 16 * colorInfluence);
            //    double glassEdgeRatio = layerAlpha - solidColorRatio;

            //    Color finalShadowColor = Helpers.Weight(solidColorRatio, shadowColor, glassEdgeRatio, layerColor);

            //    // TODO later: blend with bkg
            //    Color finalColor = Helpers.Lerp(backgroundColorBlack, finalShadowColor, layerAlpha);

            //    for (int y = 0; y < dest.Height; ++y)
            //    {
            //        dest.SetPixelColor(x, y, finalColor);
            //    }
            //}



            // CASE-2: WHITE BACKGROUND


            // CASE-3: FORMULA COMPLYING WITH BOTH

            // TODO !!!

            //for (int x = 0; x < dest.Width; x++)
            //{
            //    double layerAlpha = Helpers.Lerp(x, 0, 511, 0, 1);   // hard coded value for alpha

            //    double glassEdgeRatio = layerAlpha - Math.Pow(layerAlpha, 1 + 16 * colorInfluence);

            //    var finalShadowColor = Helpers.Lerp(shadowColor, layerColor, glassEdgeRatio);
            //    finalShadowColor = System.Drawing.Color.FromArgb(
            //        Convert.ToByte((255 * layerAlpha).Clamp0_255()), finalShadowColor.R, finalShadowColor.G, finalShadowColor.B);

            //    var finalColor = Helpers.Lerp(backgroundColor, finalShadowColor, layerAlpha);


            //    // TODO blend with bkg

            //    for (int y = 0; y < dest.Height; ++y)
            //    {
            //        dest.SetPixelColor(x, y, finalColor);
            //    }
            //}

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
