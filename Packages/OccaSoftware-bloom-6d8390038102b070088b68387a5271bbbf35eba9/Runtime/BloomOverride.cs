using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace OccaSoftware.Bloom.Runtime
{
  [
    Serializable,
    VolumeComponentMenuForRenderPipeline("OccaSoftware/Bloom", typeof(UniversalRenderPipeline))
  ]
  public sealed class BloomOverride : VolumeComponent, IPostProcessComponent
  {
    public BoolParameter enabled = new BoolParameter(false);

    [Header("Bloom")]
    public FloatParameter internalBlend = new ClampedFloatParameter(0.85f, 0, 1);
    public FloatParameter finalBlend = new ClampedFloatParameter(0.02f, 0, 1);
    public IntParameter bloomMaxIterations = new ClampedIntParameter(6, 2, 9);

    [Header("Thresholds")]
    public FloatParameter thresholdEdge = new MinFloatParameter(1f, 0);
    public FloatParameter thresholdRange = new MinFloatParameter(32f, 0);

    [Header("Ghost")]
    public FloatParameter ghostIntensity = new MinFloatParameter(0.3f, 0);
    public ColorParameter ghostTint = new ColorParameter(Color.white, false, false, true);
    public Vector3Parameter ghostChromaSpread = new Vector3Parameter(
      new Vector3(-0.0213f, 0.0f, 0.032f)
    );

    // Prefill ghostTint and ghostSpread
    private static readonly float[] distances =
    {
      -0.142f,
      0.1f,
      -0.7f,
      0.78f,
      0.23f,
      -0.1235f,
      0.53f,
      -0.412f
    };
    private static readonly Color[] colors =
    {
      new Color(0.623f, 0.145f, 0.894f), // Purple
      new Color(0.231f, 0.827f, 0.384f), // Green
      new Color(0.956f, 0.478f, 0.129f), // Orange
      new Color(0.094f, 0.654f, 0.862f), // Blue
      new Color(0.811f, 0.243f, 0.678f), // Pink
      new Color(0.427f, 0.792f, 0.156f), // Lime
      new Color(0.921f, 0.305f, 0.058f), // Red
      new Color(0.956f, 0.478f, 0.129f) // Orange
    };

    public ColorParameter ghostTint1 = new ColorParameter(colors[0], false, false, true);
    public FloatParameter ghostSpread1 = new FloatParameter(distances[0]);
    public ColorParameter ghostTint2 = new ColorParameter(colors[1], false, false, true);
    public FloatParameter ghostSpread2 = new FloatParameter(distances[1]);
    public ColorParameter ghostTint3 = new ColorParameter(colors[2], false, false, true);
    public FloatParameter ghostSpread3 = new FloatParameter(distances[2]);
    public ColorParameter ghostTint4 = new ColorParameter(colors[3], false, false, true);
    public FloatParameter ghostSpread4 = new FloatParameter(distances[3]);
    public ColorParameter ghostTint5 = new ColorParameter(colors[4], false, false, true);
    public FloatParameter ghostSpread5 = new FloatParameter(distances[4]);
    public ColorParameter ghostTint6 = new ColorParameter(colors[5], false, false, true);
    public FloatParameter ghostSpread6 = new FloatParameter(distances[5]);
    public ColorParameter ghostTint7 = new ColorParameter(colors[6], false, false, true);
    public FloatParameter ghostSpread7 = new FloatParameter(distances[6]);
    public ColorParameter ghostTint8 = new ColorParameter(colors[7], false, false, true);
    public FloatParameter ghostSpread8 = new FloatParameter(distances[7]);

    [Header("Halo")]
    public FloatParameter haloIntensity = new MinFloatParameter(0.3f, 0);
    public FloatParameter haloFisheyeStrength = new MinFloatParameter(0.5f, 0);
    public FloatParameter haloFisheyeWidth = new MinFloatParameter(0.4f, 0);
    public Vector3Parameter haloChromaSpread = new Vector3Parameter(
      new Vector3(-0.02314f, 0.0f, 0.04213f)
    );

    public ColorParameter haloTint = new ColorParameter(
      new Color(0.623f, 0.145f, 0.894f),
      false,
      false,
      true
    );

    Vector4[] ghostTints = new Vector4[8];
    float[] ghostSpreads = new float[8];

    public ref float[] PackGhostSpreads()
    {
      ghostSpreads[0] = ghostSpread1.value;
      ghostSpreads[1] = ghostSpread2.value;
      ghostSpreads[2] = ghostSpread3.value;
      ghostSpreads[3] = ghostSpread4.value;
      ghostSpreads[4] = ghostSpread5.value;
      ghostSpreads[5] = ghostSpread6.value;
      ghostSpreads[6] = ghostSpread7.value;
      ghostSpreads[7] = ghostSpread8.value;
      return ref ghostSpreads;
    }

    public ref Vector4[] PackGhostTints()
    {
      ghostTints[0] = ghostTint1.value;
      ghostTints[1] = ghostTint2.value;
      ghostTints[2] = ghostTint3.value;
      ghostTints[3] = ghostTint4.value;
      ghostTints[4] = ghostTint5.value;
      ghostTints[5] = ghostTint6.value;
      ghostTints[6] = ghostTint7.value;
      ghostTints[7] = ghostTint8.value;
      return ref ghostTints;
    }

    public bool IsActive()
    {
      if (!enabled.value)
        return false;

      return true;
    }

    public bool IsTileCompatible() => false;
  }
}
