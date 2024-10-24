#if URP_INSTALLED
using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace JPG.Universal
{
    [ExecuteInEditMode, VolumeComponentMenu("JPG - Bitcrunching Effect")] //JPG - Bitcrunching / JPEG / H264 Effect
    public class JPG : VolumeComponent, IPostProcessComponent
    {
        private float currentIntensity;  // Add this field

        public enum _BlockSize { [InspectorName("4x4 Fast")]_4x4, [InspectorName("8x8 Medium")]_8x8, [InspectorName("16x16 Slow")]_16x16 }
        [Serializable] public sealed class BlockSizeParameter : VolumeParameter<_BlockSize> { public BlockSizeParameter(_BlockSize value, bool overrideState = false) : base(value, overrideState) { } }
        
        [Tooltip("Gracefully reduces these together: Color Crunch, Downscaling, Block Size, Reproject Percent, Reproject Length Influence." +
            "\n\nFor example this can be useful to lerp out the effect by lerping out this one parameter instead of all the other ones." +
            "\n\nWarning: When scrolling through this parameter there may be numbers that land on extra low performance, because 16x16 block size + a low Downscaling setting is a very demanding combo." +
            "\nQuickly lerp away from these (like animating out the effect), or set block size to 8x8 to avoid this.")] public ClampedFloatParameter EffectIntensity = new ClampedFloatParameter(0.35f, 0f, 1f);
        [Tooltip("When enabled effect will be applied only to pixels with a stencil buffer value of '32'. Shaders that set it to this value are provided in /StencilShaders." +
            "\n\nFor example if you want the effect to only be applied to specific objects, enable this and put a +JPG shader on them." +
            "\n\nFor custom shaders it is extremely easy to modify them to become JPG maskers, see README Stencil.txt.")] public BoolParameter OnlyStenciled = new BoolParameter(false);

        [Space(6)]
        [Header("Block Encoding")]
        [Tooltip("A bit of color crunching really brings out the noise from the blocks.")] public ClampedFloatParameter ColorCrunch = new ClampedFloatParameter(1f, 0f, 1f);
        [Tooltip("Division applied to screen resolution before applying the effect.")] public ClampedIntParameter Downscaling = new ClampedIntParameter(10, 1, 10);
        [Tooltip("The size of the encoding blocks, works in conjunction with Downscaling." +
            "\n\nWarning: 16x16 will be very slow at a low Downscaling setting, so when you're scrolling through Effect Intensity there may be numbers that land on extra low performance." +
            "\nQuickly lerp away from them (like animating out the effect), or set block size to 8x8 to avoid this.")] public BlockSizeParameter BlockSize = new BlockSizeParameter(_BlockSize._16x16);
        [Space(6)]
        [Tooltip("Add some oversharpening if you're going for a deep-fried look. Not affected by Effect Intensity.")] public ClampedFloatParameter Oversharpening = new ClampedFloatParameter(0.2f, 0f, 1f);
        [Tooltip("Does color crunching try to ignore the skybox? (aka pixels at depth values 0.0 or 1.0).")] public BoolParameter DontCrunchSkybox = new BoolParameter(false);
        
        [Space(6)]
        [Header("Datamoshing Reprojection")]
        [Tooltip("Base chance of each pixel block to be randomly selected for reprojection.\nDatamoshing is disabled when this and Length Influence are 0.\nDatamoshing enables motion vectors generation if wasn't enabled already."), InspectorName("Base Noise")] public ClampedFloatParameter ReprojectBaseNoise = new ClampedFloatParameter(0f, 0f, 1f);
        [Tooltip("How many times per second base noise is rerolled."), InspectorName("Base Reroll Speed")] public ClampedFloatParameter ReprojectBaseRerollSpeed = new ClampedFloatParameter(3f, 0f, 20f);
        [Tooltip("How much does the length of a motion vector increase it's chance of being selected on top of base noise."), InspectorName("Length Influence")] public ClampedFloatParameter ReprojectLengthInfluence = new ClampedFloatParameter(0f, 0f, 5f);
        [Tooltip("Useful for debugging, as an object will only get datamoshed if it's writing to motion vectors.")] public BoolParameter VisualizeMotionVectors = new BoolParameter(false);

        public bool IsActive() => EffectIntensity.value > 0f;

        public bool IsTileCompatible() => true;

        public float GetCurrentIntensity()
        {
            return EffectIntensity.value; // Changed to return the actual effect intensity instead of the undefined field
        }
    }
}
#endif
