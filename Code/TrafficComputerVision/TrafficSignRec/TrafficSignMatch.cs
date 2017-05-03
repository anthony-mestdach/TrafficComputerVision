using Emgu.CV;
using Emgu.CV.Util;

namespace TrafficSignRec
{
    // Represents a traffic sign match
    public class TrafficSignMatch
    {
        // Candidate sign
        public TrafficSign Candidate { get; private set; }

        // Know Sign
        public TrafficSign KnownSign { get; private set; }

        // Matched features
        public VectorOfVectorOfDMatch Matches { get; private set; }

        // Score of the match
        public int MatchScore { get; private set; }

        // Homography matrix
        public Mat Homography { get; private set; }

        private TrafficSignMatch() { }

        /// <summary>
        /// Creates a traffic sign match
        /// </summary>
        /// <param name="candidate"> candidate sign </param>
        /// <param name="knownSign"> known sign </param>
        /// <param name="matchScore"> match score </param>
        /// <param name="matches"> feature matches </param>
        /// <param name="homography"> homography matrix </param>
        public TrafficSignMatch(TrafficSign candidate, TrafficSign knownSign, int matchScore, VectorOfVectorOfDMatch matches, Mat homography)
        {
            Candidate = candidate;
            KnownSign = knownSign;
            MatchScore = matchScore;
            Matches = matches;
            Homography = homography;
        }
    }
}
