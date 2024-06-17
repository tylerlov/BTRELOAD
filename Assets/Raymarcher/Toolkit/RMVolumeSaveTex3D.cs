using UnityEngine;

using Raymarcher.Objects.Volumes;
using Raymarcher.Attributes;

namespace Raymarcher.Toolkit
{
    using static RMAttributes;

    public sealed class RMVolumeSaveTex3D : MonoBehaviour
    {
        [SerializeField, Required] private RMSdf_Tex3DVolumeBox targetTex3DVolumeBox;
        public RMSdf_Tex3DVolumeBox TargetTex3DVolumeBox => targetTex3DVolumeBox;
    }
}