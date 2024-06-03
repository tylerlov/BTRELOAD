// Unity
using UnityEngine;

namespace GUPS.EasyPerformanceMonitor.Demos
{
    /// <summary>
    /// Map for the Player and Enemies to walk on.
    /// </summary>
    [RequireComponent(typeof(AHeightArrayBlockModelProvider))]
    public class Map : MonoBehaviour
    {
        /// <summary>
        /// Provider of the rendered BlockModel.
        /// </summary>
        private AHeightArrayBlockModelProvider blockModelProvider = null;

        /// <summary>
        /// The BlockModel that is rendered.
        /// </summary>
        private BlockModel blockModel = null;

        /// <summary>
        /// Called on init the GameObject.
        /// </summary>
        private void Awake()
        {
            // Find the BlockModelProvider.
            this.blockModelProvider = this.GetComponent<AHeightArrayBlockModelProvider>();

            // Get the BlockModel.
            this.blockModel = this.blockModelProvider.GenerateBlockModel();
        }

        /// <summary>
        /// Get the Block at _Position.
        /// </summary>
        /// <param name="_Position"></param>
        /// <returns></returns>
        public Block GetBlock(Vector3 _Position)
        {
            return this.blockModel.GetBlock((int)_Position.x, (int)_Position.y, (int)_Position.z);
        }
    }
}