// Microsoft
using System;
using System.Collections.Generic;
using System.Reflection;

// Unity
using Unity.Profiling;

namespace GUPS.EasyPerformanceMonitor.Provider
{
    /// <summary>
    /// Represents a profiling category used for organizing and tracking performance-related Unity Profiler metrics.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The <see cref="ProfilingCategory"/> class provides a structured way to categorize and manage
    /// various performance metrics. It includes properties to store the category,
    /// and a list of status items associated with the profiling category.
    /// </para>
    /// <para>
    /// Additionally, a predefined list of <see cref="ProfilingCategory"/> instances is available
    /// through the <see cref="AvailableCategories"/> property, offering a convenient way to access
    /// commonly used profiling categories.
    /// </para>
    /// </remarks>
    [Serializable]
    [Obfuscation(Exclude = true)]
    public class ProfilingCategory
    {
        /// <summary>
        /// Gets the specific Unity Profiler category to which the profiling belongs.
        /// </summary>
        public String Category { get; private set; }

        /// <summary>
        /// Gets the list of status items associated with the Unity Profiler category.
        /// </summary>
        public List<String> Status { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProfilingCategory"/> class.
        /// </summary>
        /// <param name="category">The specific Unity Profiler category to which the profiling belongs.</param>
        /// <param name="status">The the list of status items associated with the Unity Profiler category.</param>
        public ProfilingCategory(String category, List<String> status)
        {
            this.Category = category;
            this.Status = status;
        }

        /// <summary>
        /// Gets a list of predefined <see cref="ProfilingCategory"/> instances representing available profiling categories.
        /// </summary>
        /// <remarks>
        /// This list includes categories such as "Audio," "Memory,", "Physics" and more.
        /// Each predefined category contains a set of associated status items relevant to its specific Unity Profiler performance metrics.
        /// </remarks>
        public static List<ProfilingCategory> AvailableCategories = new List<ProfilingCategory>()
        {
            new ProfilingCategory(ProfilerCategory.Ai.Name, new List<string>()
            {
                "Custom"
            }),
            new ProfilingCategory(ProfilerCategory.Animation.Name, new List<string>()
            {
                "Custom"
            }),
            new ProfilingCategory(ProfilerCategory.Audio.Name, new List<string>()
            {
                "Custom"
            }),
            new ProfilingCategory(ProfilerCategory.FileIO.Name, new List<string>()
            {
                "Custom",
                "File Bytes Read",
                "File Bytes Written",
                "File Handles Open",
                "File Reads Finished",
                "File Reads Started",
                "File Seeks",
                "Files Closed",
                "Files Opened",
                "Reads in Flight"
            }),
            new ProfilingCategory(ProfilerCategory.Gui.Name, new List<string>()
            {
                "Custom"
            }),
            new ProfilingCategory(ProfilerCategory.Input.Name, new List<string>()
            {
                "Custom"
            }),
            new ProfilingCategory(ProfilerCategory.Internal.Name, new List<string>()
            {
                "Custom"
            }),
            new ProfilingCategory(ProfilerCategory.Lighting.Name, new List<string>()
            {
                "Custom",
                "Global Illumination Support"
            }),
            new ProfilingCategory(ProfilerCategory.Loading.Name, new List<string>()
            {
                "Custom",
                "Audio Reads",
                "Entities Reads",
                "Mesh Reads",
                "Other Reads",
                "Scripting Reads",
                "Texture Reads",
                "Virtual Texture Reads"
            }),
            new ProfilingCategory(ProfilerCategory.Memory.Name, new List<string>()
            {
                "Custom",
                "AnimationClip Count",
                "AnimationClip Memory",
                "Asset Count",
                "Audio Reserved Memory",
                "Audio Used Memory",
                "AudioClip Count",
                "AudioClip Memory",
                "Game Object Count",
                "GC Allocated In Frame",
                "GC Allocation In Frame Count",
                "GC Reserved Memory",
                "GC Used Memory",
                "Gix Reserved Memory",
                "Gíx Used Memory",
                "Material Count",
                "Material Memory",
                "Mesh Count",
                "Mesh Memory",
                "Object Count",
                "Physics Used Memory",
                "Profiler Reserved Memory",
                "Profiler Used Memory",
                "Scene Object Count",
                "System Used Memory",
                "Texture Count",
                "Texture Memory",
                "Total Reserved Memory",
                "Total Used Memory",
                "Video Reserved Memory",
                "Video Used Memory"
            }),
            new ProfilingCategory(ProfilerCategory.Network.Name, new List<string>()
            {
                "Custom"
            }),
            new ProfilingCategory(ProfilerCategory.Particles.Name, new List<string>()
            {
                "Custom"
            }),
            new ProfilingCategory(ProfilerCategory.Physics.Name, new List<string>()
            {
                "Custom",
                "Active Constraints", 
                "Active Dynamic Bodies", 
                "Active Kinematic Bodies", 
                "Articulation Bodies", 
                "Broadphase Adds", 
                "Broadphase Adds/Removes", 
                "Broadphase Removes", 
                "Colliders Synced", 
                "Continuous Overlaps", 
                "Discreet Overlaps", 
                "Dynamic Bodies", 
                "Modified Overlaps", 
                "Narrowphase Lost Touches", 
                "Narrowphase New Touches", 
                "Narrowphase Touches", 
                "Overlaps", 
                "Physics Queries", 
                "Rigidbodies Synced", 
                "Static Colliders", 
                "Trigger Overlaps"
            }),
            new ProfilingCategory(ProfilerCategory.Render.Name, new List<string>()
            {
                "Custom",
                "Batches Count",
                "CPU Main Thread Frame Time",
                "CPU Render Thread Frame Time",
                "CPU Total Frame Time",
                "Draw Calls Count",
                "Dynamic Batched Draw Calls Count",
                "Dynamic Batched Triangles Count",
                "Dynamic Batched Vertices Count",
                "Dynamic Batches Count",
                "Dynamic Batching Time",
                "GPU Frame Time",
                "Index Buffer Upload in Frame Bytes",
                "Index Buffer Upload In Frame Count",
                "Instanced Batched Draw Calls Count",
                "Instanced Batched Triangles Count",
                "Instanced Batched Vertices Count",
                "Instanced Batches Count",
                "Render Textures Bytes",
                "Render Textures Changes Count",
                "Render Textures Count",
                "SetPass Calls Count",
                "Shadow Casters Count",
                "Static Batched Draw Calls Count",
                "Static Batched Triangles Count",
                "Static Batched Vertices Count",
                "Static Batches Count",
                "Triangles Count",
                "Used Buffers Bytes",
                "Used Buffers Count",
                "Used Textures Bytes",
                "Used Textures Count",
                "Vertex Buffer Upload In Frame Bytes",
                "Vertex Buffer Upload In Frame Count",
                "Vertices Count",
                "Video Memory Bytes",
                "Visible Skinned Meshes Count"
            }),
            new ProfilingCategory(ProfilerCategory.Scripts.Name, new List<string>()
            {
                "Custom"
            }),
            new ProfilingCategory(ProfilerCategory.Video.Name, new List<string>()
            {
                "Custom"
            }),
            new ProfilingCategory(ProfilerCategory.VirtualTexturing.Name, new List<string>()
            {
                "Custom"
            }),
            new ProfilingCategory(ProfilerCategory.Vr.Name, new List<string>()
            {
                "Custom"
            }),
        };
    }        
}
