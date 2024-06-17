using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace ProjectDawn.Impostor.Samples.GameKit
{
    public class ImpostorRuntime : MonoBehaviour
    {
        public ImpostorGraph Graph;
        public GameObject Target;
        ImpostorBuilder Builder;

        void Update()
        {
            // TODO: Find out proper solution for this
            // Seems like there is a bit issue with first frame in SRP as shaders dont compile their available passes even with sync compilation
            if (Time.frameCount != 20)
                return;

            if (Target == null)
                throw new System.InvalidOperationException("Target is not set");

            Builder = ScriptableObject.CreateInstance<ImpostorBuilder>();
            Builder.Graph = Graph;
            Builder.SetGameObject("Source", Target);
            Builder.SetInteger("Frames", 12);
            Builder.SetInteger("Resolution", 2048);
            Builder.SetInteger("Resolution2", 1024);
            Stopwatch stopWatch = new();
            stopWatch.Start();
            var impostor = Builder.Build();
            stopWatch.Stop();
            Debug.Log($"Finished impostor build in {stopWatch.ElapsedMilliseconds}ms");

            if (TryGetComponent(out MeshRenderer meshRenderer))
            {
                meshRenderer.sharedMaterial = impostor.Material;
            }

            if (TryGetComponent(out MeshFilter meshFilter))
            {
                meshFilter.sharedMesh = impostor.Mesh;
            }
        }

        void OnDestroy()
        {
            DestroyImmediate(Builder);
        }
    }
}