// Core
using System.Collections;
using System.Collections.Generic;

// Unity
using UnityEngine;

namespace GUPS.EasyPerformanceMonitor.Demos
{
    /// <summary>
    /// A renderable model consisting of Blocks.
    /// </summary>
    public class BlockModel
    {
        /// <summary>
        /// The size of the BlockModel in Blocks in x direction.
        /// </summary>
        public byte SizeX { get; private set; }

        /// <summary>
        /// The size of the BlockModel in Blocks in y direction.
        /// </summary>
        public byte SizeY { get; private set; }

        /// <summary>
        /// The size of the BlockModel in Blocks in z direction.
        /// </summary>
        public byte SizeZ { get; private set; }

        /// <summary>
        /// The size of the BlockModel in Blocks.
        /// </summary>
        public Vector3 Size
        {
            get
            {
                return new Vector3(this.SizeX, this.SizeY, this.SizeZ);
            }
            private set
            {
                this.SizeX = (byte)value.x;
                this.SizeY = (byte)value.y;
                this.SizeZ = (byte)value.z;
            }
        }

        /// <summary>
        /// The blocks of the BlockModel.
        /// </summary>
        public Block[] BlockArray { get; private set; }

        /// <summary>
        /// Create an emtpy BlockModel with size zero.
        /// </summary>
        public BlockModel()
        {
            this.BlockArray = new Block[0];
            this.Size = Vector3.zero;
        }

        /// <summary>
        /// Create a BlockModel with _BlockArray and _Size.
        /// </summary>
        /// <param name="_BlockArray"></param>
        /// <param name="_Size"></param>
        public BlockModel(Block[] _BlockArray, Vector3 _Size)
        {
            this.BlockArray = _BlockArray;
            this.Size = _Size;
        }

        /// <summary>
        /// Return the Block at _X, _Y, _Z. If there is no Block or the coordinates are out of size, returns null.
        /// </summary>
        /// <param name="_X"></param>
        /// <param name="_Y"></param>
        /// <param name="_Z"></param>
        /// <returns></returns>
        public Block GetBlock(int _X, int _Y, int _Z)
        {
            if (_X >= this.SizeX || _X < 0 || _Y >= this.SizeY || _Y < 0 || _Z >= this.SizeZ || _Z < 0)
            {
                return null;
            }

            int var_ArrayPosition = (int)(_X + this.SizeX * (_Y + this.SizeY * _Z));

            return this.BlockArray[var_ArrayPosition];
        }

        /// <summary>
        /// Set the Block at _X, _Y, _Z with _Color and returns true. If the coordinates are out of size, returns false.
        /// </summary>
        /// <param name="_X"></param>
        /// <param name="_Y"></param>
        /// <param name="_Z"></param>
        /// <param name="_Color"></param>
        /// <returns></returns>
        public bool SetBlock(int _X, int _Y, int _Z, Color _Color)
        {
            if (_X >= this.SizeX || _X < 0 || _Y >= this.SizeY || _Y < 0 || _Z >= this.SizeZ || _Z < 0)
            {
                return false;
            }

            int var_ArrayPosition = (int)(_X + this.SizeX * (_Y + this.SizeY * _Z));

            this.BlockArray[var_ArrayPosition] = new Block(_Color);

            return true;
        }
    }
}