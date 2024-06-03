// Unity
using UnityEngine;

namespace GUPS.EasyPerformanceMonitor.Demos
{
    /// <summary>
    /// A simple block in a BlockModel.
    /// </summary>
    public class Block
    {
        /// <summary>
        /// The size of a block in world units.
        /// </summary>
        public static int BlockSize = 1;

        /// <summary>
        /// The color of the block.
        /// </summary>
        public Color Color { get; private set; }

        /// <summary>
        /// Create a new Block with _Color.
        /// </summary>
        /// <param name="_Color"></param>
        public Block(Color _Color)
        {
            this.Color = _Color;
        }
    }
}
