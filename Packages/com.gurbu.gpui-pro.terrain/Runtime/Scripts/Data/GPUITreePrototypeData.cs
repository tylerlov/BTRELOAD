// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using System;
using System.Collections.Generic;
using UnityEngine;

namespace GPUInstancerPro.TerrainModule
{
    [Serializable]
    public class GPUITreePrototypeData : GPUIPrototypeData, IGPUIParameterBufferData
    {
        [SerializeField]
        public bool isApplyRotation = true;
        public bool isApplyPrefabScale = true;
        public bool isApplyHeight = true;

        #region Runtime Properties
        internal GraphicsBuffer _treeInstanceDataBuffer;
        #endregion Runtime Properties

        public GPUITreePrototypeData() { }
        public GPUITreePrototypeData(TreePrototype treePrototype) : base() { }

        public override bool Initialize(GPUIPrototype prototype)
        {
            return base.Initialize(prototype);
        }

        public override void ReleaseBuffers()
        {
            base.ReleaseBuffers();
            if (_treeInstanceDataBuffer != null)
                _treeInstanceDataBuffer.Release();
        }
    }
}
