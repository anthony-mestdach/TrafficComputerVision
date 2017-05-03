using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
using System.IO;

namespace LaneDetection
{
    public partial class LaneDetection : Form, IDisposable
    {
        // Original image
        private Image<Bgr, byte> original;

        // Video Capture to load video file
        private VideoCapture capture;

        // Lane markings filter
        private LaneMarkingsFilter laneMarkFilter = new LaneMarkingsFilter();

        // Perspective transform logic
        private PerspectiveTransformer transformer = new PerspectiveTransformer();

        // Lane detection logic
        private LaneDetector detector = new LaneDetector();

        // Location of video file
        private string MediaFile;

        public LaneDetection()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Video loop, parses 1 frame every tick.
        /// </summary>
        /// <param name="sender"> event sender </param>
        /// <param name="e"> args </param>
        private void LoopVideo(object sender, EventArgs e)
        {
            // Get frame
            Mat buffer = capture.QueryFrame();

            // Handle 'null' frame at EOF
            if (buffer == null || buffer.IsEmpty) {
                timer.Enabled = false;
                btnStartStop.Text = "Start";
                return;
            }

            // Filter image based on color to find markings
            original = buffer.ToImage<Bgr, byte>().Resize(1280, 720, Inter.Linear);
            Image<Gray, byte> bin = laneMarkFilter.FilterMarkings(original.Clone());
            Image<Gray, byte> birdsEyeView = new Image<Gray, byte>(bin.Size);

            // Generate bird eye view
            Mat a = new Mat();
            Mat b = new Mat();
            transformer.GetBirdEye(bin, out a, out b, out birdsEyeView);

            // Find markings location
            Image<Bgr, byte> birdsEyeWithBoxes = new Image<Bgr, byte>(birdsEyeView.Size);
            detector.FitLinesInSlidingWindows(birdsEyeView, out birdsEyeWithBoxes, 13);

            int size = detector.LeftPoints.Count;
            if (detector.LeftPoints.Count == detector.RightPoints.Count)
            {
                Point[] l = detector.ProjectPoints(b, detector.LeftPoints).Select(x => new Point((int)x.X, (int)x.Y)).ToArray();
                Point[] r = detector.ProjectPoints(b, detector.RightPoints).Select(x => new Point((int)x.X, (int)x.Y)).ToArray();
                Point[] center = new Point[size];
                for (int i = 0; i < size; i++)
                {
                    original.DrawPolyline(new Point[]{
                        l[i],
                        r[i]
                    }, false, new Bgr(10, 250, 10), 2);
                    //center[i] = new Point((r[i].X + l[i].X) / 2, l[i].Y);
                }
                original.DrawPolyline(l, false, new Bgr(10, 250, 10), 4);
                original.DrawPolyline(r, false, new Bgr(10, 250, 10), 4);
                //original.DrawPolyline(center, false, new Bgr(10, 50, 200), 4);
            }
            imageBox1.Image = original;
            ibBirdEye.Image = ResizeImg(detector.BirdEye, ibBirdEye.Width, Inter.Area);
        }

        /// <summary>
        /// Resizes the image but keeps the aspect ratio
        /// </summary>
        /// <param name="origin"> source image to resize </param>
        /// <param name="width"> new Width </param>
        /// <param name="method"> scale method </param>
        /// <returns> resized image </returns>
        public Image<Bgr, byte> ResizeImg(Image<Bgr, byte> origin, int width, Inter method)
        {
            float ratio = (float)origin.Width / origin.Height;
            int height = (int)(width / ratio);
            return origin.Resize(width, height, method);
        }

        /// <summary>
        /// Load a new file.
        /// </summary>
        /// <param name="sender"> sender </param>
        /// <param name="e"> args </param>
        private void btnLoadFile_Click(object sender, EventArgs e)
        {
            try
            {
                // Create en open file dialog
                OpenFileDialog fileDia = new OpenFileDialog();
                fileDia.InitialDirectory = Environment.CurrentDirectory;
                fileDia.Filter = "Video files (*.mp4)|*.mp4|All files (*.*)|*.*";
                if (fileDia.ShowDialog() == DialogResult.OK)
                {
                    capture = new VideoCapture(fileDia.FileName);
                    MediaFile = fileDia.FileName;
                    lblFileName.Text = Path.GetFileName(fileDia.FileName);
                    if (capture.QueryFrame() != null && capture.GetCaptureProperty(CapProp.Fps) > 0.0)
                    {
                        double fps = capture.GetCaptureProperty(CapProp.Fps);

                        // Reset capture & init. timer
                        capture = new VideoCapture(MediaFile);
                        timer.Interval = (int)(1000 / fps);
                        timer.Enabled = true;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "File error");
            }
        }

        /// <summary>
        /// Start / Stop analyzing media file
        /// </summary>
        /// <param name="sender"> sender </param>
        /// <param name="e"> args </param>
        private void btnStartStop_Click(object sender, EventArgs e)
        {
            // File path set?
            if (String.IsNullOrEmpty(MediaFile))
            {
                MessageBox.Show("Select a file to play.", "No file error");
                return;
            }

            // Toggle status
            if (timer.Enabled)
            {
                timer.Enabled = false;
                btnStartStop.Text = "Start";
            }
            else
            {
                timer.Enabled = true;
                btnStartStop.Text = "Stop";
            }
        }

        /// <summary>
        /// Reload media file
        /// </summary>
        /// <param name="sender"> sender </param>
        /// <param name="e"> args </param>
        private void btnRestart_Click(object sender, EventArgs e)
        {
            // File path set?
            if (String.IsNullOrEmpty(MediaFile)) return;
            try
            {
                // Reload & start
                capture = new VideoCapture(MediaFile);
                timer.Enabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "File error");
            }
            btnStartStop.Text = "Stop";
        }
    }
}
