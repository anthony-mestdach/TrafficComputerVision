using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using System.Collections.Generic;
using System.Drawing;

namespace LaneDetection
{
    public class LaneDetector
    {
        /// <summary>
        /// Left side of the lane
        /// </summary>
        public List<PointF> LeftPoints { get; private set; }

        /// <summary>
        /// Right side of the lane
        /// </summary>
        public List<PointF> RightPoints { get; private set; }

        /// <summary>
        /// Bird eye view of the lane
        /// </summary>
        public Image<Bgr, byte> BirdEye { get; private set; }

        /// <summary>
        /// Run a sliding window algorithm on the bird eye view to find the 2 sides of the lane.
        /// </summary>
        /// <param name="birdEye">bird eye image</param>
        /// <param name="res"> result of windowing </param>
        /// <param name="windows"> number of stacked windows </param>
        public void FitLinesInSlidingWindows(Image<Gray, byte> birdEye, out Image<Bgr, byte> res, int windows)
        {
            LeftPoints = new List<PointF>();
            RightPoints = new List<PointF>();
            res = birdEye.Erode(2).Clone().Convert<Bgr, byte>();
            Image<Gray, byte> roi = birdEye.Copy(
                new Rectangle(0, birdEye.Height/2, birdEye.Width, 
                birdEye.Height - (birdEye.Height / 2))
                );
            
            // find left and right starting point
            int lft, rght;
            SlidingWindowsStartLoc(roi, out lft, out rght);

            // current window locations
            int currWindowL = lft;
            int currWindowR = rght;

            // window settings & buffer
            int margin = 100;
            int minpix = 140;
            int winHeight = birdEye.Height / windows;
            VectorOfPoint left_ind = new VectorOfPoint();
            VectorOfPoint right_ind = new VectorOfPoint();

            // calculate windows
            for (int i = 0; i < windows; i++)
            {
                // calculate window size and location
                int winYhigh = birdEye.Height - i * winHeight;
                int winXleftLow = currWindowL - margin;
                int winXrightLow = currWindowR - margin;
                Rectangle leftRect = new Rectangle(winXleftLow, winYhigh - winHeight, margin*2, winHeight);
                Rectangle rightRect = new Rectangle(winXrightLow, winYhigh - winHeight, margin * 2, winHeight);
                CvInvoke.Rectangle(res, leftRect, new MCvScalar(20, 20,255), 3);
                CvInvoke.Rectangle(res, rightRect, new MCvScalar(20, 20, 255), 3);
                int goodLeft;
                int goodRight;

                // save position
                LeftPoints.Add(new Point(winXleftLow + margin, winYhigh - (winHeight / 2)));
                RightPoints.Add(new Point(winXrightLow + margin, winYhigh - (winHeight / 2)));

                birdEye.ROI = leftRect;
                goodLeft = CvInvoke.CountNonZero(birdEye);
                birdEye.ROI = rightRect;
                goodRight = CvInvoke.CountNonZero(birdEye);
                birdEye.ROI = Rectangle.Empty;

                if (goodLeft > minpix) {
                    // recenter
                    birdEye.ROI = leftRect;
                    currWindowL = CenterOfLine(birdEye) + leftRect.X;
                    birdEye.ROI = Rectangle.Empty;
                }
                if (goodRight > minpix)
                {
                    // recenter
                    birdEye.ROI = rightRect;
                    currWindowR = CenterOfLine(birdEye) + rightRect.X;
                    birdEye.ROI = Rectangle.Empty;
                }
            }

            // Draw midpoints
            foreach (PointF p in LeftPoints) {
                res.Draw(new Rectangle(new Point((int)p.X, (int)p.Y), new Size(20, 20)), new Bgr(50, 50, 230), 12);
            }
            foreach (PointF p in RightPoints)
            {
                res.Draw(new Rectangle(new Point((int)p.X, (int)p.Y), new Size(20, 20)), new Bgr(50, 50, 230), 12);
            }
            BirdEye = res;
        }

        /// <summary>
        /// Perspective transform a point array with a transformation matrix
        /// </summary>
        /// <param name="trans"> transformation matrix </param>
        /// <param name="points"> point array </param>
        /// <returns> transformed point array </returns>
        public PointF[] ProjectPoints(Mat trans, List<PointF> points) {
            return CvInvoke.PerspectiveTransform(points.ToArray(), trans);
        }

        /// <summary>
        /// Calculate horizontal offset of the vertical line (center) in the binary image 
        /// </summary>
        /// <param name="src"> binary image </param>
        /// <returns> horizontal offset of the vertical line </returns>
        private int CenterOfLine(Image<Gray, byte> src) {
            // sobel in X-direction
            Image<Gray, float> sobel = src.Clone().Erode(3).Sobel(1, 0, 3);

            // min max loc
            double min = 0, max = 0;
            Point minLoc = new Point();
            Point maxLoc = new Point();
            CvInvoke.MinMaxLoc(sobel, ref min, ref max, ref minLoc, ref maxLoc);

            // invalid state
            if (minLoc.X <= maxLoc.X) return src.Width / 2;
            return (minLoc.X - maxLoc.X) / 2 + maxLoc.X;
        }

        /// <summary>
        /// Locates the starting points of the sliding windows by calculating the center of the 2 lines. 
        /// </summary>
        /// <param name="src"> binary bird eye view </param>
        /// <param name="leftMax"> horizontal center left line </param>
        /// <param name="rightMax"> horizontal center right line </param>
        private void SlidingWindowsStartLoc(Image<Gray, byte> src, out int leftMax, out int rightMax)
        {
            // Offsets & dimensions
            int xOffset = src.Width / 2;
            int winHeight = src.Height / 2;
            int w = src.Width;
            int h = src.Height;
            int whiteTh = winHeight * xOffset / 12; 
            Point minP = new Point();
            Point maxP = new Point();
            double min = 0, max = 0;
            Mat left = new Mat(1, w, Emgu.CV.CvEnum.DepthType.Cv16U, 1);
            Mat right = new Mat(1, w, Emgu.CV.CvEnum.DepthType.Cv16U, 1);

            // Set ROI to left bottom
            src.ROI = new Rectangle(0, h - winHeight, xOffset, winHeight);

            // If ROI contains low amount of white pixels enlarge window
            if (CvInvoke.CountNonZero(src) < whiteTh) {
                src.ROI = new Rectangle(0, 0, xOffset, h);
            }

            // Reduce data to x-axis & search max
            CvInvoke.Reduce(src, left, Emgu.CV.CvEnum.ReduceDimension.SingleRow, Emgu.CV.CvEnum.ReduceType.ReduceSum, Emgu.CV.CvEnum.DepthType.Cv32S);
            CvInvoke.MinMaxLoc(left, ref min, ref max, ref minP, ref maxP);
            leftMax = maxP.X;

            // Repeat for right side
            src.ROI = new Rectangle(xOffset, h - winHeight, xOffset, winHeight);
            if (CvInvoke.CountNonZero(src) < whiteTh)
            {
                src.ROI = new Rectangle(xOffset, 0, w - xOffset, h);
            }
            CvInvoke.Reduce(src, right, Emgu.CV.CvEnum.ReduceDimension.SingleRow, Emgu.CV.CvEnum.ReduceType.ReduceSum, Emgu.CV.CvEnum.DepthType.Cv32S);
            CvInvoke.MinMaxLoc(right, ref min, ref max, ref minP, ref maxP);

            // Reset ROI
            src.ROI = Rectangle.Empty;
            rightMax = maxP.X + xOffset;
        }
    }
}
