using Emgu.CV.Structure;
using Emgu.CV;
using System.Drawing;

namespace Homography
{
    /// <summary>
    /// Computes the homography between 2 given rectangles and warps the source image. 
    /// </summary>
    public class HomographyLogic
    {
        /// <summary>
        /// Computes the homography between 2 given rectangles and warps the source image.
        /// </summary>
        /// <param name="src"> Source Image </param>
        /// <param name="srcPoints"> Source rectangle, clockwise point array </param>
        /// <param name="resPoints"> Destination rectangle, clockwise point array </param>
        /// <param name="resSize"> Size of the destination image </param>
        /// <param name="interpolation"> Interpolation type </param>
        /// <returns> Destination Image </returns>
        public static Image<Bgr, byte> Render(Image<Bgr, byte> src, Point[] srcPoints, Point[] resPoints, Size resSize, Emgu.CV.CvEnum.Inter interpolation)
        {
            Image<Bgr, byte> buffer = new Image<Bgr, byte>(src.Size);
            if (srcPoints.Length != 4 || resPoints.Length != 4) return src;
            float[,] roi = {
                {srcPoints[0].X, srcPoints[0].Y},
                {srcPoints[1].X, srcPoints[1].Y},
                {srcPoints[2].X, srcPoints[2].Y},
                {srcPoints[3].X, srcPoints[3].Y}
            };
            Matrix<float> sourceMat = new Matrix<float>(roi);
            float[,] target = {
                {resPoints[0].X, resPoints[0].Y},
                {resPoints[1].X, resPoints[1].Y},
                {resPoints[2].X, resPoints[2].Y},
                {resPoints[3].X, resPoints[3].Y}
            };
            Matrix<float> targetMat = new Matrix<float>(target);
            Mat transform = CvInvoke.GetPerspectiveTransform(sourceMat, targetMat);
            CvInvoke.WarpPerspective(src, buffer, transform, resSize, interpolation);
            return buffer;
        }
    }
}
