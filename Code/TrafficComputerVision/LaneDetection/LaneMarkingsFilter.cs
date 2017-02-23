﻿using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
using System;

namespace LaneDetection
{
    public class LaneMarkingsFilter : IDisposable
    {
        // HSV color definitions
        private Hsv hsv_yellow_min = new Hsv(0, 70, 70);
        private Hsv hsv_yellow_max = new Hsv(50, 255, 255);
        private Hsv hsv_white_min = new Hsv(20, 0, 180);
        private Hsv hsv_white_max = new Hsv(255, 80, 255);

        // Kernel for 'closing' morphology
        private Mat kernel = new Mat(5, 5, DepthType.Cv8U, 1);

        /// <summary>
        /// Generates a binary image where only the lane markings wil be high.
        /// Filters based on color (allows white & yellow markings).
        /// </summary>
        /// <param name="src"> BGR image to filter </param>
        /// <returns> Binary lane markings image </returns>
        public Image<Gray, byte> FilterMarkings(Image<Bgr, byte> src)
        {
            // Filter (pass) white & yellow
            Image<Gray, byte> white = hsv_filter(src, hsv_white_min, hsv_white_max);
            Image<Gray, byte> yellow = hsv_filter(src, hsv_yellow_min, hsv_yellow_max);

            // Equalize histogram and thresh
            Image<Gray, byte> white_from_eq = white_from_histogram_eq(src, 250, 255);
            Image<Gray, byte> bin = white | yellow | white_from_eq;
            kernel.SetTo(new MCvScalar(1));
            CvInvoke.MorphologyEx(bin, bin, MorphOp.Close, kernel, new System.Drawing.Point(-1, -1), 1, BorderType.Constant, CvInvoke.MorphologyDefaultBorderValue);
            return bin;
        }

        /// <summary>
        /// HSV color filter, passes all pixels between the min-max limits. 
        /// </summary>
        /// <param name="src"> Image to filter </param>
        /// <param name="min"> lower hsv point </param>
        /// <param name="max"> upper hsv point </param>
        /// <returns> binary image (high pixels are in range) </returns>
        private Image<Gray, byte> hsv_filter(Image<Bgr, byte> src, Hsv min, Hsv max)
        {
            Image<Hsv, byte> hsv = src.Convert<Hsv, byte>();
            return hsv.InRange(min, max);
        }

        /// <summary>
        /// Equalizes the histogram of an image and thresholds the result.
        /// </summary>
        /// <param name="src"> Image to eq & thresh </param>
        /// <param name="thresh"> thresh level </param>
        /// <param name="max"> max value to use </param>
        /// <returns> binary image (high pixels are in range) </returns>
        private Image<Gray, byte> white_from_histogram_eq(Image<Bgr, byte> src, byte thresh, byte max)
        {
            Image<Gray, byte> gray = src.Convert<Gray, byte>();
            CvInvoke.EqualizeHist(gray, gray);
            CvInvoke.Threshold(gray, gray, thresh, max, ThresholdType.Binary);
            return gray;
        }

        /// <summary>
        /// Releases unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            kernel.Dispose();
        }
    }
}
