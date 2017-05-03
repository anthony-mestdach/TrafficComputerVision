using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.UI;
using System.Drawing;

namespace LaneDetection
{
    public class PerspectiveTransformer
    {
        /// <summary>
        /// Creates the transformation matrix (in both directions) to wrap the zone in front of the car.
        /// </summary>
        /// <param name="src"> source image </param>
        /// <param name="transform"> transformation matrix </param>
        /// <param name="inv_transform"> inverse transformation matrix </param>
        /// <param name="warp"> wrapped zone of interest </param>
        public void GetBirdEye(Image<Gray, byte> src, out Mat transform, out Mat inv_transform, out Image<Gray, byte> warp)
        {
            
            // Create perspective in front of the car
            float[,] roi = {
                            {src.Width, src.Height-0},
                            {0, src.Height-0},
                            {546, 460},
                            {732, 460}
                       };
            Matrix<float> sourceMat = new Matrix<float>(roi);

            // Create target bounding box
            float[,] target = {
                            {src.Width, src.Height},
                            {0, src.Height},
                            {0, 0},
                            {src.Width, 0}
                       };

            // Generate transformation matrix in both directions
            Matrix<float> targetMat = new Matrix<float>(target);
            transform = CvInvoke.GetPerspectiveTransform(sourceMat, targetMat);
            inv_transform = CvInvoke.GetPerspectiveTransform(targetMat, sourceMat);
            warp = new Image<Gray, byte>(src.Size);
            CvInvoke.WarpPerspective(src, warp, transform, src.Size, Emgu.CV.CvEnum.Inter.Linear);
        }
    }
}
