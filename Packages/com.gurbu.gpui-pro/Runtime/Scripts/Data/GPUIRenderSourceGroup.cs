// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace GPUInstancerPro
{
    public class GPUIRenderSourceGroup : IGPUIDisposable
    {
        public int Key { get; private set; }
        public int GroupID { get; private set; }
        public int PrototypeKey { get; private set; }
        public GPUIProfile Profile { get; private set; }
        public List<GPUIRenderSource> RenderSources { get; private set; }
        public string Name { get; private set; }

        /// <summary>
        /// Size of the transform buffer
        /// </summary>
        public int BufferSize { get; private set; }
        /// <summary>
        /// Total instance count for all render sources
        /// </summary>
        public int InstanceCount { get; private set; }
        /// <summary>
        /// Contains the Matrix4x4 data for instances
        /// </summary>
        public GPUITransformBufferData TransformBufferData { get; private set; }
        /// <summary>
        /// Determines how the transform buffers will be managed
        /// </summary>
        public GPUITransformBufferType TransformBufferType { get; private set; }
        /// <summary>
        /// List of enabled shader keywords for this render group
        /// </summary>
        public List<string> ShaderKeywords { get; private set; }

        /// <summary>
        /// Used for render calls
        /// </summary>
        private MaterialPropertyBlock _mpb;
        /// <summary>
        /// Renderer based material property overrides
        /// </summary>
        private GPUIMaterialPropertyOverrides _materialPropertyOverrides;
        /// <summary>
        /// Contains a list of disposables (e.g. GPUIDataBuffer) that will be disposed when this RSG is disposed
        /// </summary>
        private List<IGPUIDisposable> _dependentDisposables;

        private GPUILODGroupData _lodGroupData;
        public GPUILODGroupData LODGroupData
        {
            get
            {
                if (_lodGroupData == null)
                    GPUIRenderingSystem.Instance.LODGroupDataProvider.TryGetData(PrototypeKey, out _lodGroupData);
                return _lodGroupData;
            }
        }

        public GPUIRenderSourceGroup(int prototypeKey, GPUIProfile profile, int groupID = 0, GPUITransformBufferType transformBufferType = GPUITransformBufferType.Default, List<string> shaderKeywords = null)
        {
            this.PrototypeKey = prototypeKey;
            this.Profile = profile;
            this.GroupID = groupID;
            RenderSources = new();
            this.TransformBufferType = transformBufferType;

            ShaderKeywords = new List<string>();
            AddShaderKeywords(shaderKeywords);

            Key = GetKey(prototypeKey, profile, groupID, ShaderKeywords);

            if (LODGroupData != null)
                Name = _lodGroupData.ToString();
            else
                Name = "KEY[" + Key.ToString() + "]";
        }

        internal void UpdateCommandBuffer(GPUICameraData cameraData)
        {
            if (LODGroupData == null) 
                return;

            int lodCount = _lodGroupData.Length;

            if (!cameraData.TryGetVisibilityBufferIndex(this, out int visibilityBufferIndex))
            {
                visibilityBufferIndex = cameraData._visibilityBuffer.Length;
                cameraData._visibilityBufferIndexes.Add(Key, visibilityBufferIndex);
                for (int i = 0; i < 2; i++) // twice for shadows
                {
                    for (int l = 0; l < lodCount; l++)
                    {
                        cameraData._visibilityBuffer.Add(new GPUIVisibilityData()
                        {
                            commandCount = 0
                        });
                    }
                }
            }

            for (int i = 0; i < 2; i++) // twice for shadows
            {
                for (int l = 0; l < lodCount; l++)
                {
                    int currentVBIndex = visibilityBufferIndex + lodCount * i + l;
                    GPUIVisibilityData visibilityData = cameraData._visibilityBuffer[currentVBIndex];

                    GPUILODData gpuiLOD = _lodGroupData[l];
                    if (visibilityData.commandCount == 0)
                    {
                        uint commandStartIndex = (uint)cameraData._commandBuffer.Length;
                        List<GraphicsBuffer.IndirectDrawIndexedArgs> commandBufferArgs = gpuiLOD.GetCommandBufferArgs();
                        cameraData._commandBuffer.Add(commandBufferArgs);

                        visibilityData.commandStartIndex = commandStartIndex;
                        visibilityData.commandCount = (uint)commandBufferArgs.Count;
                        visibilityData.additional = (uint)i;
                    }
                    cameraData._visibilityBuffer[currentVBIndex] = visibilityData;
                }
            }
        }

        internal void SetBufferSize(GPUIRenderSource renderSource, int renderSourceBufferSize, bool isCopyPreviousData)
        {
            if (renderSource.bufferSize == renderSourceBufferSize)
                return;

            int previousRenderSourceBufferSize = renderSource.bufferSize;
            renderSource.bufferSize = renderSourceBufferSize;
            if (renderSource.instanceCount > renderSourceBufferSize)
                renderSource.instanceCount = renderSourceBufferSize;
            renderSource.bufferStartIndex = 0;
            BufferSize = 0;

            if (RenderSources.Count > 1)
            {
                foreach (GPUIRenderSource rs in RenderSources)
                {
                    rs.bufferStartIndex = BufferSize;
                    BufferSize += rs.bufferSize;
                }

                GPUIShaderBuffer newTransformBuffer = null;
                GPUIShaderBuffer previousTransformBuffer = null;
                if (TransformBufferData == null)
                {
                    TransformBufferData = new(this);
                    isCopyPreviousData = false;
                }
                else
                    isCopyPreviousData |= TransformBufferData.ResizeTransformBuffer(out previousTransformBuffer, out newTransformBuffer);

                if (isCopyPreviousData)
                {
                    CopyTransformBufferData(previousTransformBuffer, newTransformBuffer, 0, 0, renderSource.bufferStartIndex); // Copy previous data until the start index of the render source
                    CopyTransformBufferData(previousTransformBuffer, newTransformBuffer, renderSource.bufferStartIndex + previousRenderSourceBufferSize, renderSource.bufferStartIndex + renderSource.bufferSize, BufferSize - renderSource.bufferStartIndex - renderSource.bufferSize);
                }

                if (previousTransformBuffer != null)
                    previousTransformBuffer.Dispose();
            }
            else
            {
                BufferSize = renderSource.bufferSize;
                if (TransformBufferData == null)
                    TransformBufferData = new(this);
                else
                    TransformBufferData.ResizeTransformBuffer(isCopyPreviousData);
            }
            if (BufferSize == 0)
            {
                ReleaseBuffers();
                return;
            }

            GPUIRenderingSystem.Instance.UpdateCommandBuffers(this);
        }

        internal void CopyTransformBufferData(GPUIShaderBuffer managedBuffer, GPUIShaderBuffer transformBuffer, int managedBufferStartIndex, int graphicsBufferStartIndex, int count)
        {
            if (managedBuffer == null ||transformBuffer == null || TransformBufferData.IsCameraBasedBuffer || count <= 0) return;
            transformBuffer.Buffer.SetData(managedBuffer.Buffer, managedBufferStartIndex, graphicsBufferStartIndex, count);
        }

        internal void SetInstanceCount(GPUIRenderSource renderSource, int renderSourceInstanceCount)
        {
            if (renderSource.instanceCount == renderSourceInstanceCount)
                return;

            renderSource.instanceCount = renderSourceInstanceCount;
            UpdateInstanceCount();
        }

        private void UpdateInstanceCount()
        {
            InstanceCount = 0;
            foreach (GPUIRenderSource rs in RenderSources)
                InstanceCount += rs.instanceCount;
        }

        internal void SetTransformBufferData<T>(GPUIRenderSource renderSource, NativeArray<T> matrices, int managedBufferStartIndex, int graphicsBufferStartIndex, int count, bool isOverwritePreviousFrameBuffer) where T : struct
        {
            if (count <= 0)
                return;
            int requiredBufferSize = graphicsBufferStartIndex + count;
            bool isCopyPreviousData = graphicsBufferStartIndex != 0 || count < InstanceCount;
            if (renderSource.bufferSize < requiredBufferSize)
                SetBufferSize(renderSource, requiredBufferSize, isCopyPreviousData);

            TransformBufferData.SetTransformBufferData(matrices, managedBufferStartIndex, renderSource.bufferStartIndex + graphicsBufferStartIndex, count, isOverwritePreviousFrameBuffer);
            if (renderSource.instanceCount < count)
                SetInstanceCount(renderSource, count);
        }

        internal void SetTransformBufferData<T>(GPUIRenderSource renderSource, T[] matrices, int managedBufferStartIndex, int graphicsBufferStartIndex, int count, bool isOverwritePreviousFrameBuffer) where T : struct
        {
            if (count <= 0)
                return;
            int requiredBufferSize = graphicsBufferStartIndex + count;
            bool isCopyPreviousData = graphicsBufferStartIndex != 0 || count < InstanceCount;
            if (renderSource.bufferSize < requiredBufferSize)
                SetBufferSize(renderSource, requiredBufferSize, isCopyPreviousData);

            TransformBufferData.SetTransformBufferData(matrices, managedBufferStartIndex, renderSource.bufferStartIndex + graphicsBufferStartIndex, count, isOverwritePreviousFrameBuffer);
            if (renderSource.instanceCount < count)
                SetInstanceCount(renderSource, count);
        }

        internal void SetTransformBufferData<T>(GPUIRenderSource renderSource, List<T> matrices, int managedBufferStartIndex, int graphicsBufferStartIndex, int count, bool isOverwritePreviousFrameBuffer) where T : struct
        {
            if (count <= 0)
                return;
            int requiredBufferSize = graphicsBufferStartIndex + count;
            bool isCopyPreviousData = graphicsBufferStartIndex != 0 || count < InstanceCount;
            if (renderSource.bufferSize < requiredBufferSize)
                SetBufferSize(renderSource, requiredBufferSize, isCopyPreviousData);

            TransformBufferData.SetTransformBufferData(matrices, managedBufferStartIndex, renderSource.bufferStartIndex + graphicsBufferStartIndex, count, isOverwritePreviousFrameBuffer);
            if (renderSource.instanceCount < count)
                SetInstanceCount(renderSource, count);
        }

        internal void UpdateTransformBufferData(int frameNo)
        {
            TransformBufferData?.UpdateData(frameNo);
        }

        private void RemoveRenderSource(GPUIRenderSource renderSource)
        {
            int rsIndex = RenderSources.IndexOf(renderSource);
            if (rsIndex < 0)
                return;
            RenderSources.RemoveAt(rsIndex);
            if (renderSource.bufferSize == 0)
                return;
            BufferSize = 0;
            foreach (GPUIRenderSource rs in RenderSources)
            {
                rs.bufferStartIndex = BufferSize;
                BufferSize += rs.bufferSize;
            }
            UpdateInstanceCount();

            TransformBufferData.RemoveIndexes(renderSource.bufferStartIndex, renderSource.bufferSize);

            if (GPUIRenderingSystem.IsActive)
                GPUIRenderingSystem.Instance.UpdateCommandBuffers(this);
        }

        internal void Dispose(GPUIRenderSource renderSource)
        {
            if (RenderSources == null)
                return;
            if (!RenderSources.Contains(renderSource))
            {
                //Debug.LogError("Can not find render source with key: " + renderSource.Key);
                return;
            }
            if (RenderSources.Count == 1)
            {
                Dispose();
                return;
            }
            RemoveRenderSource(renderSource);
        }

        public void Dispose()
        {
            ReleaseBuffers();
            BufferSize = 0;
            if (RenderSources != null)
                foreach (GPUIRenderSource rs in RenderSources) { rs?.DisposeRenderSource(); }
            RenderSources = null;
            if (GPUIRenderingSystem.IsActive)
            {
                if (!GPUIRenderingSystem.Instance.RenderSourceGroupProvider.Remove(Key))
                {
#if GPUIPRO_DEVMODE
                    Debug.LogError("Can not remove RenderSourceGroup with key: " + Key);
#endif
                }
            }

            if (_dependentDisposables != null)
            {
                foreach (IGPUIDisposable disposable in _dependentDisposables)
                    disposable.Dispose();
                _dependentDisposables = null;
            }
        }

        public void ReleaseBuffers()
        {
            if (TransformBufferData != null)
            {
                TransformBufferData.Dispose();
                TransformBufferData = null;
            }
        }

        internal bool AddRenderSource(UnityEngine.Object source, GPUIRenderSource renderSource)
        {
            if (RenderSources.Exists(rs => rs.Key == renderSource.Key))
            {
                Debug.LogWarning("Renderer already registered for: " + Name + " with Key:" + renderSource.Key, source);
                return false;
            }
//#if GPUIPRO_DEVMODE
//            Debug.Log("Registered renderer for: " + Name + " with Key:" + renderSource.Key, source);
//#endif
            RenderSources.Add(renderSource);
            return true;
        }

        internal void AddDependentDisposable(IGPUIDisposable gpuiDisposable)
        {
            if (_dependentDisposables == null)
                _dependentDisposables = new List<IGPUIDisposable>();
            if (!_dependentDisposables.Contains(gpuiDisposable))
                _dependentDisposables.Add(gpuiDisposable);
        }

        private void CreateMaterialPropertyBlock()
        {
            if (_mpb == null)
            {
                _mpb = new MaterialPropertyBlock();
                _mpb.SetVector(GPUIConstants.PROP_unity_LODFade, new Vector4(1, 16, 0, 0)); // Set the default value for LODFade in case GPUI setup does not run on some shader passes
            }
        }

        internal MaterialPropertyBlock GetMaterialPropertyBlock(GPUILODGroupData lgd, GPUICameraData cameraData)
        {
            CreateMaterialPropertyBlock();
            if (Application.isPlaying && lgd.requiresTreeProxy)
                GPUIRenderingSystem.Instance.TreeProxyProvider.GetMaterialPropertyBlock(lgd, cameraData, _mpb);
            return _mpb;
        }

        internal void ApplyMaterialPropertyOverrides(MaterialPropertyBlock mpb, int lodIndex, int rendererIndex)
        {
            if (_materialPropertyOverrides != null)
                _materialPropertyOverrides.ApplyOverrides(mpb, lodIndex, rendererIndex);
        }

        public void AddMaterialPropertyOverride(string propertyName, object value, int lodIndex = -1, int rendererIndex = -1)
        {
            AddMaterialPropertyOverride(Shader.PropertyToID(propertyName), value, lodIndex, rendererIndex);
        }

        public void AddMaterialPropertyOverride(int nameID, object value, int lodIndex = -1, int rendererIndex = -1)
        {
            GPUILODGroupData lgd = LODGroupData;
            if (lgd != null && !lgd.requiresTreeProxy)
            {
                CreateMaterialPropertyBlock();
                _mpb.SetValue(nameID, value);
                return;
            }
            if (_materialPropertyOverrides == null)
                _materialPropertyOverrides = new GPUIMaterialPropertyOverrides();
            _materialPropertyOverrides.AddOverride(lodIndex, rendererIndex, nameID, value);
        }

        private void AddShaderKeyword(string keyword)
        {
            if (string.IsNullOrEmpty(keyword))
                return;
            if (!ShaderKeywords.Contains(keyword))
                ShaderKeywords.Add(keyword);
        }

        private void AddShaderKeywords(IEnumerable<string> keywords)
        {
            if (keywords == null)
                return;
            foreach (string keyword in keywords)
                AddShaderKeyword(keyword);
        }

        public static int GetKey(int prototypeKey, GPUIProfile profile, int groupID, List<string> shaderKeywords)
        {
            if (shaderKeywords == null || shaderKeywords.Count == 0)
                return GPUIUtility.GenerateHash(prototypeKey, profile.GetInstanceID(), groupID);
            shaderKeywords.Sort();
            return GPUIUtility.GenerateHash(prototypeKey, profile.GetInstanceID(), groupID, string.Concat(shaderKeywords).GetHashCode());
        }

        public int GetRenderSourceKey(UnityEngine.Object source)
        {
            return GPUIUtility.GenerateHash(source.GetInstanceID(), Key);
        }

        public override string ToString()
        {
            return Name;
        }

#if UNITY_EDITOR
        public GPUIRenderStatistics[] lodRenderStatistics;

        public GPUIRenderStatistics[] GetRenderStatisticsArray(int lodCount)
        {
            if (lodRenderStatistics == null || lodRenderStatistics.Length != lodCount)
                lodRenderStatistics = new GPUIRenderStatistics[lodCount];
            else
            {
                for (int i = 0; i < lodCount; i++)
                    lodRenderStatistics[i] = new GPUIRenderStatistics();
            }
            return lodRenderStatistics;
        }
#endif
    }

    #region Render Source

    public class GPUIRenderSource : IGPUIDisposable
    {
        public int Key { get; private set; }
        public GPUIRenderSourceGroup renderSourceGroup;
        public UnityEngine.Object source;

        public int bufferStartIndex;
        public int bufferSize;
        public int instanceCount;
        public bool isDisposed;

        public GPUIRenderSource(UnityEngine.Object source, GPUIRenderSourceGroup renderSourceGroup)
        {
            this.source = source;
            this.renderSourceGroup = renderSourceGroup;
            Key = GetKey(source, renderSourceGroup);
            bufferStartIndex = -1;
            bufferSize = 0;
            instanceCount = 0;
        }

        public void SetBufferSize(int bufferSize, bool isCopyPreviousData)
        {
            if (instanceCount < 0)
                instanceCount = bufferSize;
            renderSourceGroup.SetBufferSize(this, bufferSize, isCopyPreviousData);
        }

        public void SetInstanceCount(int instanceCount)
        {
            renderSourceGroup.SetInstanceCount(this, instanceCount);
        }

        public void SetTransformBufferData<T>(NativeArray<T> matrices, int managedBufferStartIndex, int graphicsBufferStartIndex, int count, bool isOverwritePreviousFrameBuffer) where T : struct
        {
            renderSourceGroup.SetTransformBufferData(this, matrices, managedBufferStartIndex, graphicsBufferStartIndex, count, isOverwritePreviousFrameBuffer);
        }

        public void SetTransformBufferData<T>(T[] matrices, int managedBufferStartIndex, int graphicsBufferStartIndex, int count, bool isOverwritePreviousFrameBuffer) where T : struct
        {
            renderSourceGroup.SetTransformBufferData(this, matrices, managedBufferStartIndex, graphicsBufferStartIndex, count, isOverwritePreviousFrameBuffer);
        }

        public void SetTransformBufferData<T>(List<T> matrices, int managedBufferStartIndex, int graphicsBufferStartIndex, int count, bool isOverwritePreviousFrameBuffer) where T : struct
        {
            renderSourceGroup.SetTransformBufferData(this, matrices, managedBufferStartIndex, graphicsBufferStartIndex, count, isOverwritePreviousFrameBuffer);
        }

        public void Dispose()
        {
            if (isDisposed) return;
            renderSourceGroup.Dispose(this);
            DisposeRenderSource();
        }

        internal void DisposeRenderSource()
        {
            if (isDisposed) return;
            isDisposed = true;
            if (GPUIRenderingSystem.IsActive)
                GPUIRenderingSystem.Instance.RenderSourceProvider.Remove(Key);
            bufferStartIndex = -1;
            bufferSize = 0;
            instanceCount = 0;

            if (source is GPUIManager gpuiManager && gpuiManager.IsInitialized)
                gpuiManager.OnRenderSourceDisposed(Key);
        }

        public void ReleaseBuffers()
        {
            if (source is IGPUIDisposable disposable)
                disposable.ReleaseBuffers();
        }

        public static int GetKey(UnityEngine.Object source, GPUIRenderSourceGroup renderSourceGroup)
        {
            return GPUIUtility.GenerateHash(source.GetInstanceID(), renderSourceGroup.Key);
        }
    }

    #endregion Render Source
}