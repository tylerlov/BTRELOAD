using UnityEngine;

using Raymarcher.Objects.Volumes;
using Raymarcher.Utilities;
using Raymarcher.Attributes;

namespace Raymarcher.Toolkit
{
    using static RMAttributes;
    using static RMVolumeUtils;
    using static RMTextureUtils;

    [ExecuteAlways]
    public sealed class RMVolumeParticleTracker : MonoBehaviour
    {
        // Serialized Fields

        [Space]
        [Required] public ParticleSystem targetParticleSystem;
        [Required] public RMSdf_Tex3DVolumeBox targetVolumeBoxTex3D;
        [Space]
        public bool updateEveryFrame = true;
        public CommonVolumeResolution volumeResolution = CommonVolumeResolution.x64;
        public float pointSize = 1;
        [Range(0f, 1f)] public float pointSmoothness01 = 1.0f;
        [Required, Range(MIN_PARTICLES, MAX_PARTICLES), Tooltip("The higher the number, the more performance and allocations it consumes")]
        public int maxParticles = 128;
        [Space]
        [SerializeField, Button("Point/ Bilinear Filtering", "SwitchFiltering")] private bool BUTTON_TEMP0;
        [Space]
        [SerializeField, Button("Initialize Canvas Manually", "InitializeCanvas")] private bool BUTTON_TEMP1;
        [SerializeField, Button("Update Particles Manually", "UpdateParticleTracker")] private bool BUTTON_TEMP2;
        [Space]
        [SerializeField, Button("Clear Particles", "ClearParticleTracker")] private bool BUTTON_TEMP3;
        [SerializeField, HideInInspector] private CommonVolumeResolution cachedVolumeResolution;
        [SerializeField, HideInInspector] private bool pointFiltering = false;

        // Properties

        public int CurrentResolution { get; private set; }

        // Constants

        private const string COMPUTE_NAME = "RMTex3DParticleTrackerCompute";
        private const string COMPUTE_KERNEL0_NAME = "ParticleTracker";
        private const string COMPUTE_KERNEL1_NAME = "ParticleClear";
        private const string COMPUTE_TEX3D = "Tex3DInput";
        private const string COMPUTE_PARTICLES = "ParticlesInput";
        private const string COMPUTE_RADIUS = "PointRadius";
        private const string COMPUTE_SMOOTHNESS = "PointSmoothness";
        private const string COMPUTE_TEXRES = "TexRes";
        private const string COMPUTE_PCOUNT = "ParticleCount";

        private const int THREAD_GROUPS = 8;
        private const int MIN_PARTICLES = 1;
        private const int MAX_PARTICLES = 1024;

        // Privates

        private ParticleSystem.Particle[] particles;
        private Vector4[] particlePositions;

        private RenderTexture canvasRenderTexture;
        private int computeShaderKernelID_Tracker;
        private int computeShaderKernelID_Clear;
        private ComputeShader volumeComputeShader;
        private ComputeBuffer particlesBuffer;

        private int threadGroupWorker;

        private void OnDisable()
        {
            if (particlesBuffer != null)
                particlesBuffer.Release();
            particlesBuffer = null;
            particlePositions = null;
            particles = null;
        }

        private void OnDestroy()
        {
            if (canvasRenderTexture != null)
                canvasRenderTexture.Release();
            if (volumeComputeShader != null)
            {
                if (Application.isPlaying)
                    Destroy(volumeComputeShader);
                else
                    DestroyImmediate(volumeComputeShader);
            }
        }

        public void InitializeCanvas()
        {
            var shaderResource = Resources.Load<ComputeShader>(COMPUTE_NAME);
            if (shaderResource == null)
            {
                RMDebug.Debug(this, "Couldn't find a compute shader for modifying a 3D render texture while initializing the volume particle tracker canvas", true);
                return;
            }
            if (targetVolumeBoxTex3D == null)
            {
                RMDebug.Debug(this, $"Couldn't initialize the painting canvas. '{nameof(targetVolumeBoxTex3D)}' is null!", true);
                return;
            }
            if(volumeComputeShader != null)
            {
                if (Application.isPlaying)
                    Destroy(volumeComputeShader);
                else
                    DestroyImmediate(volumeComputeShader);
            }
            volumeComputeShader = Instantiate(shaderResource);
            volumeComputeShader.name = COMPUTE_NAME;

            cachedVolumeResolution = volumeResolution;
            CurrentResolution = GetCommonVolumeResolution(volumeResolution);

            if (canvasRenderTexture != null)
                canvasRenderTexture.Release();

            threadGroupWorker = CurrentResolution / THREAD_GROUPS;

            canvasRenderTexture = CreateDynamic3DRenderTexture(CurrentResolution, name);

            targetVolumeBoxTex3D.VolumeTexture = canvasRenderTexture;
            targetVolumeBoxTex3D.transform.rotation = Quaternion.identity;

            computeShaderKernelID_Tracker = volumeComputeShader.FindKernel(COMPUTE_KERNEL0_NAME);
            computeShaderKernelID_Clear = volumeComputeShader.FindKernel(COMPUTE_KERNEL1_NAME);
            volumeComputeShader.SetTexture(computeShaderKernelID_Tracker, COMPUTE_TEX3D, canvasRenderTexture);
            volumeComputeShader.SetInt(COMPUTE_TEXRES, CurrentResolution);
            if(particles != null)
                volumeComputeShader.SetInt(COMPUTE_PCOUNT, particles.Length);

            ChangeFiltering(!pointFiltering);
        }

        public void InitializeParticles()
        {
            if (targetParticleSystem == null)
                return;

            maxParticles = Mathf.Clamp(maxParticles, MIN_PARTICLES, MAX_PARTICLES);
            var maxParts = targetParticleSystem.main;
            maxParts.maxParticles = maxParticles;

            particles = new ParticleSystem.Particle[maxParticles];
            particlePositions = new Vector4[particles.Length];

            if(volumeComputeShader != null)
                volumeComputeShader.SetInt(COMPUTE_PCOUNT, particles.Length);
            if (particlesBuffer != null)
                particlesBuffer.Release();
            particlesBuffer = new ComputeBuffer(particles.Length, sizeof(float) * 4);
        }

        public void SwitchFiltering()
        {
            if (canvasRenderTexture != null)
                ChangeFiltering(canvasRenderTexture.filterMode == FilterMode.Bilinear ? false : true);
        }

        public void ChangeFiltering(bool toBilinear)
        {
            if (canvasRenderTexture != null)
                canvasRenderTexture.filterMode = toBilinear ? FilterMode.Bilinear : FilterMode.Point;
            bool newFiltering = !toBilinear;
#if UNITY_EDITOR
            if (pointFiltering != newFiltering)
            {
                pointFiltering = newFiltering;
                UnityEditor.EditorUtility.SetDirty(this);
            }
#endif
        }

        public void UpdateParticleTracker()
        {
            if (targetVolumeBoxTex3D == null)
                return;
            if (targetParticleSystem == null)
                return;

            if (canvasRenderTexture == null || volumeComputeShader == null || cachedVolumeResolution != volumeResolution)
            {
                InitializeCanvas();
                return;
            }

            if (particles == null || particles.Length != maxParticles || targetParticleSystem.main.maxParticles != maxParticles || particlesBuffer == null)
                InitializeParticles();

            if (!targetParticleSystem.isPlaying)
                return;

            for (int i = 0; i < targetParticleSystem.GetParticles(particles); i++)
            {
                Vector4 particleData = ConvertWorldToVolumeTextureSpace(
                    targetParticleSystem.main.simulationSpace == ParticleSystemSimulationSpace.World ?
                    particles[i].position : targetParticleSystem.transform.TransformPoint(particles[i].position), targetVolumeBoxTex3D, CurrentResolution);
                particleData.w = particles[i].remainingLifetime > 0 ? particles[i].GetCurrentSize(targetParticleSystem) : 0;
                particlePositions[i] = particleData;
            }

            volumeComputeShader.SetFloat(COMPUTE_RADIUS, Mathf.Abs(pointSize));
            volumeComputeShader.SetFloat(COMPUTE_SMOOTHNESS, Mathf.Clamp01(1 - pointSmoothness01));

            particlesBuffer.SetData(particlePositions);
            volumeComputeShader.SetBuffer(computeShaderKernelID_Tracker, COMPUTE_PARTICLES, particlesBuffer);
            
            volumeComputeShader.Dispatch(computeShaderKernelID_Tracker, threadGroupWorker, threadGroupWorker, threadGroupWorker);
        }

        public void ClearParticleTracker()
        {
            if(volumeComputeShader != null && canvasRenderTexture != null)
            {
                volumeComputeShader.SetTexture(computeShaderKernelID_Clear, COMPUTE_TEX3D, canvasRenderTexture);
                volumeComputeShader.Dispatch(computeShaderKernelID_Clear, threadGroupWorker, threadGroupWorker, threadGroupWorker);
            }

            if (targetParticleSystem == null)
                return;

            targetParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            InitializeParticles();
        }

        private void LateUpdate()
        {
            if (updateEveryFrame)
                UpdateParticleTracker();
        }
    }
}