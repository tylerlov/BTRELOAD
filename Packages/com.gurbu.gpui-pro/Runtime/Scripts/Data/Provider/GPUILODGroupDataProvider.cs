// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using System;
using System.Collections.Generic;
using UnityEngine;

namespace GPUInstancerPro
{
    public class GPUILODGroupDataProvider : GPUIDataProvider<int, GPUILODGroupData>
    {
        /// <summary>
        /// List of runtime generated GPUILODGroupData
        /// </summary>
        private List<GPUILODGroupData> _generatedLODGroups;

        public override void Initialize()
        {
            base.Initialize();
            if (_generatedLODGroups == null)
                _generatedLODGroups = new();
        }

        public override void Dispose()
        {
            base.Dispose();
            DestroyGeneratedLODGroups();
        }

        private void DestroyGeneratedLODGroups()
        {
            if (_generatedLODGroups != null)
            {
                foreach (GPUILODGroupData lgd in _generatedLODGroups)
                {
                    lgd.Dispose();
                    lgd.DestroyGeneric();
                }
                _generatedLODGroups.Clear();
            }
        }

        public void RegenerateLODGroups()
        {
            if (IsInitialized)
            {
                foreach (var key in _dataDict.Keys)
                {
                    GPUILODGroupData data = _dataDict[key];
                    if (data != null && data.prototype != null)
                    {
                        data.CreateRenderersFromPrototype(data.prototype);
                    }
                }
                GPUIRenderingSystem.Instance.UpdateCommandBuffers();
            }
        }

        public void RecalculateLODGroupBounds()
        {
            if (IsInitialized)
            {
                foreach (var key in _dataDict.Keys)
                {
                    GPUILODGroupData data = _dataDict[key];
                    if (data != null && data.prototype != null)
                    {
                        data.CalculateBounds();
                    }
                }
            }
        }

        public void RegenerateLODGroupData(GPUIPrototype prototype)
        {
            if (IsInitialized)
            {
                GPUILODGroupData lodGroupData = GetOrCreateLODGroupData(prototype);
                if (lodGroupData != null)
                {
                    lodGroupData.CreateRenderersFromPrototype(prototype);
                    lodGroupData.SetParameterBufferData();
                    GPUIRenderingSystem.Instance.UpdateCommandBuffers();
                }
            }
        }

        public GPUILODGroupData GetOrCreateLODGroupData(GPUIPrototype prototype)
        {
            if (!IsInitialized)
                Initialize();

            int key = prototype.GetKey();
            if (!TryGetData(key, out GPUILODGroupData lodGroupData) || lodGroupData == null)
            {
                if (prototype.prototypeType == GPUIPrototypeType.LODGroupData)
                    lodGroupData = prototype.gpuiLODGroupData;
                else
                {
                    lodGroupData = GPUILODGroupData.CreateLODGroupData(prototype);
                    _generatedLODGroups.Add(lodGroupData);
                }
                _dataDict[key] = lodGroupData;
            }
            return lodGroupData;
        }

        public void ClearNullValues()
        {
            if (!IsInitialized)
                return;

            for (int i = 0; i < Count; i++)
            {
                var kvPair = GetKVPairAtIndex(i);
                if (kvPair.Value == null)
                {
                    Remove(kvPair.Key);
                    i--;
                }
            }
        }
    }
}