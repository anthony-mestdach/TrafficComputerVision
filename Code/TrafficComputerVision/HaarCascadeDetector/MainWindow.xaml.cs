using Emgu.CV;
using Emgu.CV.Structure;
using Microsoft.Win32;
using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media.Imaging;


namespace HaarCascadeDetector
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Haar cascade based detector
        private HaarDetector detector;
        public MainWindow()
        {
            InitializeComponent();
            btnLoadImage.IsEnabled = false;
            detector = new HaarDetector();
        }

        /// <summary>
        /// Load image
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnLoadImage_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Image files (*.jpg, *.jpeg, *.jpe, *.jfif, *.png) | *.jpg; *.jpeg; *.jpe; *.jfif; *.png|All files (*.*)|*.*";
            if (ofd.ShowDialog() == true)
            {
                try
                {
                    detector.ImagePath = ofd.FileName;
                    if (detector.ImageAfterDetection != null) imgResult.Source = ToBitmapSource(detector.ImageAfterDetection);
                    this.Title = detector.Info;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: Could not read image file: " + ex.Message);
                }
            } 
        }

        /// <summary>
        /// Load cascade
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnLoadCascade_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Vec files (*.xml) | *.xml;|All files (*.*)|*.*";
            if (ofd.ShowDialog() == true)
            {
                try
                {
                    detector.CascadePath = ofd.FileName;
                    btnLoadImage.IsEnabled = true;
                    if (detector.ImageAfterDetection != null) imgResult.Source = ToBitmapSource(detector.ImageAfterDetection);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: Could not read cascade file: " + ex.Message);
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
    }
}
