using UnityEngine;

using Raymarcher.Constants;
using Raymarcher.RendererData;

namespace Raymarcher
{
    /// <summary>
    /// EXPERIMENTAL version of the RM presets.
    /// This component is still in a development and is a subject to change in the future...
    /// </summary>
    [CreateAssetMenu(fileName = nameof(RMRenderMasterPresetObject), menuName = RMConstants.RM_EDITOR_ROOT_PATH + "Render Master Preset")]
    public sealed class RMRenderMasterPresetObject : ScriptableObject
    {
        [Space]
        [Header("RM Preset Object (Experimental)")]
        public RMRenderMaster.TargetPlatform targetPlatform;

        public RMCoreRenderMasterRenderingData renderMasterRenderingData;

        public RMCoreRenderMasterLights renderMasterLights;

        public RMCoreRenderMasterMaterials renderMasterMaterials;
    }
}