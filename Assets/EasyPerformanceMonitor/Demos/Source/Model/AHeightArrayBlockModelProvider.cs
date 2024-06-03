// Unity
using UnityEngine;

namespace GUPS.EasyPerformanceMonitor.Demos
{
    /// <summary>
    /// This class is used to generate a BlockModel from a height map.
    /// </summary>
    public abstract class AHeightArrayBlockModelProvider : MonoBehaviour, IBlockModelProvider
    {
        /// <summary>
        /// The maximum height of the generated BlockModel.
        /// </summary>
        private const int CMaxHeight = 25;

        /// <summary>
        /// The height map.
        /// </summary>
        protected abstract int[,] MapHeight { get; }

        /// <summary>
        /// Color for the blocks. Default is white.
        /// </summary>
        public virtual Color Color { get { return Color.white; } }

        /// <summary>
        /// Generates and returns a new BlockModel based on a height map.
        /// </summary>
        /// <returns></returns>
        public BlockModel GenerateBlockModel()
        {
            int var_SizeX = this.MapHeight.GetLength(0);
            int var_SizeY = CMaxHeight;
            int var_SizeZ = this.MapHeight.GetLength(1);
            
            int var_BlockCount = this.MapHeight.GetLength(0) * CMaxHeight * this.MapHeight.GetLength(1);

            BlockModel var_BlockModel = new BlockModel(new Block[var_BlockCount], new Vector3(var_SizeX, var_SizeY, var_SizeZ));

            for (int var_X = 0; var_X < var_SizeX; var_X++)
            {
                for (int var_Z = 0; var_Z < var_SizeZ; var_Z++)
                {
                    for (int var_Y = 0; var_Y < var_SizeY; var_Y++)
                    {
                        if (var_Y < this.MapHeight[var_X, var_Z])
                        {
                            var_BlockModel.SetBlock(var_X, Mathf.Min(var_Y, CMaxHeight), var_Z, this.Color);
                        }
                    }
                }
            }

            return var_BlockModel;
        }
    }
}