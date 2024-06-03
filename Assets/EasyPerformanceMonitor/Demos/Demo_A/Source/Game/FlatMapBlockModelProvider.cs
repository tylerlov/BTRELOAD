// Unity
using UnityEngine;

namespace GUPS.EasyPerformanceMonitor.Demos.A
{
    /// <summary>
    /// Returns the BlockModel for the Flat Map.
    /// </summary>
    public class FlatMapBlockModelProvider : AHeightArrayBlockModelProvider
    {
        /// <summary>
        /// Stores the height map.
        /// </summary>
        private int[,] mapHeight;

        /// <summary>
        /// Returns the height map of the Flat Map.
        /// </summary>
        protected override int[,] MapHeight 
        {
            get
            {
                if(this.mapHeight == null)
                {
                    this.mapHeight = this.GenerateMapHeight();
                }
                
                return this.mapHeight;
            }
        }

        /// <summary>
        /// Generate a height map for the Flat Map.
        /// </summary>
        /// <returns></returns>
        private int[,] GenerateMapHeight()
        {
            int var_SizeX = 75;
            int var_SizeZ = 75;

            int[,] var_MapHeight = new int[var_SizeX, var_SizeZ];

            for(int x = 0; x < var_SizeX; x++)
            {
                for(int z = 0; z < var_SizeZ; z++)
                {
                    // Default height is 1.
                    var_MapHeight[x, z] = 1;

                    // The percent of the height to rise.
                    int var_Percent = 5;

                    // If any neighbor is 2, increase the percentage to rise the self height to 2.
                    if(x > 0 && var_MapHeight[x - 1, z] == 2)
                    {
                        var_Percent += 10;
                    }
                    if(z > 0 && var_MapHeight[x, z - 1] == 2)
                    {
                        var_Percent += 10;
                    }
                    if(x < var_SizeX - 1 && var_MapHeight[x + 1, z] == 2)
                    {
                        var_Percent += 10;
                    }
                    if(z < var_SizeZ - 1 && var_MapHeight[x, z + 1] == 2)
                    {
                        var_Percent += 10;
                    }

                    // Random rise the height to 2.
                    if (UnityEngine.Random.Range(0, 100) < var_Percent)
                    {
                        var_MapHeight[x, z] = 2;
                    }
                }
            }

            return var_MapHeight;
        }

        /// <summary>
        /// Returns a grey to white color.
        /// </summary>
        public override Color Color
        {
            get
            {
                return new Color(0.15f, 0.15f, 0.15f);
            }
        }
    }
}