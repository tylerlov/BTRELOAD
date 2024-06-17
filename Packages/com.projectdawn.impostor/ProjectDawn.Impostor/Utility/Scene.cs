using System;
using UnityEngine;

namespace ProjectDawn.Impostor
{
    [AttributeUsage(AttributeTargets.Class)]
    public class RenderPipelineAttribute : Attribute
    {
        public Type PipelineType { get; private set; }

        public RenderPipelineAttribute(Type pipelineType)
        {
            PipelineType = pipelineType;
        }
    }

    public abstract class Scene : IDisposable
    {
        public abstract CapturePoints CapturePoints { get; }
        public void Dispose() => Cleanup();

        public abstract void Render(RenderTexture target, RenderMode mode);
        public abstract void RenderCombinedMask(RenderTexture target, Material blitMaterial);
        protected virtual void Cleanup() { }
    }
}