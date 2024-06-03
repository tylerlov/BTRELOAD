// Core
using System.Collections;
using System.Collections.Generic;

// Unity
using UnityEngine;

namespace GUPS.EasyPerformanceMonitor.Demos
{
    /// <summary>
    /// Renders the BlockModel of an IBlockModelProvider and creates a Collider if needed..
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(IBlockModelProvider), typeof(MeshFilter), typeof(MeshRenderer))]
    public class BlockRenderer : MonoBehaviour
    {
        /// <summary>
        /// Center the generate Mesh on the GameObject.
        /// </summary>
        public bool CenterBlockModel = false;

        /// <summary>
        /// Use the BlockModel also as a collider.
        /// </summary>
        public bool UseAsCollider = false;
        
        /// <summary>
        /// Provider of the rendered BlockModel.
        /// </summary>
        private IBlockModelProvider blockModelProvider = null;

        /// <summary>
        /// The BlockModel to render.
        /// </summary>
        private BlockModel blockModel = null;

        /// <summary>
        /// Needed for the mesh generation.
        /// </summary>
        private int renderFaceCount = 0;

        /// <summary>
        /// Needed for the mesh generation.
        /// </summary>
        private List<Vector3> renderVertices = new List<Vector3>();

        /// <summary>
        /// Needed for the mesh generation.
        /// </summary>
        private List<int> renderTriangles = new List<int>();

        /// <summary>
        /// Needed for the mesh generation.
        /// </summary>
        private List<Color> renderColor = new List<Color>();

        /// <summary>
        /// Called on init of the GameObject.
        /// </summary>
        private void Start()
        {
            // Create Model and Mesh.
            this.GenerateBlockModelAndMesh();
        }

        /// <summary>
        /// Generate the BlockModel, Mesh and assign it to the Filter and Collider.
        /// </summary>
        private void GenerateBlockModelAndMesh()
        {
            // Find the IBlockModelProvider.
            if (this.blockModelProvider == null)
            {
                this.blockModelProvider = this.GetComponent<IBlockModelProvider>();
            }
            
            // Get the model.
            this.blockModel = this.blockModelProvider.GenerateBlockModel();

            // Assign the mesh from model.
            this.GetComponent<MeshFilter>().sharedMesh = this.GenerateMesh();

            // Assign the mesh as collider.
            if(this.UseAsCollider)
            {
                this.GetComponent<MeshCollider>().sharedMesh = this.GetComponent<MeshFilter>().sharedMesh;
            }
        }

        /// <summary>
        /// Return the Block at _X, _Y, _Z. If there is no Block or the coordinates are out of size, returns null.
        /// </summary>
        /// <param name="_X"></param>
        /// <param name="_Y"></param>
        /// <param name="_Z"></param>
        /// <returns></returns>
        private Block GetBlock(int _X, int _Y, int _Z)
        {
            if (_X >= this.blockModel.Size.x || _X < 0 || _Y >= this.blockModel.Size.y || _Y < 0 || _Z >= this.blockModel.Size.z || _Z < 0)
            {
                return null;
            }

            int var_ArrayPosition = (int)(_X + this.blockModel.Size.x * (_Y + this.blockModel.Size.y * _Z));

            return this.blockModel.BlockArray[var_ArrayPosition];
        }
        
        /// <summary>
        /// Generate and return the Mesh of the BlockModel.
        /// </summary>
        /// <returns></returns>
        private Mesh GenerateMesh()
        {
            // Clear
            this.renderFaceCount = 0;
            this.renderVertices.Clear();
            this.renderTriangles.Clear();
            this.renderColor.Clear();

            // Generate
            if (this.blockModel != null)
            {
                for (int x = 0; x < this.blockModel.Size.x; x += Block.BlockSize)
                {
                    for (int y = 0; y < this.blockModel.Size.y; y += Block.BlockSize)
                    {
                        for (int z = 0; z < this.blockModel.Size.z; z += Block.BlockSize)
                        {
                            Block var_Block = this.GetBlock(x, y, z);
                            this.GenerateBlockMesh(x, y, z, var_Block);
                        }
                    }
                }
            }

            // Center Vertices if needed.
            if(this.CenterBlockModel)
            {
                Vector3 var_Center = new Vector3(this.blockModel.Size.x / 2, 0, this.blockModel.Size.z / 2);
                for (int i = 0; i < this.renderVertices.Count; i++)
                {
                    this.renderVertices[i] -= var_Center;
                }
            }

            // Create
            Mesh var_Mesh = new Mesh();

            var_Mesh.vertices = this.renderVertices.ToArray();
            var_Mesh.triangles = this.renderTriangles.ToArray();
            var_Mesh.colors = this.renderColor.ToArray();

            var_Mesh.RecalculateNormals();

            return var_Mesh;
        }

        /// <summary>
        /// Returns if the face needs to be rendered.
        /// </summary>
        /// <param name="_X"></param>
        /// <param name="_Y"></param>
        /// <param name="_Z"></param>
        /// <param name="_NeighbourBlock"></param>
        /// <returns></returns>
        private bool ShallRenderFace(int _X, int _Y, int _Z, Block _NeighbourBlock)
        {
            return _NeighbourBlock == null;
        }

        /// <summary>
        /// Validate and create the Mesh for a single _Block.
        /// </summary>
        /// <param name="_X"></param>
        /// <param name="_Y"></param>
        /// <param name="_Z"></param>
        /// <param name="_Block"></param>
        private void GenerateBlockMesh(int _X, int _Y, int _Z, Block _Block)
        {
            if (_Block != null)
            {
                Block var_Block = this.GetBlock(_X, _Y + 1, _Z);
                if (this.ShallRenderFace(_X, _Y, _Z, var_Block))
                {
                    // Create
                    List<Vector3> var_VerticeList = this.CubeTop(_X, _Y, _Z, _Block, Block.BlockSize);
                    List<int> var_TriangleList = this.CreateCubeFace();
                    Color var_Color = _Block.Color;

                    // Store
                    this.renderVertices.AddRange(var_VerticeList);
                    this.renderTriangles.AddRange(var_TriangleList);
                    this.renderColor.AddRange(new Color[] { var_Color, var_Color, var_Color, var_Color });
                }

                var_Block = this.GetBlock(_X, _Y - 1, _Z);
                if (this.ShallRenderFace(_X, _Y, _Z, var_Block))
                {
                    // Create
                    List<Vector3> var_VerticeList = this.CubeBot(_X, _Y, _Z, _Block, Block.BlockSize); 
                    List<int> var_TriangleList = this.CreateCubeFace();
                    Color var_Color = _Block.Color;

                    // Store
                    this.renderVertices.AddRange(var_VerticeList);
                    this.renderTriangles.AddRange(var_TriangleList);
                    this.renderColor.AddRange(new Color[] { var_Color, var_Color, var_Color, var_Color });
                }

                var_Block = this.GetBlock(_X + 1, _Y, _Z);
                if (this.ShallRenderFace(_X, _Y, _Z, var_Block))
                {
                    // Create
                    List<Vector3> var_VerticeList = this.CubeEast(_X, _Y, _Z, _Block, Block.BlockSize);
                    List<int> var_TriangleList = this.CreateCubeFace();
                    Color var_Color = _Block.Color;

                    // Store
                    this.renderVertices.AddRange(var_VerticeList);
                    this.renderTriangles.AddRange(var_TriangleList);
                    this.renderColor.AddRange(new Color[] { var_Color, var_Color, var_Color, var_Color });
                }

                var_Block = this.GetBlock(_X - 1, _Y, _Z);
                if (this.ShallRenderFace(_X, _Y, _Z, var_Block))
                {
                    // Create
                    List<Vector3> var_VerticeList = this.CubeWest(_X, _Y, _Z, _Block, Block.BlockSize);
                    List<int> var_TriangleList = this.CreateCubeFace();
                    Color var_Color = _Block.Color;

                    // Store
                    this.renderVertices.AddRange(var_VerticeList);
                    this.renderTriangles.AddRange(var_TriangleList);
                    this.renderColor.AddRange(new Color[] { var_Color, var_Color, var_Color, var_Color });
                }

                var_Block = this.GetBlock(_X, _Y, _Z + 1);
                if (this.ShallRenderFace(_X, _Y, _Z, var_Block))
                {
                    // Create
                    List<Vector3> var_VerticeList = this.CubeNorth(_X, _Y, _Z, _Block, Block.BlockSize);
                    List<int> var_TriangleList = this.CreateCubeFace();
                    Color var_Color = _Block.Color;

                    // Store
                    this.renderVertices.AddRange(var_VerticeList);
                    this.renderTriangles.AddRange(var_TriangleList);
                    this.renderColor.AddRange(new Color[] { var_Color, var_Color, var_Color, var_Color });
                }

                var_Block = this.GetBlock(_X, _Y, _Z - 1);
                if (this.ShallRenderFace(_X, _Y, _Z, var_Block))
                {
                    // Create
                    List<Vector3> var_VerticeList = this.CubeSouth(_X, _Y, _Z, _Block, Block.BlockSize);
                    List<int> var_TriangleList = this.CreateCubeFace();
                    Color var_Color = _Block.Color;

                    // Store
                    this.renderVertices.AddRange(var_VerticeList);
                    this.renderTriangles.AddRange(var_TriangleList);
                    this.renderColor.AddRange(new Color[] { var_Color, var_Color, var_Color, var_Color });
                }
            }
        }
        
        /// <summary>
        /// Create the Vertices.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <param name="_Block"></param>
        /// <param name="_BlockSize"></param>
        /// <returns></returns>
        private List<Vector3> CubeTop(float x, float y, float z, Block _Block, float _BlockSize)
        {
            List<Vector3> var_VerticeList = new List<Vector3>();

            var_VerticeList.Add(new Vector3(x, y + _BlockSize, z + _BlockSize));
            var_VerticeList.Add(new Vector3(x + _BlockSize, y + _BlockSize, z + _BlockSize));
            var_VerticeList.Add(new Vector3(x + _BlockSize, y + _BlockSize, z));
            var_VerticeList.Add(new Vector3(x, y + _BlockSize, z));

            return var_VerticeList;
        }

        /// <summary>
        /// Create the Vertices.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <param name="_Block"></param>
        /// <param name="_BlockSize"></param>
        /// <returns></returns>
        private List<Vector3> CubeBot(float x, float y, float z, Block _Block, float _BlockSize)
        {
            List<Vector3> var_VerticeList = new List<Vector3>();

            var_VerticeList.Add(new Vector3(x, y, z));
            var_VerticeList.Add(new Vector3(x + _BlockSize, y, z));
            var_VerticeList.Add(new Vector3(x + _BlockSize, y, z + _BlockSize));
            var_VerticeList.Add(new Vector3(x, y, z + _BlockSize));

            return var_VerticeList;
        }

        /// <summary>
        /// Create the Vertices.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <param name="_Block"></param>
        /// <param name="_BlockSize"></param>
        /// <returns></returns>
        private List<Vector3> CubeNorth(float x, float y, float z, Block _Block, float _BlockSize)
        {
            List<Vector3> var_VerticeList = new List<Vector3>();

            var_VerticeList.Add(new Vector3(x + _BlockSize, y, z + _BlockSize));
            var_VerticeList.Add(new Vector3(x + _BlockSize, y + _BlockSize, z + _BlockSize));
            var_VerticeList.Add(new Vector3(x, y + _BlockSize, z + _BlockSize));
            var_VerticeList.Add(new Vector3(x, y, z + _BlockSize));

            return var_VerticeList;
        }

        /// <summary>
        /// Create the Vertices.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <param name="_Block"></param>
        /// <param name="_BlockSize"></param>
        /// <returns></returns>
        private List<Vector3> CubeEast(float x, float y, float z, Block _Block, float _BlockSize)
        {
            List<Vector3> var_VerticeList = new List<Vector3>();

            var_VerticeList.Add(new Vector3(x + _BlockSize, y, z));
            var_VerticeList.Add(new Vector3(x + _BlockSize, y + _BlockSize, z));
            var_VerticeList.Add(new Vector3(x + _BlockSize, y + _BlockSize, z + _BlockSize));
            var_VerticeList.Add(new Vector3(x + _BlockSize, y, z + _BlockSize));

            return var_VerticeList;
        }

        /// <summary>
        /// Create the Vertices.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <param name="_Block"></param>
        /// <param name="_BlockSize"></param>
        /// <returns></returns>
        private List<Vector3> CubeSouth(float x, float y, float z, Block _Block, float _BlockSize)
        {
            List<Vector3> var_VerticeList = new List<Vector3>();

            var_VerticeList.Add(new Vector3(x, y, z));
            var_VerticeList.Add(new Vector3(x, y + _BlockSize, z));
            var_VerticeList.Add(new Vector3(x + _BlockSize, y + _BlockSize, z));
            var_VerticeList.Add(new Vector3(x + _BlockSize, y, z));

            return var_VerticeList;
        }

        /// <summary>
        /// Create the Vertices.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <param name="_Block"></param>
        /// <param name="_BlockSize"></param>
        /// <returns></returns>
        protected virtual List<Vector3> CubeWest(float x, float y, float z, Block _Block, float _BlockSize)
        {
            List<Vector3> var_VerticeList = new List<Vector3>();

            var_VerticeList.Add(new Vector3(x, y, z + _BlockSize));
            var_VerticeList.Add(new Vector3(x, y + _BlockSize, z + _BlockSize));
            var_VerticeList.Add(new Vector3(x, y + _BlockSize, z));
            var_VerticeList.Add(new Vector3(x, y, z));

            return var_VerticeList;
        }

        /// <summary>
        /// Create the triangles.
        /// </summary>
        /// <returns></returns>
        private List<int> CreateCubeFace()
        {
            List<int> var_TriangleList = new List<int>();
            var_TriangleList.Add(renderFaceCount * 4); //1
            var_TriangleList.Add(renderFaceCount * 4 + 1); //2
            var_TriangleList.Add(renderFaceCount * 4 + 2); //3
            var_TriangleList.Add(renderFaceCount * 4); //1
            var_TriangleList.Add(renderFaceCount * 4 + 2); //3
            var_TriangleList.Add(renderFaceCount * 4 + 3); //4

            this.renderFaceCount++;

            return var_TriangleList;
        }
    }
}