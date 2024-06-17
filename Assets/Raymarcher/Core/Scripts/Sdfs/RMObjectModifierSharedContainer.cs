using UnityEngine;

using Raymarcher.Attributes;

namespace Raymarcher.Objects.Modifiers
{
    using static RMAttributes;

    public sealed class RMObjectModifierSharedContainer : ScriptableObject
    {
        [Space]
        [SerializeField, ReadOnly] private string containerIdentifier;
        [SerializeField, ReadOnly] private string shaderQueue;
        [SerializeReference] private object sharedContainer;

        public object SharedContainerInstance => sharedContainer;
        public string SharedContainerIdentifier => containerIdentifier;
        public string ShaderQueueID => shaderQueue;

#if UNITY_EDITOR
        public void SetSharedContainer(object entry)
        {
            containerIdentifier = entry.GetType().Name;
            sharedContainer = entry;
        }

        public void SetShaderQueueID(string queue)
        {
            shaderQueue = queue;
        }
#endif
    }
}