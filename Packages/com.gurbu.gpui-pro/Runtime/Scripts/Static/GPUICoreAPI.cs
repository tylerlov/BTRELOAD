// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace GPUInstancerPro
{
    public static class GPUICoreAPI
    {
        #region Global

        /// <summary>
        /// GPU Instancer Pro features a centralized rendering system, allowing for efficient rendering of objects with minimal draw calls and compute shader calculations. When GPUI rendering starts, either through GPUI Managers or API calls, the Rendering System is automatically initialized. However, you can also use this API method to initialize it manually.
        /// </summary>
        public static void InitializeRenderingSystem()
        {
            GPUIRenderingSystem.InitializeRenderingSystem();
        }

        /// <summary>
        /// The Rendering System generates internal renderers and buffers based on the prototype information, such as MeshRenderers on a prefab. When there are changes to the renderers of a prototype, such as meshes or materials, it may be necessary to update the internal renderer setup using the RegenerateRenderers method.
        /// </summary>
        public static void RegenerateRenderers()
        {
            GPUIRenderingSystem.RegenerateRenderers();
        }

        /// <summary>
        /// The Rendering System maintains various parameters such as renderer and profile settings inside a GPU buffer. When there are changes to these settings, the parameter buffer needs to be updated with the new values. For example, when the LODBias setting under the QualitySettings is changed, you need to call the UpdateParameterBufferData method for the changes to take effect for GPUI renders.
        /// </summary>
        public static void UpdateParameterBufferData()
        {
            if (GPUIRenderingSystem.IsActive)
                GPUIRenderingSystem.Instance.UpdateParameterBufferData();
        }

        /// <summary>
        /// When the DisposeAll method is called, all the runtime data related to GPUI rendering will be deleted, the allocated GPU buffers will be disposed of, and the GPUI Rendering System instance will be destroyed. 
        /// </summary>
        public static void DisposeAll()
        {
            GPUIUtility.DestroyGeneric(GPUIRenderingSystem.Instance);
        }

        #endregion Global

        #region Renderer Methods

        /// <summary>
        /// <para>The RegisterRenderer method is used to set up GPUI renderers for the specified GameObject.</para>
        /// <para>This method needs to be called to produce a renderKey, which can then be used with the <see cref="SetTransformBufferData"/> method to start rendering instances with the given transform matrices.</para>
        /// <para>To stop the rendering, the <see cref="DisposeRenderer"/> method should be called with the rendererKey output.</para>
        /// </summary>
        /// <param name="source">The source responsible for registering the renderer. E.g. the MonoBehaviour class</param>
        /// <param name="prefab">GameObject that the renderers will be based on</param>
        /// <param name="rendererKey">Integer key output that uniquely identifies the renderer</param>
        /// <returns>True when the renderers are successfully registered.</returns>
        public static bool RegisterRenderer(UnityEngine.Object source, GameObject prefab, out int rendererKey)
        {
            return GPUIRenderingSystem.RegisterRenderer(source, prefab, out rendererKey);
        }

        /// <summary>
        /// <para>The RegisterRenderer method is used to set up GPUI renderers for the specified GameObject.</para>
        /// <para>This method needs to be called to produce a renderKey, which can then be used with the <see cref="SetTransformBufferData"/> method to start rendering instances with the given transform matrices.</para>
        /// <para>To stop the rendering, the <see cref="DisposeRenderer"/> method should be called with the rendererKey output.</para>
        /// </summary>
        /// <param name="source">The source responsible for registering the renderer. E.g. the MonoBehaviour class</param>
        /// <param name="prefab">GameObject that the renderers will be based on</param>
        /// <param name="profile">GPUIProfile that determines various rendering settings</param>
        /// <param name="rendererKey">Integer key output that uniquely identifies the renderer</param>
        /// <returns>True when the renderers are successfully registered.</returns>
        public static bool RegisterRenderer(UnityEngine.Object source, GameObject prefab, GPUIProfile profile, out int rendererKey)
        {
            return GPUIRenderingSystem.RegisterRenderer(source, prefab, profile, out rendererKey);
        }

        /// <summary>
        /// <para>The RegisterRenderer method is used to set up GPUI renderers for the specified GameObject.</para>
        /// <para>This method needs to be called to produce a renderKey, which can then be used with the <see cref="SetTransformBufferData"/> method to start rendering instances with the given transform matrices.</para>
        /// <para>To stop the rendering, the <see cref="DisposeRenderer"/> method should be called with the rendererKey output.</para>
        /// </summary>
        /// <param name="source">The source responsible for registering the renderer. E.g. the MonoBehaviour class</param>
        /// <param name="prototype">GPUIPrototype that the renderers will be based on</param>
        /// <param name="rendererKey">Integer key output that uniquely identifies the renderer</param>
        /// <returns>True when the renderers are successfully registered.</returns>
        public static bool RegisterRenderer(UnityEngine.Object source, GPUIPrototype prototype, out int rendererKey)
        {
            return GPUIRenderingSystem.RegisterRenderer(source, prototype, out rendererKey);
        }

        /// <summary>
        /// Disposes the renderer data previously defined with the <see cref="RegisterRenderer"/> method.
        /// </summary>
        /// <param name="rendererKey">Integer key that uniquely identifies the renderer</param>
        public static void DisposeRenderer(int rendererKey)
        {
            GPUIRenderingSystem.DisposeRenderer(rendererKey);
        }

        /// <summary>
        /// Sets the transform matrix data to a renderer previously defined with the <see cref="RegisterRenderer"/> method.
        /// </summary>
        /// <param name="rendererKey">Integer key that uniquely identifies the renderer.</param>
        /// <param name="matrices">Matrix4x4 collection that store the transform data of instances.</param>
        /// <param name="managedBufferStartIndex">(Optional)The first element index in matrices to copy to the graphics buffer.</param>
        /// <param name="graphicsBufferStartIndex">(Optional)The first element index in the graphics buffer to receive the data.</param>
        /// <param name="count">(Optional)The number of elements to copy.</param>
        public static bool SetTransformBufferData(int rendererKey, NativeArray<Matrix4x4> matrices, int managedBufferStartIndex = 0, int graphicsBufferStartIndex = 0, int count = 0)
        {
            return GPUIRenderingSystem.SetTransformBufferData(rendererKey, matrices, managedBufferStartIndex, graphicsBufferStartIndex, count > 0 ? count : matrices.Length);
        }

        /// <summary>
        /// Sets the transform matrix data to a renderer previously defined with the <see cref="RegisterRenderer"/> method.
        /// </summary>
        /// <param name="rendererKey">Integer key that uniquely identifies the renderer.</param>
        /// <param name="matrices">Matrix4x4 collection that store the transform data of instances.</param>
        /// <param name="managedBufferStartIndex">(Optional)The first element index in matrices to copy to the graphics buffer.</param>
        /// <param name="graphicsBufferStartIndex">(Optional)The first element index in the graphics buffer to receive the data.</param>
        /// <param name="count">(Optional)The number of elements to copy.</param>
        public static bool SetTransformBufferData(int rendererKey, Matrix4x4[] matrices, int managedBufferStartIndex = 0, int graphicsBufferStartIndex = 0, int count = 0)
        {
            return GPUIRenderingSystem.SetTransformBufferData(rendererKey, matrices, managedBufferStartIndex, graphicsBufferStartIndex, count > 0 ? count : matrices.Length);
        }

        /// <summary>
        /// Sets the transform matrix data to a renderer previously defined with the <see cref="RegisterRenderer"/> method.
        /// </summary>
        /// <param name="rendererKey">Integer key that uniquely identifies the renderer.</param>
        /// <param name="matrices">Matrix4x4 collection that store the transform data of instances.</param>
        /// <param name="managedBufferStartIndex">(Optional)The first element index in matrices to copy to the graphics buffer.</param>
        /// <param name="graphicsBufferStartIndex">(Optional)The first element index in the graphics buffer to receive the data.</param>
        /// <param name="count">(Optional)The number of elements to copy.</param>
        public static bool SetTransformBufferData(int rendererKey, List<Matrix4x4> matrices, int managedBufferStartIndex = 0, int graphicsBufferStartIndex = 0, int count = 0)
        {
            return GPUIRenderingSystem.SetTransformBufferData(rendererKey, matrices, managedBufferStartIndex, graphicsBufferStartIndex, count > 0 ? count : matrices.Count);
        }

        /// <summary>
        /// Outputs the GraphicsBuffer that contains the Matrix4x4 data for instances.
        /// </summary>
        /// <param name="rendererKey">Integer key that uniquely identifies the renderer.</param>
        /// <param name="transformBuffer">Graphics buffer that contains the Matrix4x4 data for instances.</param>
        /// <param name="bufferStartIndex">Start index for instances for the given render key on the GraphicsBuffer.</param>
        /// <returns>True when a transform buffer is found for the given rendererKey.</returns>
        public static bool TryGetTransformBuffer(int rendererKey, out GraphicsBuffer transformBuffer, out int bufferStartIndex)
        {
            return TryGetTransformBuffer(rendererKey, out transformBuffer, out bufferStartIndex, out _);
        }

        /// <summary>
        /// Outputs the GraphicsBuffer that contains the Matrix4x4 data for instances.
        /// </summary>
        /// <param name="rendererKey">Integer key that uniquely identifies the renderer.</param>
        /// <param name="transformBuffer">Graphics buffer that contains the Matrix4x4 data for instances.</param>
        /// <param name="bufferStartIndex">Start index for instances for the given render key on the GraphicsBuffer.</param>
        /// <param name="bufferSize">Total size for instances for the given render key on the GraphicsBuffer.</param>
        /// <returns>True when a transform buffer is found for the given rendererKey.</returns>
        public static bool TryGetTransformBuffer(int rendererKey, out GraphicsBuffer transformBuffer, out int bufferStartIndex, out int bufferSize)
        {
            transformBuffer = null;
            bool result = GPUIRenderingSystem.TryGetTransformBuffer(rendererKey, out GPUIShaderBuffer shaderBuffer, out bufferStartIndex, out bufferSize, null, false);
            if (result)
                transformBuffer = shaderBuffer.Buffer;
            return result;
        }

        /// <summary>
        /// Sets the transform data buffer size to a renderer previously defined with the <see cref="RegisterRenderer"/> method.
        /// </summary>
        /// <param name="rendererKey">Integer key that uniquely identifies the renderer.</param>
        /// <param name="bufferSize">Size of the buffer to allocate in GPU memory.</param>
        public static bool SetBufferSize(int rendererKey, int bufferSize)
        {
            return GPUIRenderingSystem.SetBufferSize(rendererKey, bufferSize);
        }

        /// <summary>
        /// Sets the number of instances currently rendered for a renderer previously defined with the <see cref="RegisterRenderer"/> method.
        /// </summary>
        /// <param name="rendererKey">Integer key that uniquely identifies the renderer.</param>
        /// <param name="instanceCount">Number of instances to be processed.</param>
        /// <returns></returns>
        public static bool SetInstanceCount(int rendererKey, int instanceCount)
        {
            return GPUIRenderingSystem.SetInstanceCount(rendererKey, instanceCount);
        }

        /// <summary>
        /// AddMaterialPropertyOverride methods lets you add or change properties on the MaterialPropertyBlock that is used for the draw calls.
        /// </summary>  
        /// <param name="rendererKey">Integer key that uniquely identifies the renderer</param>
        /// <param name="propertyName">The name of the property</param>
        /// <param name="propertyValue">The value of the property</param>
        /// <param name="lodIndex">(Optional)LOD</param>
        /// <param name="rendererIndex">(Optional)The renderer index on the LOD</param>
        public static void AddMaterialPropertyOverride(int rendererKey, string propertyName, object propertyValue, int lodIndex = -1, int rendererIndex = -1)
        {
            GPUIRenderingSystem.AddMaterialPropertyOverride(rendererKey, propertyName, propertyValue, lodIndex, rendererIndex);
        }

        /// <summary>
        /// AddMaterialPropertyOverride methods lets you add or change properties on the MaterialPropertyBlock that is used for the draw calls.
        /// </summary>  
        /// <param name="rendererKey">Integer key that uniquely identifies the renderer</param>
        /// <param name="nameID">The name ID of the property retrieved by Shader.PropertyToID</param>
        /// <param name="propertyValue">The value of the property</param>
        /// <param name="lodIndex">(Optional)LOD</param>
        /// <param name="rendererIndex">(Optional)The renderer index on the LOD</param>
        public static void AddMaterialPropertyOverride(int rendererKey, int nameID, object propertyValue, int lodIndex = -1, int rendererIndex = -1)
        {
            GPUIRenderingSystem.AddMaterialPropertyOverride(rendererKey, nameID, propertyValue, lodIndex, rendererIndex);
        }

        #endregion Renderer Methods

        #region GPUI Manager Methods

        /// <summary>
        /// Adds the prototype to the GPUI Manager.
        /// </summary>
        /// <param name="gpuiManager"></param>
        /// <param name="prototype"></param>
        /// <returns>Prototype index on the GPUI Manager. -1 when add operation fails.</returns>
        public static int AddPrototype(GPUIManager gpuiManager, GPUIPrototype prototype)
        {
            return gpuiManager.AddPrototype(prototype);
        }

        /// <summary>
        /// Adds the given GameObject as a prototype to the GPUI Manager.
        /// </summary>
        /// <param name="gpuiManager"></param>
        /// <param name="prefab"></param>
        /// <returns>Prototype index. -1 when add operation fails.</returns>
        public static int AddPrototype(GPUIManager gpuiManager, GameObject prefab)
        {
            return gpuiManager.AddPrototype(prefab);
        }

        #endregion GPUI Manager Methods

        #region Camera Events

        /// <summary>
        /// Adds a method to execute before each camera's visibility calculations.
        /// </summary>
        /// <param name="cameraEvent">Action that will be executed for each camera</param>
        public static void AddCameraEventOnPreCull(UnityAction<GPUICameraData> cameraEvent)
        {
            if (!GPUIRenderingSystem.IsActive)
                GPUIRenderingSystem.InitializeRenderingSystem();
            GPUIRenderingSystem.Instance.OnPreCull.AddListener(cameraEvent);
        }

        /// <summary>
        /// Removes the method previously added with the <see cref="AddCameraEventOnPreCull"/> method.
        /// </summary>
        /// <param name="cameraEvent">Action that is previously added with the AddCameraEvent method</param>
        public static void RemoveCameraEventOnPreCull(UnityAction<GPUICameraData> cameraEvent)
        {
            if (GPUIRenderingSystem.IsActive)
                GPUIRenderingSystem.Instance.OnPreCull.RemoveListener(cameraEvent);
        }

        /// <summary>
        /// Adds a method to execute after each camera's visibility calculations and before draw calls.
        /// </summary>
        /// <param name="cameraEvent">Action that will be executed for each camera</param>
        public static void AddCameraEventOnPreRender(UnityAction<GPUICameraData> cameraEvent)
        {
            if (!GPUIRenderingSystem.IsActive)
                GPUIRenderingSystem.InitializeRenderingSystem();
            GPUIRenderingSystem.Instance.OnPreRender.AddListener(cameraEvent);
        }

        /// <summary>
        /// Removes the method previously added with the <see cref="AddCameraEventOnPreRender"/> method.
        /// </summary>
        /// <param name="cameraEvent">Action that is previously added with the AddCameraEvent method</param>
        public static void RemoveCameraEventOnPreRender(UnityAction<GPUICameraData> cameraEvent)
        {
            if (GPUIRenderingSystem.IsActive)
                GPUIRenderingSystem.Instance.OnPreRender.RemoveListener(cameraEvent);
        }

        /// <summary>
        /// Adds a method to execute after each camera's draw calls.
        /// </summary>
        /// <param name="cameraEvent">Action that will be executed for each camera</param>
        public static void AddCameraEventOnPostRender(UnityAction<GPUICameraData> cameraEvent)
        {
            if (!GPUIRenderingSystem.IsActive)
                GPUIRenderingSystem.InitializeRenderingSystem();
            GPUIRenderingSystem.Instance.OnPostRender.AddListener(cameraEvent);
        }

        /// <summary>
        /// Removes the method previously added with the <see cref="AddCameraEventOnPostRender"/> method.
        /// </summary>
        /// <param name="cameraEvent">Action that is previously added with the AddCameraEvent method</param>
        public static void RemoveCameraEventOnPostRender(UnityAction<GPUICameraData> cameraEvent)
        {
            if (GPUIRenderingSystem.IsActive)
                GPUIRenderingSystem.Instance.OnPostRender.RemoveListener(cameraEvent);
        }

        #endregion Camera Events
    }
}