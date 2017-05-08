using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Features2D;
using Emgu.CV.Flann;
using Emgu.CV.Structure;
using Emgu.CV.UI;
using Emgu.CV.Util;
using Emgu.CV.XFeatures2D;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace TrafficSignRec
{
    /// <summary>
    /// Tries to match candidate signs with known signs.
    /// </summary>
    public class SignMatcher
    {
        // Path to a folder containing the known signs.
        public string KnownSignsPath;

        /// <summary>
        /// List of known signs
        /// </summary>
        public List<TrafficSign> KnownSigns { get; private set; }

        // Feature detector & descriptor
        private SURF detector;

        // List of candidate signs
        private List<TrafficSign> candidates;

        // List of matches between known signs & candidates.
        // If a candidate doesn't match a known signs no match will be generated.
        public List<TrafficSignMatch> Matches { get; private set; }

        // List of known signs
        public List<TrafficSign> Candidates
        {
            get { return candidates; }
            set {
                candidates = value;
                // Iterate over candidates and compute descriptors if required
                foreach (TrafficSign candidate in candidates) {
                    if (candidate.Features == null || candidate.Features.IsEmpty)
                    {
                        try
                        {
                            UpdateDescriptors(candidate);
                        }
                        catch (Exception ex)
                        {
                            // Log problem
                            Console.WriteLine(ex.Message);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Generates a SignMatcher with default path
        /// </summary>
        public SignMatcher()
        {
            if (Directory.Exists(@"KnownSigns/")) KnownSignsPath = @"KnownSigns/";
            
            // Upright-SURF to speedup the process with extended descriptors
            detector = new SURF(500, 4, 3, true, true);
        }

        /// <summary>
        /// Generate a SignMatcher with custom path
        /// </summary>
        /// <param name="knownSignsPath"> Folder with known signs </param>
        public SignMatcher(string knownSignsPath)
        {
            KnownSignsPath = knownSignsPath;

            // Upright-SURF to speedup the process with extended descriptors
            detector = new SURF(500, 4, 3, true, true);
        }

        /// <summary>
        /// Detects the keypoints and computes the features for a sign 
        /// </summary>
        /// <param name="sign"> Sign to update </param>
        private void UpdateDescriptors(TrafficSign sign)
        {
            Mat desc = new Mat();
            VectorOfKeyPoint kp = new VectorOfKeyPoint();
            detector.DetectAndCompute(sign.ImageGray, null, kp, desc, false);
            sign.Features = desc;
            sign.KeyPoints = kp;
        }

        /// <summary>
        /// Reads known signs from folder
        /// </summary>
        public void ReadKnownSigns()
        {
            if (!Directory.Exists(KnownSignsPath)) throw new Exception("Cannot open folder: " + KnownSignsPath);
            KnownSigns = new List<TrafficSign>(); 
            foreach (string file in Directory.EnumerateFiles(KnownSignsPath))
            {
                try
                {
                    TrafficSign sign = new TrafficSign(new Image<Bgr, byte>(file), Path.GetFileName(file));
                    UpdateDescriptors(sign);
                    KnownSigns.Add(sign);
                } catch (Exception ex)
                {
                    // Log problem
                    Console.WriteLine(ex.Message);
                }
            }
        }

        /// <summary>
        /// Matches a list of known signs with a list of candidates
        /// </summary>
        /// <param name="candidates"> candidate signs </param>
        /// <param name="knownSigns"> known signs </param>
        /// <returns> matched signs </returns>
        public List<TrafficSignMatch> MatchSigns(List<TrafficSign> candidates, List<TrafficSign> knownSigns) {
            // Return empty list if parameters are unusable
            if (candidates == null || knownSigns == null || candidates.Count < 1 || knownSigns.Count < 1) return new List<TrafficSignMatch>();

            Matches = new List<TrafficSignMatch>();
            List<TrafficSignMatch> result = new List<TrafficSignMatch>();
            LinearIndexParams ip = new LinearIndexParams();
            SearchParams sp = new SearchParams();

            // Iterate over candidates
            foreach (TrafficSign candi in Candidates) {

                // best match and score 
                int bestScore = 0;
                TrafficSignMatch bestMatch = null;

                // Iterate over known signs
                foreach (TrafficSign knownsign in knownSigns) {
                    VectorOfVectorOfDMatch match = new VectorOfVectorOfDMatch();
                    
                    // Match the sign or log the problem
                    try
                    {
                        match = knownsign.MatchToOtherSign(candi);
                    } catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                    
                    Mat mask = new Mat(match.Size, 1, DepthType.Cv8U, 1);
                    mask.SetTo(new MCvScalar(255));

                    // Filter duplicate matches
                    Features2DToolbox.VoteForUniqueness(match, 0.8, mask);

                    // Compute the homography matrix
                    Mat homography = Features2DToolbox.GetHomographyMatrixFromMatchedFeatures(knownsign.KeyPoints, candi.KeyPoints, match, mask, 1.5);
                    
                    // Compute a score for the match
                    int score = ScoreMatch(homography, knownsign, candi);

                    // Current score is better than previous
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestMatch = new TrafficSignMatch(candi, knownsign, score, match, homography);
                        //ImageViewer viewer = new ImageViewer();
                        //viewer.Image = Draw(knownsign.ImageGray.Convert<Bgr, byte>(), candi.ImageGray.Convert<Bgr, byte>(), knownsign.KeyPoints, candi.KeyPoints, match);
                        //viewer.Text = "Match score: " + score;
                        //viewer.Show();
                    }
                }
                if (bestMatch != null && bestMatch.MatchScore > 0) Matches.Add(bestMatch);
            }
            return result;
        }

        /// <summary>
        /// Computes a score for a match, higher is better
        /// </summary>
        /// <param name="homography"> homography matrix </param>
        /// <param name="known"> known sign </param>
        /// <param name="candidate"> candidate sign </param>
        /// <returns> score of the match </returns>
        public int ScoreMatch(Mat homography, TrafficSign known, TrafficSign candidate)
        {
            // Default score
            int score = -1;
            if (homography == null) return 0;

            // Create rectangle and transorm based on homography
            Rectangle rect = new Rectangle(Point.Empty, known.ImageGray.Size);
            PointF[] pts = new PointF[]
            {
                  new PointF(rect.Left, rect.Top),
                  new PointF(rect.Right, rect.Top),
                  new PointF(rect.Right, rect.Bottom),
                  new PointF(rect.Left, rect.Bottom)
            };
            pts = CvInvoke.PerspectiveTransform(pts, homography);
            
            // Check transformed recatangle
            // Scene corners should form rectangular shape
            // Check relative positions left upper
            if ((pts[0].X > pts[1].X) || (pts[0].X > pts[2].X)) return 0;
            if ((pts[0].Y > pts[2].Y) || (pts[0].Y > pts[3].Y)) return 0;

            // Check relative positions right upper
            if ((pts[1].X < pts[0].X) || (pts[1].X < pts[3].X)) return 0;
            if ((pts[1].Y > pts[2].Y) || (pts[1].Y > pts[3].Y)) return 0;

            // Check relative positions right lower
            if ((pts[2].X < pts[0].X) || (pts[2].X < pts[3].X)) return 0;
            if ((pts[2].Y < pts[0].Y) || (pts[2].Y < pts[1].Y)) return 0;

            // Check relative positions left lower
            if ((pts[3].Y > pts[1].X) || (pts[3].X > pts[2].X)) return 0;
            if ((pts[3].Y < pts[0].Y) || (pts[3].Y < pts[1].Y)) return 0;

            score = 1;
            
            // Low score for strange shaped rectangles
            double dia1 = Math.Sqrt(Math.Pow(pts[2].X - pts[0].X, 2) + Math.Pow(pts[2].Y - pts[0].Y, 2));
            double dia2 = Math.Sqrt(Math.Pow(pts[1].X - pts[3].X, 2) + Math.Pow(pts[3].Y - pts[1].Y, 2));
            if (dia1 * dia2 < known.ImageGray.Size.Height / 5) return score;

            double edgeRatioMax = 0.2;
            if (Math.Abs(1.0 - (dia1 / dia2)) > edgeRatioMax) return score;
            score = 2;

            // Check based on template  matching
            Image<Gray, byte> perspective = new Image<Gray, byte>(known.ImageGray.Size);
            Image<Gray, byte> templateRes = new Image<Gray, byte>(known.ImageGray.Size);
            double maxValue = 0;
            Point maxLoc = new Point();
            double minValue = 0;
            Point minLoc = new Point();
            CvInvoke.WarpPerspective(known.ImageGray, perspective, homography, known.ImageGray.Size, Emgu.CV.CvEnum.Inter.Linear);
            CvInvoke.MatchTemplate(candidate.ImageGray, perspective, templateRes, TemplateMatchingType.Ccoeff, null);
            CvInvoke.MinMaxLoc(templateRes, ref minValue, ref maxValue, ref minLoc, ref maxLoc);

            return score + (int)maxValue;
        }

        /// <summary>
        /// Draws the matches and homography 
        /// </summary>
        /// <param name="knownSign"> known sign </param>
        /// <param name="candidate"> candidate </param>
        /// <param name="signKp"> sign keypoints </param>
        /// <param name="candKp"> candidate keypints </param>
        /// <param name="match"> matches </param>
        /// <returns> resulting image </returns>
        public static Image<Bgr, byte> Draw(Image<Bgr, byte> knownSign, Image<Bgr, byte> candidate, VectorOfKeyPoint signKp, VectorOfKeyPoint candKp, VectorOfVectorOfDMatch match)
        {
            Mat homography;

            //Draw the matched keypoints
            Mat result = new Mat();
            Features2DToolbox.DrawMatches(knownSign, signKp, candidate, candKp,
                match, result, new MCvScalar(255, 255, 255), new MCvScalar(255, 255, 255), null);
            homography = Features2DToolbox.GetHomographyMatrixFromMatchedFeatures(signKp, candKp, match, null, 2);
            if (homography != null)
            {
                //draw a rectangle along the projected model
                Rectangle rect = new Rectangle(Point.Empty, knownSign.Size);
                PointF[] pts = new PointF[]
                {
                new PointF(rect.Left, rect.Bottom),
                new PointF(rect.Right, rect.Bottom),
                new PointF(rect.Right, rect.Top),
                new PointF(rect.Left, rect.Top)
                };
                pts = CvInvoke.PerspectiveTransform(pts, homography);


                Point[] points = Array.ConvertAll<PointF, Point>(pts, Point.Round);
                using (VectorOfPoint vp = new VectorOfPoint(points))
                {
                    CvInvoke.Polylines(result, vp, true, new MCvScalar(255, 0, 0, 255), 5);
                }
            }
            return result.ToImage<Bgr, byte>();
        }
    }
}
