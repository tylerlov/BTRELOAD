
using GraphProcessor;
using UnityEngine;
using UnityEngine.Assertions;

namespace ProjectDawn.Impostor
{
    [NodeMenuItem("Capture Points/Hemi-Octahedral Capture Points")]
    public class HemiOctahedronCapturePointsNode : ImpostorNode
    {
        public override string name => "Hemi-Octahedral Capture Points";

        [Input]
        public Surface Surface;
        [SerializeField, Input]
        public int Frames = 10;
        [Output]
        public CapturePoints CapturePoints;
        public bool TightBounds = true;

        protected override void Process()
        {
            if (Frames % 2 != 0)
            {
                throw new System.InvalidOperationException("Frames must be a multiple of 2.");
            }

            if (TightBounds)
            {
                CapturePoints = CapturePoints.HemiOctahedral(Surface.GetTightBounds().position, Surface.GetTightBounds().radius, Frames);

            }
            else
            {
                CapturePoints = CapturePoints.HemiOctahedral(Surface.GetBounds().position, Surface.GetBounds().radius, Frames);
            }
        }
    }
}