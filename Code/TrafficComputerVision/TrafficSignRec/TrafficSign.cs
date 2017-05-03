using Emgu.CV;
using Emgu.CV.Features2D;
using Emgu.CV.Flann;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using System;
using System.Drawing;

namespace TrafficSignRec
{
    public class TrafficSign
    {
        // Height of sign image
        private static int SignHeight = 100;

        // Name of sign
        public string Name { get; private set; }

        // Feature matcher
        private FlannBasedMatcher matcher;

        // Original color image
        public Image<Bgr, byte> ImageOriginal { private set; get; }

        // Gray image
        public Image<Gray, byte> ImageGray { private set; get; }

        // Keypoints
        public VectorOfKeyPoint KeyPoints;

        // Features
        private Mat features;

        // Features 
        public Mat Features
        {
            get { return features; }
            set {
                if (features != null && IsKnownSign) InitMatcher();
                features = value;
                if (IsKnownSign) matcher.Add(value);
            }
        }

        // Initialises the matcher
        private void InitMatcher()
        {
            LinearIndexParams ip = new LinearIndexParams();
            SearchParams sp = new SearchParams();
            matcher = new FlannBasedMatcher(ip, sp);
        }

        // Bounding box in scene
        public Rectangle BoundingBoxInScene;

        // True if current sign is a known sign
        public bool IsKnownSign { get; private set; }

        /// <summary>
        /// Creates a traffic sign
        /// </summary>
        /// <param name="Image"> image of the sign </param>
        public TrafficSign(Image<Bgr, byte> Image)
        {
            ImageOriginal = Image;
            Emgu.CV.CvEnum.Inter inter = Image.Height > SignHeight ? Emgu.CV.CvEnum.Inter.Area : Emgu.CV.CvEnum.Inter.Cubic;
            ImageGray = Image.Convert<Gray, byte>().Resize(int.MaxValue, SignHeight, inter, true);
            Name = null;
            matcher = null;
            IsKnownSign = false;
        }

        /// <summary>
        /// Creates a traffic sign
        /// </summary>
        /// <param name="image"> image of the sign </param>
        /// <param name="name"> name of the sign </param>
        public TrafficSign(Image<Bgr, byte> image, string name)
        {
            ImageOriginal = image;
            Emgu.CV.CvEnum.Inter inter = image.Height > SignHeight ? Emgu.CV.CvEnum.Inter.Area : Emgu.CV.CvEnum.Inter.Cubic;
            ImageGray = image.Convert<Gray, byte>().Resize(int.MaxValue, SignHeight, inter, true);
            Name = name;
            InitMatcher();
            IsKnownSign = true;
        }

        /// <summary>
        /// Match this sign to an other sign
        /// </summary>
        /// <param name="otherSign"> other traffic sign </param>
        /// <returns> matched features as matches </returns>
        public VectorOfVectorOfDMatch MatchToOtherSign(TrafficSign otherSign) {
            if (!IsKnownSign) throw new Exception("Only known signs support matching.");
            if (otherSign.Features.Cols < 1) throw new Exception("Other sign has no descriptors to match.");
            if (Features.Cols < 1) throw new Exception("Current sign has no features to match.");
            VectorOfVectorOfDMatch matches = new VectorOfVectorOfDMatch();
            matcher.KnnMatch(otherSign.features, matches, 2, null);
            return matches;
        }
    }
}
