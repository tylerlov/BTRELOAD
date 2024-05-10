using UnityEngine.Rendering;
using UnityEngine;

namespace OccaSoftware.LocalGI.Runtime
{
    public class IrradianceData
    {
        private Material diffuseConvolutionMaterial;

        /// <summary>
        /// This is a Material that represents a shader program for performing convolution on a cubemap texture.
        /// It is lazily initialized when accessed via the DiffuseConvolutionMaterial property getter, which creates a new material object if one does not exist yet.
        /// The material is located by its shader name Hidden/CubemapDiffuseConvolve.
        /// </summary>
        public Material DiffuseConvolutionMaterial
        {
            get
            {
                if (diffuseConvolutionMaterial == null)
                {
                    diffuseConvolutionMaterial = new Material(
                        Shader.Find("Hidden/CubemapDiffuseConvolve")
                    );
                }

                return diffuseConvolutionMaterial;
            }
        }

        /// <summary>
        /// This is a static class that holds integer values corresponding to shader property identifiers used by DiffuseConvolutionMaterial. It contains two properties: _Exposure and _EnvironmentMap.
        /// </summary>
        private static class ShaderParams
        {
            public static int _Exposure = Shader.PropertyToID("_Exposure");
            public static int _EnvironmentMap = Shader.PropertyToID("_EnvironmentMap");
        }

        /// <summary>
        /// This method sets the values of _EnvironmentMap and _Exposure properties in DiffuseConvolutionMaterial. The _EnvironmentMap property is set to the given environmentMap texture, and the _Exposure property is set to the given exposure value.
        /// </summary>
        /// <param name="environmentMap"></param>
        /// <param name="exposure"></param>
        public void SetShaderParams(Texture environmentMap, float exposure)
        {
            DiffuseConvolutionMaterial.SetTexture(ShaderParams._EnvironmentMap, environmentMap);
            DiffuseConvolutionMaterial.SetFloat(ShaderParams._Exposure, exposure);
        }

        private CustomRenderTexture irradianceTexture;

        /// <summary>
        /// This is a CustomRenderTexture object that represents a cubemap texture used for storing precomputed diffuse irradiance. It is lazily initialized when accessed via the IrradianceTexture property getter, which creates a new CustomRenderTexture object if one does not exist yet. The texture is created with specific parameters such as texture size, format, dimension, filtering mode, and update mode. Its initialization mode is set to OnDemand, which means that the texture is only updated when it is requested. The texture is initialized with the DiffuseConvolutionMaterial material and its global shader property is set to _DiffuseIrradianceData.
        /// </summary>
        public CustomRenderTexture IrradianceTexture
        {
            get
            {
                if (irradianceTexture == null)
                {
                    irradianceTexture = new CustomRenderTexture(
                        Common.IrradianceResolution,
                        Common.IrradianceResolution,
                        UnityEngine.Experimental.Rendering.DefaultFormat.HDR
                    );
                    irradianceTexture.dimension = TextureDimension.Cube;
                    irradianceTexture.anisoLevel = 0;
                    irradianceTexture.antiAliasing = 1;
                    irradianceTexture.autoGenerateMips = false;
                    irradianceTexture.depth = 0;
                    irradianceTexture.filterMode = FilterMode.Bilinear;
                    irradianceTexture.initializationMode = CustomRenderTextureUpdateMode.OnDemand;
                    irradianceTexture.updateMode = CustomRenderTextureUpdateMode.OnDemand;
                    irradianceTexture.initializationSource =
                        CustomRenderTextureInitializationSource.Material;
                    irradianceTexture.initializationMaterial = DiffuseConvolutionMaterial;
                    irradianceTexture.Create();

                    RenderTexture activeRenderTexture = RenderTexture.active;
                    RenderTexture.active = irradianceTexture;
                    GL.Clear(true, true, Color.black);
                    RenderTexture.active = activeRenderTexture;
                }

                return irradianceTexture;
            }
        }

        /// <summary>
        /// This is a constructor for the IrradianceData class. It does not take any arguments and does not perform any initialization.
        /// </summary>
        public IrradianceData() { }
    }
}
