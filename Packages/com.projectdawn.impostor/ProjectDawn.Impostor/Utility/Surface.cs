using System.Collections.Generic;
using UnityEngine;

namespace ProjectDawn.Impostor
{
    public class Surface
    {
        public GameObject GameObject;
        public Renderer[] Renderers;
        public Matrix4x4 Matrix;

        public Surface(GameObject gameObject)
        {
            GameObject = gameObject;
            Renderers = CollectRenderers(gameObject);

            // Bake transform too
            if (GameObject.transform.parent != null)
                Matrix = GameObject.transform.parent.worldToLocalMatrix * Matrix4x4.Translate(-GameObject.transform.localPosition);
            else
                Matrix = Matrix4x4.Translate(-GameObject.transform.localPosition);
        }

        private Surface()
        {
        }

        public BoundingSphere GetBounds()
        {
            return CalculateBoundingSphere(Renderers, Matrix);
        }

        public BoundingSphere GetTightBounds()
        {
            return CalculateBoundingSphereTight(Renderers, Matrix);
        }

        public Surface Clone(System.Func<Renderer, bool> filterRenderer)
        {
            var renderers = new List<Renderer>();
            foreach (var renderer in Renderers)
            {
                if (filterRenderer(renderer))
                    renderers.Add(renderer);
            }

            return new Surface
            {
                GameObject = GameObject,
                Renderers = renderers.ToArray(),
                Matrix = Matrix,
            };
        }

        static BoundingSphere CalculateBoundingSphereTight(Renderer[] renderers, Matrix4x4 matrix, float scaleMargin = 1.0f)
        {
            // Early out
            if (renderers == null || renderers.Length == 0)
                return new BoundingSphere();

            // Grow bounds, first centered on root transform
            var bounds = new Bounds(Vector3.zero, Vector3.zero);
            foreach (var renderer in renderers)
            {
                var mesh = renderer.GetSharedMesh();
                var transform = renderer.transform;
                var vertices = mesh.vertices;
                foreach (var vertex in vertices)
                {
                    // Convert to root space
                    var vertexOS = vertex;
                    var vertexWS = transform.localToWorldMatrix.MultiplyPoint3x4(vertexOS);
                    var vertexRS = matrix.MultiplyPoint3x4(vertexWS);

                    bounds.Encapsulate(vertexRS);
                }
            }

            var radius = 0f;
            foreach (var renderer in renderers)
            {
                var mesh = renderer.GetSharedMesh();
                var transform = renderer.transform;
                var vertices = mesh.vertices;
                foreach (var vertex in vertices)
                {
                    // Convert to root space
                    var vertexOS = vertex;
                    var vertexWS = transform.localToWorldMatrix.MultiplyPoint3x4(vertexOS);
                    var vertexRS = matrix.MultiplyPoint3x4(vertexWS);

                    var vertexRSCentered = vertexRS - bounds.center;

                    radius = Mathf.Max(vertexRSCentered.magnitude, radius);
                }
            }

            return new BoundingSphere(bounds.center, radius * scaleMargin);
        }

        static BoundingSphere CalculateBoundingSphere(Renderer[] renderers, Matrix4x4 matrix)
        {
            // Early out
            if (renderers == null || renderers.Length == 0)
                return new BoundingSphere();

            // Grow bounds, first centered on root transform
            var bounds = new Bounds(Vector3.zero, Vector3.zero);
            foreach (var renderer in renderers)
            {
                var mesh = renderer.GetSharedMesh();
                var transform = renderer.transform;
                var vertices = mesh.vertices;
                foreach (var vertex in vertices)
                {
                    // Convert to root space
                    var vertexOS = vertex;
                    var vertexWS = transform.localToWorldMatrix.MultiplyPoint3x4(vertexOS);
                    var vertexRS = matrix.MultiplyPoint3x4(vertexWS);

                    bounds.Encapsulate(vertexRS);
                }
            }

            var radius = Vector3.Distance(bounds.min, bounds.max) * 0.5f;

            return new BoundingSphere(bounds.center, radius);
        }

        static Renderer[] CollectRenderers(GameObject gameObject)
        {
            var lodGroups = gameObject.GetComponentsInChildren<LODGroup>();

            // If not lods are available simply take all renderers
            if (lodGroups.Length == 0)
            {
                return gameObject.GetComponentsInChildren<Renderer>();
            }

            // Find renderers from other levels that will bed skipped
            var renderersToSkip = new HashSet<Renderer>();
            foreach (var lodGroup in lodGroups)
            {
                var lods = lodGroup.GetLODs();

                // Only skip if there is more than one lod
                if (lods.Length < 2)
                    continue;

                for (int i = 1; i < lods.Length; i++)
                {
                    foreach (var renderer in lods[i].renderers)
                    {
                        if (!renderersToSkip.Contains(renderer))
                            renderersToSkip.Add(renderer);
                    }
                }
            }

            var renderers = new List<Renderer>();
            foreach (var renderer in gameObject.GetComponentsInChildren<Renderer>())
            {
                if (!renderersToSkip.Contains(renderer))
                    renderers.Add(renderer);
            }
            return renderers.ToArray();
        }
    }
}