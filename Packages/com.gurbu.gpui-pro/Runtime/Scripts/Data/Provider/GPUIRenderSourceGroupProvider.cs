// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using System;
using System.Collections.Generic;
using UnityEngine;

namespace GPUInstancerPro
{
    public class GPUIRenderSourceGroupProvider : GPUIDataProvider<int, GPUIRenderSourceGroup>
    {
        public override void Dispose()
        {
            if (_dataDict != null)
            {
                foreach (var rsg in Values)
                {
                    if (rsg != null)
                        rsg.Dispose();
                }
            }

            base.Dispose();
        }

        internal GPUIRenderSourceGroup GetOrCreateRenderSourceGroup(int prototypeKey, GPUILODGroupData lodGroupData, GPUIProfile profile, int groupID = 0, GPUITransformBufferType transformBufferType = GPUITransformBufferType.Default, List<string> shaderKeywords = null)
        {
            int key = GPUIRenderSourceGroup.GetKey(prototypeKey, profile, groupID, shaderKeywords);
            if (!TryGetData(key, out GPUIRenderSourceGroup renderSourceGroup))
            {
                renderSourceGroup = new GPUIRenderSourceGroup(prototypeKey, profile, groupID, transformBufferType, shaderKeywords);
                _dataDict.Add(key, renderSourceGroup);

                profile.SetParameterBufferData();
                lodGroupData.SetParameterBufferData();
                GPUIRenderingSystem.Instance.UpdateCommandBuffers(renderSourceGroup);
            }
            return renderSourceGroup;
        }

        internal void DisposeCameraData(GPUICameraData cameraData)
        {
            if (_dataDict != null)
            {
                foreach (var rsg in Values)
                {
                    if (rsg != null && rsg.TransformBufferData != null)
                        rsg.TransformBufferData.Dispose(cameraData);
                }
            }
        }
    }
}