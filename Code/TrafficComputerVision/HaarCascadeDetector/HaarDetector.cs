using Emgu.CV;
using Emgu.CV.Structure;
using System;
using System.Diagnostics;
using System.Drawing;

namespace HaarCascadeDetector
{
    public class HaarDetector
    {
        // Original image
        public Image<Bgr, byte> ImageOriginal;

        // Image with markers around traffic signs
        public Image<Bgr, byte> ImageAfterDetection;

        // Path to the cascade xml file
        private string cascadePath;

        // Path to the image
        private string imagePath;

        /// <summary>
        /// Path to the cascade
        /// </summary>
        public string CascadePath
        {
            get
            {
                return cascadePath;
            }

            set
            {
                cascadePath = value;
                if (!string.IsNullOrEmpty(value)) FindSignHaar();
            }
        }

        /// <summary>
        /// Path to the image
        /// </summary>
        public string ImagePath
        {
            get
            {
                return imagePath;
            }

            set
            {
                imagePath = value;
                ImageOriginal = new Image<Bgr, byte>(value).Resize(1000, 1000, Emgu.CV.CvEnum.Inter.Cubic, true);
                if (ImageOriginal != null) FindSignHaar();
            }
        }

        /// <summary>
        /// Contains time and hit info
        /// </summary>
        public string Info { get; private set; }

        /// <summary>
        /// Find traffic signs based on the Haar cascade file
        /// </summary>
        public void FindSignHaar()
        {
            // Do files exists?
            if (string.IsNullOrEmpty(cascadePath)) return;
            if (string.IsNullOrEmpty(imagePath)) return;

            // load and use cascade file
            using (CascadeClassifier signCas = new CascadeClassifier(cascadePath))
            {
                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();

                // Convert to grayscale and equalize histogram
                Image<Gray, byte> graySrc = ImageOriginal.Convert<Gray, byte>();
                graySrc._EqualizeHist();

                // Find BBs around traffic signs
                Rectangle[] signsDetected = signCas.DetectMultiScale(graySrc, 1.1, 10, new System.Drawing.Size(20, 20));
                stopWatch.Stop();

                // Draw markers
                ImageAfterDetection = ImageOriginal.Clone();
                foreach (Rectangle sign in signsDetected)
                {
                    ImageAfterDetection.Draw(sign, new Bgr(0, 0, 255), 3);
                }

                // Generate basic info
                Info = "Hits: " + signsDetected.Length + "  Time: " + stopWatch.ElapsedMilliseconds;
            }
            
        }
    }
}
