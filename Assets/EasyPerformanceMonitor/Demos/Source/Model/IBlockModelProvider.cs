// Unity
using UnityEngine;

namespace GUPS.EasyPerformanceMonitor.Demos
{
    /// <summary>
    /// This interface is used to generate a BlockModel.
    /// </summary>
    public interface IBlockModelProvider
    {
        /// <summary>
        /// Generates and returns a new BlockModel.
        /// </summary>
        /// <returns></returns>
        BlockModel GenerateBlockModel();
    }
}