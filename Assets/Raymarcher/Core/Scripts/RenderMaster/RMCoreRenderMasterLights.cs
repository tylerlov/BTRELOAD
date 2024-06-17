using System;
using System.Collections.Generic;

using UnityEngine;

using Raymarcher.Constants;

namespace Raymarcher.RendererData
{
    using static RMConstants.CommonRendererProperties;

    [Serializable]
    public sealed class RMCoreRenderMasterLights : IRMRenderMasterDependency
    {
        // Serialized fields

        [SerializeField] private bool useMainDirectionalLight = true;
        public Light mainDirectionalLight;
        [Range(1.0e-5f, 10f)] public float mainDirectionalLightDamping = 1f;

        [SerializeField] private bool useAdditionalLights = true;
        [Range(1.0e-5f, 10f)] public float addLightsDamping = 1f;
        [SerializeField] private List<AdditionalLightData> additionalLightsCollection = new List<AdditionalLightData>();

        [Serializable]
        public sealed class AdditionalLightData
        {
            public Light lightSource;
            [Space]
            public float lightIntensityMultiplier = 1.0f;
            public float lightRangeMultiplier = 1.0f;
            [Space]
            public float shadowIntensityOverride = 1.0f;
            public float shadowAttenuationOffset = 0.0f;
        }

        [SerializeField] private int additionalLightsCompiledCount;

        [SerializeField] private RMRenderMaster renderMaster;

        // Properties

        public RMRenderMaster RenderMaster => renderMaster;
        public bool UseMainDirectionalLight => useMainDirectionalLight;
        public bool UseAdditionalLights => useAdditionalLights;
        public List<AdditionalLightData> AdditionalLightsCollection => additionalLightsCollection;
        public int GetAdditionalLightsCompiledCount => additionalLightsCompiledCount;

        public void SetAdditionalLightsCachedCount()
            => additionalLightsCompiledCount = additionalLightsCollection.Count;

#if UNITY_EDITOR

        public void SetupDependency(in RMRenderMaster renderMaster)
        {
            this.renderMaster = renderMaster;

            Light[] directionalLight = GameObject.FindObjectsOfType<Light>(true);

            if (directionalLight.Length > 0)
            {
                foreach(Light light in directionalLight)
                {
                    if(light.type == LightType.Directional)
                    {
                        mainDirectionalLight = light;
                        break;
                    }
                }
            }
        }

        public void DisposeDependency()
        {
            mainDirectionalLight = null;
            additionalLightsCollection = null;
        }

#endif

        public void UpdateDependency(in Material raymarcherSessionMaterial)
        {
            if (useMainDirectionalLight)
            {
                Light mainLight = mainDirectionalLight;
                bool isNullorInActive = mainLight == null || !mainLight.gameObject.activeInHierarchy;
                Vector3 mainLightDir = isNullorInActive ? Vector3.down : mainLight.transform.forward;
                raymarcherSessionMaterial.SetVector(DirectionalLightDirection, mainLightDir);

                Color mainLightCol = isNullorInActive ? Color.white : mainLight.color;
                mainLightCol.a = isNullorInActive ? 1 : mainLight.intensity * mainDirectionalLightDamping;
                raymarcherSessionMaterial.SetColor(DirectionalLightColor, mainLightCol);
            }

            if (useAdditionalLights && additionalLightsCollection.Count > 0)
            {
                Vector4[] addLights = new Vector4[additionalLightsCollection.Count * 3];
                int ident = 0;
                bool hasZeroLightSource = false;
                for (int i = 0; i < additionalLightsCollection.Count; i++)
                {
                    AdditionalLightData lData = additionalLightsCollection[i];

                    if (lData.lightSource == null)
                    {
                        hasZeroLightSource = true;
                        break;
                    }

                    Light l = lData.lightSource;
                    bool isActive = l.enabled && l.gameObject.activeInHierarchy;
                    float shadowStrenght = l.shadows == LightShadows.None || !isActive ? 0f : l.shadowStrength;
                    Transform lTrans = l.transform;
                    Vector3 lPos = lTrans.position;
                    addLights[ident] = new Vector4(lPos.x, lPos.y, lPos.z, (l.intensity * Mathf.Abs(lData.lightIntensityMultiplier) * addLightsDamping) * (isActive ? 1.0f : 0.0f));
                    addLights[ident + 1] = new Vector4(l.color.r, l.color.g, l.color.b, l.range * Mathf.Abs(lData.lightRangeMultiplier) * (isActive ? 1.0f : 0.0f));
                    addLights[ident + 2] = new Vector4(shadowStrenght * Mathf.Abs(lData.shadowIntensityOverride), Mathf.Clamp(lData.shadowAttenuationOffset, 0, 999));
                    ident += 3;
                }
                if (!hasZeroLightSource)
                    raymarcherSessionMaterial.SetVectorArray(AdditionalLightsData, addLights);
            }
        }
    }
}