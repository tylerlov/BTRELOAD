// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using System;
using System.Collections.Generic;
using UnityEngine;

namespace GPUInstancerPro
{
    public class GPUIMaterialPropertyOverrides
    {
        private List<GPUIMaterialPropertyOverride> _overrides;

        public void AddOverride(int lodIndex, int rendererIndex, int nameID, object value)
        {
            if (_overrides == null)
                _overrides = new List<GPUIMaterialPropertyOverride>();
            GPUIMaterialPropertyOverride propertyOverride = new GPUIMaterialPropertyOverride()
            {
                lodIndex = lodIndex,
                rendererIndex = rendererIndex,
                nameID = nameID,
                value = value
            };
            int overrideIndex = _overrides.IndexOf(propertyOverride);
            if (overrideIndex != -1 )
                _overrides[overrideIndex] = propertyOverride;
            else
                _overrides.Add(propertyOverride);
        }

        public void ApplyOverrides(MaterialPropertyBlock mpb, int lodIndex, int rendererIndex)
        {
            if (_overrides != null)
            {
                for (int i = 0; i < _overrides.Count; i++)
                {
                    GPUIMaterialPropertyOverride propertyOverride = _overrides[i];
                    if (propertyOverride.lodIndex == lodIndex && propertyOverride.rendererIndex == rendererIndex)
                        propertyOverride.ApplyOverride(mpb);
                }
            }
        }

        public object GetOverrideValue(int lodIndex, int rendererIndex, int nameID)
        {
            if (_overrides != null)
            {
                for (int i = 0; i < _overrides.Count; i++)
                {
                    GPUIMaterialPropertyOverride propertyOverride = _overrides[i];
                    if (propertyOverride.lodIndex == lodIndex && propertyOverride.rendererIndex == rendererIndex && propertyOverride.nameID == nameID)
                        return propertyOverride.value;
                }
            }
            return null;
        }

        public int GetOverrideCount()
        {
            return _overrides != null ? _overrides.Count : 0;
        }

        internal struct GPUIMaterialPropertyOverride : IEquatable<GPUIMaterialPropertyOverride>
        {
            public int lodIndex;
            public int rendererIndex;
            public int nameID;
            public object value;

            public void ApplyOverride(MaterialPropertyBlock mpb)
            {
                if (value == null)
                    return;
                mpb.SetValue(nameID, value);
            }

            public override int GetHashCode()
            {
                return GPUIUtility.GenerateHash(lodIndex + 1, rendererIndex + 1, nameID);
            }

            public bool Equals(GPUIMaterialPropertyOverride other)
            {
                return lodIndex == other.lodIndex && rendererIndex == other.rendererIndex && nameID == other.nameID;
            }

            public override bool Equals(object obj)
            {
                if (obj is GPUIMaterialPropertyOverride other)
                    return Equals(other);
                return base.Equals(obj);
            }
        }
    }
}