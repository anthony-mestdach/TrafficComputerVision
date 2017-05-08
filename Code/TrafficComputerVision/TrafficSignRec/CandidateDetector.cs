using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.UI;
using Emgu.CV.Util;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace TrafficSignRec
{
    // Detects candidate traffic signs based on color
    public class CandidateDetector
    {
        // List of candidates
        public List<TrafficSign> Candidates { get; private set; }

        // Amount of padding to add around candidate
        private int candidatePadding;

        // Amount of padding to add around candidate
        public int CandidatePadding {
            get { return candidatePadding; }
            set {
                if (candidatePadding < 0) throw new Exception("Padding must be 0 or positive.");
                candidatePadding = value;
            }
        }

        /// <summary>
        /// Creates a candidate detector without padding
        /// </summary>
        private CandidateDetector() { }

        /// <summary>
        /// Creates a candidatedetector
        /// </summary>
        /// <param name="candidatePadding"> padding to add around candidate </param>
        public CandidateDetector(int candidatePadding) {
            if (candidatePadding < 0) throw new ArgumentException("Negative paddings not supported.");
            CandidatePadding = candidatePadding;
        }

        /// <summary>
        /// Looks for candidate signs in an image based on color
        /// </summary>
        /// <param name="src"> image which might contain traffic signs </param>
        public void FindCandidates(Image<Bgr, byte> src)
        {
            // Remove noise
            Image<Bgr, byte> smooth = src.SmoothBilatral(5, 1, 1);

            // Convert to Ycc color space and equalize Y-component
            // to improve low-light 
            Image<Ycc, byte> eq = smooth.Convert<Ycc, byte>();
            Image<Gray, byte>[] ch = eq.Split();
            CvInvoke.EqualizeHist(ch[0], ch[0]);
            eq = new Image<Ycc, byte>(ch);
            smooth = eq.Convert<Bgr, byte>();

            // Inc. contrast
            smooth.Mat.ConvertTo(smooth.Mat, DepthType.Default, 2.0, 1.0);
            Image<Hsv, byte> hsv = smooth.Convert<Hsv, byte>();

            // Split into HSV color chanels
            ch = hsv.Split();

            // HSV color thresholds for blue & red
            Gray value_blue_low = new Gray(120);
            Gray value_blue_high = new Gray(255);
            Gray value_red_low = new Gray(120);
            Gray value_red_high = new Gray(255);

            Gray sat_blue_low = new Gray(185);
            Gray sat_red_low = new Gray(130);

            Gray red_left_high = new Gray(5);
            Gray red_right_low = new Gray(170);

            Gray blue_left = new Gray(100);
            Gray blue_right = new Gray(140);

            // Red pixels
            Image<Gray, byte> th_red =
                ch[0].InRange(new Gray(0), red_left_high)
                .Or(ch[0].InRange(red_right_low, new Gray(255)))
                .And(ch[1].InRange(sat_red_low, new Gray(255)))
                .And(ch[2].InRange(value_red_low, value_red_high));

            // Blue pixels
            Image<Gray, byte> th_blue =
                ch[0].InRange(blue_left, blue_right)
                .And(ch[1].InRange(sat_blue_low, new Gray(255)))
                .And(ch[2].InRange(value_blue_low, value_blue_high));

            // Show binary images
            //ImageViewer vBL = new ImageViewer();
            //vBL.Image = th_blue;
            //vBL.Text = "Blue binary";
            //vBL.Show();
            //ImageViewer vRD = new ImageViewer();
            //vRD.Image = th_red;
            //vRD.Text = "Red binary";
            //vRD.Show();

            // Find contours of blobs
            VectorOfVectorOfPoint cont_red = new VectorOfVectorOfPoint();
            CvInvoke.FindContours(th_red.Copy(), cont_red, null, RetrType.External, ChainApproxMethod.ChainApproxSimple);
            VectorOfVectorOfPoint cont_blue = new VectorOfVectorOfPoint();
            CvInvoke.FindContours(th_blue.Copy(), cont_blue, null, RetrType.External, ChainApproxMethod.ChainApproxSimple);

            // Convert contours to candidates and remove small/big contours
            Candidates = new List<TrafficSign>(); 
            Candidates.AddRange(ContoursToCandidates(cont_red, src));
            Candidates.AddRange(ContoursToCandidates(cont_blue, src));
        }

        /// <summary>
        /// Converts contours to candidates and deny small or big contours
        /// </summary>
        /// <param name="contours"> contours of blobs </param>
        /// <param name="context"> original color image </param>
        /// <returns> Sign Candidates </returns>
        private List<TrafficSign> ContoursToCandidates(VectorOfVectorOfPoint contours, Image<Bgr, byte> context)
        {
            List<TrafficSign> goodCandidates = new List<TrafficSign>();
            List<Rectangle> boxes = new List<Rectangle>();

            // Iterate over contours
            for (int i = 0; i < contours.Size; i++)
            {
                Rectangle bb = CvInvoke.BoundingRectangle(contours[i]);
                // Deny small
                if (bb.Height < 20) continue;
                if (bb.Width < 20) continue;

                // Deny strange shape
                if ((double)bb.Width / bb.Height < 0.80) continue;

                // Merge overlapping BBs
                Rectangle rectToRemove = Rectangle.Empty;
                boxes.ForEach(other => 
                {
                    if (other.IntersectsWith(bb))
                    {
                        bb = CvInvoke.cvMaxRect(bb, other);
                        rectToRemove = other;
                    }
                });
                if (!rectToRemove.IsEmpty) boxes.Remove(rectToRemove);
                Rectangle paddedRoi = new Rectangle(bb.Location, bb.Size);
                paddedRoi.Inflate(CandidatePadding, CandidatePadding);
                boxes.Add(paddedRoi);
            }

            // Convert boxes to Candidate objects
            boxes.ForEach(paddedRoi =>
            {
                TrafficSign newCandidate = new TrafficSign(context.Copy(paddedRoi), null);
                newCandidate.BoundingBoxInScene = paddedRoi;
                goodCandidates.Add(newCandidate);
            });
            return goodCandidates;
        }
    }
}
