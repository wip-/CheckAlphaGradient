using System;
//using System.Windows.Media;
//using System.Drawing;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
//using System.Drawing.Imaging;
using System.Windows.Media;

namespace CheckAlphaGradation
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        DataLine blackBkgImages = new DataLine();
        DataLine whiteBkgImages = new DataLine();
        //DataLine rebuiltImages = new DataLine();
        DataLine rebuiltImages;


        int colorInfluence = 0;

        public MainWindow()
        {
            InitializeComponent();

            whiteBkgImages.LoadFromFolder(
                "F:\\myFolder1\\");

            blackBkgImages.LoadFromFolder(
                "F:\\myFolder2\\");

            if(rebuiltImages!=null)
                rebuiltImages.BuildFromFormula();

            SliderInfluence.Value = 100;
            Zoom(0.65);
        }


        private void ScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            double vert = ((ScrollViewer)sender).VerticalOffset;
            double hori = ((ScrollViewer)sender).HorizontalOffset;

            ScrollViewer[] scrollViewers = new ScrollViewer[]
            {
                ScrollViewerImageBlack, ScrollViewerImageBlackCurveRed, ScrollViewerImageBlackCurveGreen,
                ScrollViewerImageWhite, ScrollViewerImageWhiteCurveRed, ScrollViewerImageWhiteCurveGreen,
                ScrollViewerImageRebuilt, ScrollViewerImageRebuiltCurveRed, ScrollViewerImageRebuiltCurveGreen
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

            blackBkgImages.SetImages(new Image[] { ImageBlack, ImageBlackCurveRed, ImageBlackCurveGreen }, newColorInfluence);
            whiteBkgImages.SetImages(new Image[] { ImageWhite, ImageWhiteCurveRed, ImageWhiteCurveGreen }, newColorInfluence);
            if (rebuiltImages != null)
                rebuiltImages.SetImages(new Image[] { ImageRebuilt, ImageRebuiltCurveRed, ImageRebuiltCurveGreen }, newColorInfluence);
        }



        private void ImageBlack_MouseMove(object sender, MouseEventArgs e)
        {
            Image_MouseMove(ImageBlack, blackBkgImages.imageBitmapInfos, sender, e);
        }

        private void ImageWhite_MouseMove(object sender, MouseEventArgs e)
        {
            Image_MouseMove(ImageWhite, whiteBkgImages.imageBitmapInfos, sender, e);
        }

        private void ImageRebuilt_MouseMove(object sender, MouseEventArgs e)
        {
            if (rebuiltImages != null)
                Image_MouseMove(ImageRebuilt, rebuiltImages.imageBitmapInfos, sender, e);
        }

        private void Image_MouseMove(Image image, BitmapInfo[] imageBitmapInfos, object sender, MouseEventArgs e)
        {
            int x = (int)(e.GetPosition(image).X);
            int y = (int)(e.GetPosition(image).Y);
            var color = imageBitmapInfos[colorInfluence].GetPixelColor(x, y);
            LabelInfo.Content = String.Format("X={0:D4}, Y={1:D4}, A={2:D3}, R={3:D3}, G={4:D3}, B={5:D3}",
                x, y, color.A, color.R, color.G, color.B);
        }

        private void SliderZoomOut_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (SliderZoomOut.Value != 1)
                Zoom(SliderZoomOut.Value);
            if (SliderZoomIn != null)
                SliderZoomIn.Value = 1;
        }

        private void SliderZoomIn_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (SliderZoomIn.Value != 1)
                Zoom(SliderZoomIn.Value);
            if (SliderZoomOut != null)
                SliderZoomOut.Value = 1;
        }

        private void ButtonResetZoom_Click(object sender, RoutedEventArgs e)
        {
            Zoom(1);
            SliderZoomIn.Value = 1;
            SliderZoomOut.Value = 1;
        }

        private double GetCurrentZoom()
        {
            if (SliderZoomIn.Value != 1)
                return SliderZoomIn.Value;
            if (SliderZoomOut.Value != 1)
                return SliderZoomIn.Value;
            return 1;
        }

        private void Zoom(double val)
        {
            try
            {
                ScaleTransform myScaleTransform = new ScaleTransform();
                myScaleTransform.ScaleY = val;
                myScaleTransform.ScaleX = val;
                if (LabelZoom != null)
                    LabelZoom.Content = val;
                TransformGroup myTransformGroup = new TransformGroup();
                myTransformGroup.Children.Add(myScaleTransform);

                System.Windows.Controls.Image[] images = new System.Windows.Controls.Image[] 
                { 
                    ImageBlack, ImageBlackCurveRed, ImageBlackCurveGreen,
                    ImageWhite, ImageWhiteCurveRed, ImageWhiteCurveGreen,
                    ImageRebuilt, ImageRebuiltCurveRed, ImageRebuiltCurveGreen,
                    
                };

                foreach (System.Windows.Controls.Image image in images)
                {
                    if (image == null || image.Source == null)
                        continue;
                    //image.RenderTransform = myTransformGroup;
                    image.LayoutTransform = myTransformGroup;
                }
            }
            catch (System.Exception ex)
            {
                Helpers.MyCatch(ex);
            }
        }






    }
}
