// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using System;
using System.Collections.Generic;
using UnityEngine;

namespace GPUInstancerPro
{
    public class GPUIRenderSourceProvider : GPUIDataProvider<int, GPUIRenderSource>
    {
        public override void Dispose()
        {
            if (_dataDict != null)
            {
                foreach (var rs in Values)
                {
                    if (rs != null)
                        rs.Dispose();
                }
            }

            base.Dispose();
        }

        internal void DisposeRenderer(int renderKey)
        {
            if (TryGetData(renderKey, out GPUIRenderSource renderSource))
            {
                Remove(renderKey);
                renderSource.Dispose();
            }
        }
    }
}