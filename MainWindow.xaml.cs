using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
//using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Drawing;
using System.Windows.Interop;
using System.Diagnostics;
using System.Drawing.Imaging;

namespace CheckAlphaGradation
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        IntPtr[] imageBitmaps;
        IntPtr[] redCurveBitmaps;
        IntPtr[] greenCurveBitmaps;

        List<double[]> redCurves;
        List<double[]> greenCurves;

        int colorInfluence = 0;

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

        public MainWindow()
        {
            InitializeComponent();

            imageBitmaps = new IntPtr[101];
            redCurves = new List<double[]>();
            greenCurves = new List<double[]>();           
            List<double> redCurvesMins = new List<double>();
            List<double> greenCurvesMins = new List<double>();
            List<double> redCurvesMaxs = new List<double>();
            List<double> greenCurvesMaxs = new List<double>();
            for (int i = 0; i < 101; ++i)
            {
                String filepath = String.Format(
                    "F:\\myFolder\\alphaGradation_{0:D3}.png", i);
                Bitmap bitmap = LoadBitmap(filepath);
                BitmapInfo imageBitmapInfo = new BitmapInfo(bitmap);
                double[] redCurve = GetColorLevelCurve(imageBitmapInfo,
                    (color, a) => 
                    {
                        double r = color.RNormalized();
                        if (a == 1) 
                            a = 0;
                        //return r /(1-a);
                        return r;
                    } 
                    );
                double[] greenCurve = GetColorLevelCurve(imageBitmapInfo, 
                    (color, a) => color.GNormalized()
                    );
                imageBitmaps[i] = imageBitmapInfo.ToBitmap().GetHbitmap();
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


            redCurveBitmaps = new IntPtr[101];
            greenCurveBitmaps = new IntPtr[101];
            for (int i = 0; i < 101; ++i)
            {
                BitmapInfo redCurveBitmapInfo = GetColorLevelCurveImage(redCurves[i], redCurvesMin, redCurvesMax);
                redCurveBitmaps[i] = redCurveBitmapInfo.ToBitmap().GetHbitmap();
                BitmapInfo greenCurveBitmapInfo = GetColorLevelCurveImage(greenCurves[i], greenCurvesMin, greenCurvesMax);
                greenCurveBitmaps[i] = greenCurveBitmapInfo.ToBitmap().GetHbitmap();
            }

            SliderInfluence.Value = 100;
        }

        public delegate double ColorComponentExtractor(System.Drawing.Color color, double a);

        private double[] GetColorLevelCurve(BitmapInfo source, ColorComponentExtractor extractor)
        {
            // vertical Y value where image is sampled
            // TODO make it a parameter
            int Ysample = source.Height / 2;

            double[] CurveValues = new double[source.Width];
            for (int x = 0; x < source.Width; x++)
            {
                Color colorSource = source.GetPixelColor(x, Ysample);
                double alpha = (double)x / (source.Width - 1);
                double yVal = extractor(colorSource, alpha);
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


            BitmapInfo dest = new BitmapInfo(512, 512, PixelFormat.Format32bppArgb);
            for (int x = 0; x < dest.Width; x++)
            {
                int yVal = quantizedValues[x];

                // in the original image y is higher at the bottom
                // we invert y so that output curves are easier to understand
                for (int y = 0; y < dest.Height; ++y)
                {
                    if (y == yVal)
                        dest.SetPixelColor(x, (dest.Height - 1) - y, Color.Black);
                    else
                        dest.SetPixelColor(x, (dest.Height - 1) - y, Color.White);
                }
            }

            return dest;
        }

        private void ScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            double vert = ((ScrollViewer)sender).VerticalOffset;
            double hori = ((ScrollViewer)sender).HorizontalOffset;

            ScrollViewer[] scrollViewers = new ScrollViewer[]
            {
                ScrollViewerImage, 
                ScrollViewerCurveRed, 
                ScrollViewerCurveGreen
            };

            foreach (ScrollViewer scrollViewer in scrollViewers)
            {
                if (scrollViewer == null)
                    continue;
                scrollViewer.ScrollToVerticalOffset(vert);
                scrollViewer.ScrollToHorizontalOffset(hori);
                scrollViewer.UpdateLayout();
            }
        }

        private void SliderInfluence_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            int newColorInfluence = Convert.ToInt32(Math.Round(SliderInfluence.Value));

            if (newColorInfluence == colorInfluence)
                return;

            colorInfluence = newColorInfluence;
            LabelInfluence.Content = newColorInfluence;

            Image.Source =
                Imaging.CreateBitmapSourceFromHBitmap(
                    imageBitmaps[colorInfluence], IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());

            ImageCurveRed.Source =
                Imaging.CreateBitmapSourceFromHBitmap(
                    redCurveBitmaps[colorInfluence], IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());

            ImageCurveGreen.Source =
                Imaging.CreateBitmapSourceFromHBitmap(
                    greenCurveBitmaps[colorInfluence], IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
        }


        private void MyCatch(System.Exception ex)
        {
            var st = new StackTrace(ex, true);      // stack trace for the exception with source file information
            var frame = st.GetFrame(0);             // top stack frame
            String sourceMsg = String.Format("{0}({1})", frame.GetFileName(), frame.GetFileLineNumber());
            Console.WriteLine(sourceMsg);
            MessageBox.Show(ex.Message + Environment.NewLine + sourceMsg);
            Debugger.Break();
        }


    }
}
