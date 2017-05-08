using Emgu.CV;
using Emgu.CV.Structure;
using Microsoft.Win32;
using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Homography
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Original Image from file
        private Image<Bgr, byte> OriginalImage;

        // Buffers to work with
        private Image<Bgr, byte> OriginalBuffer;
        private Image<Bgr, byte> ResultBuffer;
        public MainWindow()
        {
            InitializeComponent();

            // Handle rectangle changes
            canvasOrig.RecatangleChanged += RenderHomography;
            canvasRes.RecatangleChanged += RenderHomography;
        }

        /// <summary>
        /// Handler to compute homography
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RenderHomography(object sender, EventArgs e)
        {
            if (OriginalBuffer == null || OriginalBuffer.Mat.IsEmpty) return;
            ResultBuffer = HomographyLogic.Render(OriginalBuffer, canvasOrig.Points, canvasRes.Points, new System.Drawing.Size(0, 0), Emgu.CV.CvEnum.Inter.Area);

            // Convert to ImageSource
            imResult.Source = ToBitmapSource(ResultBuffer);
        }

        /// <summary>
        /// Loads an image from file
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnLoadFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Image files (*.jpg, *.jpeg, *.jpe, *.jfif, *.png) | *.jpg; *.jpeg; *.jpe; *.jfif; *.png|All files (*.*)|*.*";
            if (ofd.ShowDialog() == true)
            {
                try
                {
                    OriginalImage = new Image<Bgr, byte>(ofd.FileName);
                    UpdateGui();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: Could not read image file: " + ex.Message);
                }
            }
        }

        // Import GDI32
        [DllImport("gdi32")]
        private static extern int DeleteObject(IntPtr o);

        /// <summary>
        /// Image to bitmap Source converter
        /// </summary>
        /// <param name="image"> image to convert </param>
        /// <returns> bitmap source </returns>
        public static BitmapSource ToBitmapSource(Image<Bgr, byte> image)
        {
            using (System.Drawing.Bitmap source = image.Bitmap)
            {
                IntPtr ptr = source.GetHbitmap(); //obtain the Hbitmap

                BitmapSource bs = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                    ptr,
                    IntPtr.Zero,
                    Int32Rect.Empty,
                    System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());

                DeleteObject(ptr); //release the HBitmap
                return bs;
            }
        }

        /// <summary>
        /// Updates the images after rescaling the form
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UpdateGui( object sender = null, SizeChangedEventArgs e = null)
        {
            if (OriginalImage == null || OriginalImage.Mat.IsEmpty) return;
            OriginalBuffer = OriginalImage.Resize((int)canvasOrig.ActualWidth, (int)canvasOrig.ActualHeight, Emgu.CV.CvEnum.Inter.Cubic, true);
            imOriginal.Source = ToBitmapSource(OriginalBuffer);
        }

        /// <summary>
        /// Rectifies the right rectangle
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnRectifyTarget_Click(object sender, RoutedEventArgs e)
        {
            canvasRes.Rectify();
        }
    }
}
