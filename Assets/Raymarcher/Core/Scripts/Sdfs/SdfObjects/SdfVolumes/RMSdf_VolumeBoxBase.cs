using UnityEngine;

namespace Raymarcher.Objects.Volumes
{
    /// <summary>
    /// Base class for all volume boxes in the Raymarcher.
    /// Inherit from this class to create a volume box with common fields.
    /// </summary>
    public abstract class RMSdf_VolumeBoxBase : RMSdfObjectBase
    {
        [Space]
        [Range(0.1f, 64f)] public float volumeSize = 1;
        [Range(1.0f, 64f)] public float volumeAmplifier = 1.5f;
        [Range(1.0f, 256f)] public float volumePrecision = 64f;

        private const string VOLUME_SIZE = nameof(volumeSize);
        private const string VOLUME_AMPLIFY = nameof(volumeAmplifier);
        private const string VOLUME_PRECIS = nameof(volumePrecision);

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(Vector3.zero, Vector3.one * (volumeSize * 2));
        }
#endif

        public override ISDFEntity.SDFUniformField[] SdfUniformFields => new ISDFEntity.SDFUniformField[3]
        {
            new ISDFEntity.SDFUniformField(VOLUME_SIZE, ISDFEntity.SDFUniformType.Float3),
            new ISDFEntity.SDFUniformField(VOLUME_AMPLIFY, ISDFEntity.SDFUniformType.Float),
            new ISDFEntity.SDFUniformField(VOLUME_PRECIS, ISDFEntity.SDFUniformType.Float)
        };

        public override void PushSdfEntityToShader(in Material raymarcherSceneMaterial, in string iterationIndex)
        {
            raymarcherSceneMaterial.SetVector(VOLUME_SIZE + iterationIndex, Vector3.one * volumeSize);
            raymarcherSceneMaterial.SetFloat(VOLUME_AMPLIFY + iterationIndex, volumeAmplifier);
            raymarcherSceneMaterial.SetFloat(VOLUME_PRECIS + iterationIndex, volumePrecision);
        }
    }
}