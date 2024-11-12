using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mirage.Impostors.Core {
    public class ImpostorGeometrySolver
    {
        public ImpostorPreset Preset { get; private set; }
        public int POVNumber { get {
                if (Dirty)
                    Update();
                return povNb;
            } }
        public bool Dirty { get { return lastPresets != Preset;  } }

        public int Subdivisions { get { return Mathf.CeilToInt(Mathf.Sqrt(POVNumber)); } }
       
        private int povNb;
        private ImpostorPreset lastPresets;
        private int[] latitudeFibonacciClusters;

        public ImpostorGeometrySolver(ref ImpostorPreset settings)
        {
            Preset = settings;
            Update();
        }

        void Update()
        {
            if (Preset.FibonacciSphere)
            {
                latitudeFibonacciClusters = new int[1 + Preset.latitudeSamples * 2];
                for (int i = 0; i <= Preset.latitudeSamples; i++)
                {
                    latitudeFibonacciClusters[Preset.latitudeSamples + i] = Mathf.RoundToInt(Mathf.Cos(i * Mathf.PI / (2f * (Preset.latitudeSamples + 1))) * Preset.longitudeSamples);
                    latitudeFibonacciClusters[Preset.latitudeSamples - i] = Mathf.RoundToInt(Mathf.Cos(i * Mathf.PI / (2f * (Preset.latitudeSamples + 1))) * Preset.longitudeSamples);
                }
                povNb = 0;
                foreach (int i in latitudeFibonacciClusters)
                {
                    povNb += i;
                }
            }
            else
                povNb = Preset.longitudeSamples * (1 + 2 * Preset.latitudeSamples);
            lastPresets = ImpostorPreset.Clone(Preset);
        }

        public List<Vector3> GetNormalizedPOVs()
        {
            if (Dirty)
                Update();
            List<Vector3> cameraPOVs = new List<Vector3>();
            for (int i = 0; i < POVNumber; ++i)
                cameraPOVs.Add(GetNormalizedCameraPosition(i));
            return cameraPOVs;
        }
        public List<Vector3> GetPOVs(Mesh mesh)
        {
            if (Dirty)
                Update();
            List<Vector3> cameraPOVs = new List<Vector3>();
            for (int i = 0; i < POVNumber; ++i)
                cameraPOVs.Add(GetCameraPosition(mesh, i));
            return cameraPOVs;
        }
        public List<Vector3> GetPOVs(float distance)
        {
            if (Dirty)
                Update();
            List<Vector3> cameraPOVs = new List<Vector3>();
            for (int i = 0; i < POVNumber; ++i)
                cameraPOVs.Add(GetNormalizedCameraPosition(i) * distance);
            return cameraPOVs;
        }

        /// <summary>
        /// Helper method to get the camera position for a given Mesh
        /// </summary>
        public Vector3 GetCameraPosition(Mesh mesh, int iteration)
        {
            return GetNormalizedCameraPosition(iteration) * (mesh.bounds.size.magnitude + 0.01f);
        }

        public Vector3 GetNormalizedCameraPosition(int iteration)
        {
            int subdivisionMod = (iteration) % (Subdivisions * Subdivisions);
            if (Preset.FibonacciSphere)
            {
                int cumulativeLength = 0;
                float elevation = 0f;
                float azimut = 0f;
                for (int i = 0; i < latitudeFibonacciClusters.Length; ++i)
                {
                    cumulativeLength += latitudeFibonacciClusters[i];
                    if (cumulativeLength > subdivisionMod)
                    {
                        elevation = (i - Preset.latitudeSamples) * Preset.latitudeAngularStep * Mathf.Deg2Rad;
                        azimut = (subdivisionMod - cumulativeLength + latitudeFibonacciClusters[i]) / (float)(latitudeFibonacciClusters[i]) * Mathf.PI * 2f;
                        break;
                    }
                }
                return new Vector3(Mathf.Cos(azimut) * Mathf.Cos(elevation), Mathf.Sin(elevation), Mathf.Sin(azimut) * Mathf.Cos(elevation));
            }
            else
            {
                float azimut = (subdivisionMod % Preset.longitudeSamples) / (float)Preset.longitudeSamples * 2f * Mathf.PI;
                float elevation = (Mathf.FloorToInt(subdivisionMod / (float)Preset.longitudeSamples) - Preset.latitudeSamples + Preset.latitudeOffset) * Preset.latitudeAngularStep * Mathf.Deg2Rad;
                return new Vector3(Mathf.Cos(azimut) * Mathf.Cos(elevation), Mathf.Sin(elevation), Mathf.Sin(azimut) * Mathf.Cos(elevation));
            }
        }

        public Vector3 GetBoundsAwareCenter(MeshFilter meshFilter)
        {
            Bounds bds = meshFilter.sharedMesh.bounds;
            Vector3 rotatedCenter = meshFilter.transform.TransformVector(bds.center);
            float pivotLocalExtent = bds.center.magnitude;
            rotatedCenter = rotatedCenter.normalized * pivotLocalExtent;
            Vector3 pivotExentricity = Vector3.ProjectOnPlane(rotatedCenter, Vector3.up);
            return new Vector3(-pivotExentricity.x, meshFilter.transform.position.y - meshFilter.GetComponent<MeshRenderer>().bounds.center.y * meshFilter.transform.localScale.y, -pivotExentricity.z);
        }

        public float GetOrthgraphicCameraSize(Mesh mesh)
        {
            Bounds bds = mesh.bounds;
            return bds.extents.magnitude;
        }

        public float GetOrthgraphicCameraSize(List<MeshFilter> meshFilters)
        {
            return GetBounds(meshFilters).extents.magnitude;
        }

        public Vector3 GetPivotExcentricity(MeshFilter meshFilter)
        {

            Mesh mesh = meshFilter.sharedMesh;
            Bounds bds = mesh.bounds;
            Vector3 rotatedCenter = meshFilter.transform.TransformVector(bds.center);
            float pivotLocalExtent = bds.center.magnitude;
            rotatedCenter = rotatedCenter.normalized * pivotLocalExtent;
            return Vector3.ProjectOnPlane(rotatedCenter, Vector3.up);
        }
        
        public Bounds GetBounds(List<MeshFilter> meshFilters)
        {
            if (meshFilters.Count == 0 || meshFilters[0] == null)
            {
                return new Bounds();
            }
            Bounds initBounds = meshFilters[0].GetComponent<MeshRenderer>().bounds;
            Vector3 min = initBounds.min;
            Vector3 max = initBounds.max;

            foreach (MeshFilter filter in meshFilters)
            {
                if (filter == null)
                    continue;
                MeshRenderer renderer = filter.GetComponent<MeshRenderer>();
                Bounds bounds = renderer.bounds;
                min = Vector3.Min(min, bounds.min);
                max = Vector3.Max(max, bounds.max);
            }

            Bounds combinedBounds = new Bounds();
            combinedBounds.SetMinMax(min, max);

            return combinedBounds;
        }
    }    
}
