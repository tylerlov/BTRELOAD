// Unity
using UnityEngine;

namespace GUPS.EasyPerformanceMonitor.Demos
{
    /// <summary>
    /// This class is used to generate a BlockModel from a size with a color.
    /// </summary>
    public class SizeBlockModelProvider : MonoBehaviour, IBlockModelProvider
    {
        /// <summary>
        /// BlockModel size.
        /// </summary>
        public Vector3 Size;

        /// <summary>
        /// BlockModel color.
        /// </summary>
        public Color Color;

        /// <summary>
        /// Generate a BlockModel from a size with a color.
        /// </summary>
        /// <returns></returns>
        public BlockModel GenerateBlockModel()
        {
            int var_BlockCount = (int)(Size.x * Size.y * Size.z);

            BlockModel var_BlockModel = new BlockModel(new Block[var_BlockCount], new Vector3(Size.x, Size.y, Size.z));
            
            for (int var_X = 0; var_X < Size.x; var_X++)
            {
                for (int var_Z = 0; var_Z < Size.z; var_Z++)
                {
                    for (int var_Y = 0; var_Y < Size.y; var_Y++)
                    {
                        var_BlockModel.SetBlock(var_X, var_Y, var_Z, Color);
                    }
                }
            }

            return var_BlockModel;
        }
    }
}