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
        public void fit_lines_sliding_window(Image<Gray, byte> birdEye, out Image<Bgr, byte> res, int windows)
        {
            LeftPoints = new List<PointF>();
            RightPoints = new List<PointF>();
            res = birdEye.Clone().Convert<Bgr, byte>();
            Image<Gray, byte> roi = birdEye.Copy(new Rectangle(0, birdEye.Height/2, birdEye.Width, birdEye.Height - (birdEye.Height / 2)));
            
            // find left and right starting point
            int lft, rght;
            SlidingWindowsStartLoc(roi, out lft, out rght);

            // current window locations
            int curr_wind_l = lft;
            int curr_wind_r = rght;

            // window settings & buffer
            int margin = 100;
            int minpix = 50;
            int win_height = birdEye.Height / windows;
            VectorOfPoint left_ind = new VectorOfPoint();
            VectorOfPoint right_ind = new VectorOfPoint();

            // calculate windows
            for (int i = 0; i < windows; i++)
            {
                // calculate window size and location
                int win_y_high = birdEye.Height - i * win_height;
                int win_xleft_low = curr_wind_l - margin;
                int win_xright_low = curr_wind_r - margin;
                Rectangle left_rect = new Rectangle(win_xleft_low, win_y_high - win_height, margin*2, win_height);
                Rectangle right_rect = new Rectangle(win_xright_low, win_y_high - win_height, margin * 2, win_height);
                CvInvoke.Rectangle(res, left_rect, new MCvScalar(20, 20,255), 3);
                CvInvoke.Rectangle(res, right_rect, new MCvScalar(20, 20, 255), 3);
                int good_left;
                int good_right;

                // save position
                LeftPoints.Add(new Point(win_xleft_low + margin, win_y_high - (win_height / 2)));
                RightPoints.Add(new Point(win_xright_low + margin, win_y_high - (win_height / 2)));

                birdEye.ROI = left_rect;
                good_left = CvInvoke.CountNonZero(birdEye);
                birdEye.ROI = right_rect;
                good_right = CvInvoke.CountNonZero(birdEye);
                birdEye.ROI = Rectangle.Empty;

                if (good_left > minpix) {
                    // recenter
                    birdEye.ROI = left_rect;
                    curr_wind_l = CenterOfLine(birdEye) + left_rect.X;
                    birdEye.ROI = Rectangle.Empty;
                }
                if (good_right > minpix)
                {
                    // recenter
                    birdEye.ROI = right_rect;
                    curr_wind_r = CenterOfLine(birdEye) + right_rect.X;
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
            Point min_loc = new Point();
            Point max_loc = new Point();
            CvInvoke.MinMaxLoc(sobel, ref min, ref max, ref min_loc, ref max_loc);

            // invalid state
            if (min_loc.X <= max_loc.X) return src.Width / 2;
            return (min_loc.X - max_loc.X) / 2 + max_loc.X;
        }

        /// <summary>
        /// Locates the starting points of the sliding windows by calculating the center of the 2 lines. 
        /// </summary>
        /// <param name="src"> binary bird eye view </param>
        /// <param name="left_max"> horizontal center left line </param>
        /// <param name="right_max"> horizontal center right line </param>
        private void SlidingWindowsStartLoc(Image<Gray, byte> src, out int left_max, out int right_max)
        {
            // Offsets & dimensions
            int x_offset = src.Width / 2;
            int win_height = src.Height / 2;
            int w = src.Width;
            int h = src.Height;
            int white_th = win_height * x_offset / 8;
            Point min_p = new Point();
            Point max_p = new Point();
            double min = 0, max = 0;
            Mat left = new Mat(1, w, Emgu.CV.CvEnum.DepthType.Cv16U, 1);
            Mat right = new Mat(1, w, Emgu.CV.CvEnum.DepthType.Cv16U, 1);

            // Set ROI to left bottom
            src.ROI = new Rectangle(0, h - win_height, x_offset, win_height);

            // If ROI contains low amount of white pixels enlarge window
            if (CvInvoke.CountNonZero(src) < white_th) {
                src.ROI = new Rectangle(0, 0, x_offset, h);
            }

            // Reduce data to x-axis & search max
            CvInvoke.Reduce(src, left, Emgu.CV.CvEnum.ReduceDimension.SingleRow, Emgu.CV.CvEnum.ReduceType.ReduceSum, Emgu.CV.CvEnum.DepthType.Cv32S);
            CvInvoke.MinMaxLoc(left, ref min, ref max, ref min_p, ref max_p);
            left_max = max_p.X;

            // Repeat for right side
            src.ROI = new Rectangle(x_offset, h - win_height, x_offset, win_height);
            if (CvInvoke.CountNonZero(src) < white_th)
            {
                src.ROI = new Rectangle(x_offset, 0, w - x_offset, h);
            }
            CvInvoke.Reduce(src, right, Emgu.CV.CvEnum.ReduceDimension.SingleRow, Emgu.CV.CvEnum.ReduceType.ReduceSum, Emgu.CV.CvEnum.DepthType.Cv32S);
            CvInvoke.MinMaxLoc(right, ref min, ref max, ref min_p, ref max_p);

            // Reset ROI
            src.ROI = Rectangle.Empty;
            right_max = max_p.X + x_offset;
        }
    }
}
