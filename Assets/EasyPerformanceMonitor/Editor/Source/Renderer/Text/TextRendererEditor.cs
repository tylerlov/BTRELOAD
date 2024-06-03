// Unity
using UnityEngine;
using UnityEditor;

// GUPS
using GUPS.EasyPerformanceMonitor.Provider;
using GUPS.EasyPerformanceMonitor.Renderer;

namespace GUPS.EasyPerformanceMonitor.Editor
{
    /// <summary>
    /// Custom editor for inspecting and modifying the ITextRenderer component in the Unity editor.
    /// </summary>
    [CustomEditor(typeof(ITextRenderer), editorForChildClasses: true)]
    public class TextRendererEditor : UnityEditor.Editor
    {
        /// <summary>
        /// Get all provider of the target object.
        /// </summary>
        /// <returns>An array of provider.</returns>
        protected IProvider[] GetProvider()
        {
            // Get the target object.
            MonoBehaviour var_Target = this.serializedObject.targetObject as MonoBehaviour;

            // Get the providers.
            var var_Providers = var_Target.GetComponents<IProvider>();

            // Return the providers.
            return var_Providers;
        }

        /// <summary>
        /// Get all provider of the target object.
        /// </summary>
        /// <typeparam name="TProvider">The type of the provider.</typeparam>
        /// <returns>An array of provider.</returns>
        protected TProvider[] GetProvider<TProvider>()
            where TProvider : IProvider
        {
            // Get the target object.
            MonoBehaviour var_Target = this.serializedObject.targetObject as MonoBehaviour;

            // Get the providers.
            var var_Providers = var_Target.GetComponents<TProvider>();

            // Return the providers.
            return var_Providers;
        }
    }
}