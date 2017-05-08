using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Emgu.CV;
using Emgu.CV.Structure;
using System.Diagnostics;

namespace TrafficSignRec
{
    /// <summary>
    /// Form configuration for Traffic Sign Recognition
    /// </summary>
    public partial class TrafficSignRecognition : Form
    {
        // Detector for andidate traffic signs
        private CandidateDetector detector;

        // Matcher for traffic signs
        private SignMatcher matcher;
        public TrafficSignRecognition()
        {
            InitializeComponent();
            this.Text = "Traffic Sign Recognition";
            detector = new CandidateDetector(5);
            matcher = new SignMatcher();
        }

        /// <summary>
        /// Load image
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnLoadImage_Click(object sender, EventArgs e)
        {
            try
            {
                // Create en open file dialog
                OpenFileDialog fileDia = new OpenFileDialog();
                fileDia.InitialDirectory = Environment.CurrentDirectory;
                fileDia.Filter = "JPEG Files (*.jpeg)|*.jpeg|PNG Files (*.png)|*.png|JPG Files (*.jpg)|*.jpg|GIF Files (*.gif)|*.gif";
                if (fileDia.ShowDialog() == DialogResult.OK)
                {
                    Stopwatch stopWatch = new Stopwatch();
                    stopWatch.Start();
                    Image<Bgr, byte> src = new Image<Bgr, byte>(fileDia.FileName);
                    detector.FindCandidates(src);
                    matcher.ReadKnownSigns();
                    matcher.Candidates = detector.Candidates;
                    matcher.MatchSigns(matcher.Candidates, matcher.KnownSigns);
                    imageBox1.Image = DrawSigns(src, matcher.Matches, detector.Candidates);
                    stopWatch.Stop();
                    this.Text = "Hits: " + matcher.Matches + "  Time: " + stopWatch.ElapsedMilliseconds;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "File error");
            }
        }

        /// <summary>
        /// Draws the recognised signs
        /// </summary>
        /// <param name="src"> original image to draw on </param>
        /// <param name="matches"> traffic sign matches </param>
        /// <param name="rawCandidates"> raw candidates </param>
        /// <returns> image with signs </returns>
        private Image<Bgr, byte> DrawSigns(Image<Bgr, byte> src, List<TrafficSignMatch> matches, List<TrafficSign> rawCandidates)
        {
            Image<Bgr, byte> result = src.Copy();
            rawCandidates.ForEach(candidate => 
            {
                // Draw candidates
                result.Draw(candidate.BoundingBoxInScene, new Bgr(255, 20, 20), 1);
            });
            matches.ForEach(match => 
            {
                // Draw matches bounding boxes
                result.Draw(match.Candidate.BoundingBoxInScene, new Bgr(50, 255, 50), 3);
                Image<Bgr, byte> signToDraw = match.KnownSign.ImageOriginal.Clone();
                Emgu.CV.CvEnum.Inter inter = match.Candidate.BoundingBoxInScene.Width > signToDraw.Width ? Emgu.CV.CvEnum.Inter.Area : Emgu.CV.CvEnum.Inter.Cubic;
                signToDraw = signToDraw.Resize(match.Candidate.BoundingBoxInScene.Width, int.MaxValue, inter, true);

                if (match.Candidate.BoundingBoxInScene.Bottom + match.KnownSign.ImageOriginal.Height > src.Height)
                {
                    // Draw sign above candidate
                    Point p = match.Candidate.BoundingBoxInScene.Location;
                    p.Offset(0, -signToDraw.Size.Height);
                    result.ROI = new Rectangle(p, signToDraw.Size);
                }
                else
                {
                    // Draw sign below candidate
                    Point p = match.Candidate.BoundingBoxInScene.Location;
                    p.Offset(0, match.Candidate.BoundingBoxInScene.Size.Height);
                    result.ROI = new Rectangle(p, signToDraw.Size);
                }
                signToDraw.CopyTo(result);
                result.ROI = Rectangle.Empty;
            });
            return result;
        }
    }
}
