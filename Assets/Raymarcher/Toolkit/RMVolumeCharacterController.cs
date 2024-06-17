using UnityEngine;

using Raymarcher.Attributes;
using Raymarcher.Objects.Volumes;
using Raymarcher.Utilities;

namespace Raymarcher.Toolkit
{
    using static RMVolumeUtils;
    using static RMTextureUtils;
    using static RMAttributes;

    public sealed class RMVolumeCharacterController : MonoBehaviour
    {
        // Serialized
        [Header("Initialization")]
        [SerializeField, Tooltip("If enabled, the volume character controller will be automatically initialized in 'OnAwake' for a static Tex3DVolumeBox (The Tex3D won't be modified)")]
        private bool autoInitForStaticVolumeBox = false;
        [SerializeField, ShowIf("autoInitForStaticVolumeBox", 1)] private RMSdf_Tex3DVolumeBox targetTex3DVolumeBox;
        [Header("Player Definition")]
        [Range(0.1f, 32f)] public float playerHeight = 1f;
        [Range(0.1f, 16f)] public float playerRadius = 0.5f;
        [Header("Player Collision")]
        [SerializeField, Range(6, MAX_RESOLUTION)] private int radialColliderResolution = 8;
        [SerializeField, Range(2, 64)] private int heightDepthIterations = 8;
        [SerializeField, Range(2, 16)] private int radialDepthIterations = 2;
        [SerializeField, Range(0f, 1f)] private float groundPixelThreshold = 0.2f;
        [SerializeField, Range(0f, 1f)] private float obstaclePixelThreshold = 0.2f;
        [Header("Player Physical Params")]
        [Range(0.1f, 8f)] public float movementSpeed = 2f;
        [Range(0.01f, 1f)] public float gravityForce = 0.5f;
        [Range(0.01f, 1f)] public float jumpForce = 0.25f;
        [Range(6f, 32f)] public float heightSmoothing = 8f;

        // Properties

        public bool IsInitialized { get; private set; }
        public bool IsGrounded { get; private set; }
        public bool IsCollidingWithObstacle { get; private set; }

        // Privates

        private Vector3 playerHeightVelocity;

        private RMVolumeDepthSampler depthSamplerHeight;
        private RMVolumeDepthSampler depthSamplerRadialCollision;
        private RenderTexture workingVolumeRT3D;
        private RMSdf_Tex3DVolumeBox targetVolumeBox;

        private float PlayerHeightHalf => playerHeight / 2f;
        private float PlayerHeightThird => playerHeight / 3f;
        private float AngleIncrement => 360f / radialColliderResolution;

        // Constants

        private const int MAX_RESOLUTION = 32;

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.green;

            const int capResolution = 8;
            const float capAngleIncrement = 90f / capResolution;

            Vector3 pos = transform.position;
            for (int i = 0; i < radialColliderResolution; i++)
            {
                Vector3 circlePoint = pos + (transform.position - GetPointOnCircle(i)).normalized * playerRadius;
                Vector3 top = Vector3.up * PlayerHeightHalf;
                Vector3 bot = Vector3.down * PlayerHeightHalf;
                Gizmos.DrawLine(circlePoint + bot, circlePoint + top);

                float a = i * AngleIncrement;
                for (int x = 0; x < capResolution; x++)
                {
                    Gizmos.DrawLine(pos + top + capPoint(x, a + 90), pos + top + capPoint(x + 1, a + 90));
                    Gizmos.DrawLine(pos + bot - capPoint(x, a - 90), pos + bot - capPoint(x + 1, a - 90));
                }
            }

            Vector3 capPoint(int x, float a)
            {
                float rad = Mathf.Deg2Rad * (x * capAngleIncrement);

                Vector3 point = Vector3.zero;
                point.y = playerRadius * Mathf.Cos(rad);
                point.z = playerRadius * Mathf.Sin(rad);
                float cosTh = Mathf.Cos(Mathf.Deg2Rad * a);
                float sinTh = Mathf.Sin(Mathf.Deg2Rad * a);

                Vector2 mat2 = new Vector2(
                    cosTh * point.x - sinTh * point.z,
                    sinTh * point.x + cosTh * point.z);

                return new Vector3(mat2.x, point.y, mat2.y);
            }
        }

        [UnityEditor.MenuItem(Constants.RMConstants.RM_EDITOR_OBJECT_TOOLKIT_PATH + "Volume Character Controller")]
        private static void CreateExtraInEditor()
        {
            GameObject go = new GameObject(nameof(RMVolumeCharacterController));
            go.AddComponent<RMVolumeCharacterController>();
            UnityEditor.Selection.activeObject = go;
        }
#endif

        private void Awake()
        {
            if (autoInitForStaticVolumeBox && targetTex3DVolumeBox != null)
                InitializeCharacter(targetTex3DVolumeBox);
        }

        public void InitializeCharacter(RMVolumeVoxelPainter existingVoxelPainter)
        {
            if (existingVoxelPainter == null)
            {
                RMDebug.Debug(this, "Target volume voxel painter is null!", true);
                return;
            }
            if (!existingVoxelPainter.IsInitialized)
            {
                RMDebug.Debug(this, "Target volume voxel painter is not initialized! Initialize the voxel volume painter first with all the required parameters", true);
                return;
            }
            InitializeCharacter(existingVoxelPainter.TargetTex3DVolumeBox, existingVoxelPainter.WorkingVolumeCanvas3D, existingVoxelPainter.CurrentCommonVolumeResolution);
        }

        public void InitializeCharacter(RMSdf_Tex3DVolumeBox targetTex3DVolumeBox, RenderTexture volumeRT3D, CommonVolumeResolution volumeCanvasResolution)
        {
            if (IsInitialized)
            {
                RMDebug.Debug(this, $"Volume Character Controller '{name}' is already initialized. Call '{nameof(UpdateCharacter)}' to update the character with a new target volume painter/ painting canvas", true);
                return;
            }

            int currentResolution = GetCommonVolumeResolution(volumeCanvasResolution);
            if (!CompareRT3DDimensions(volumeRT3D, currentResolution))
            {
                RMDebug.Debug(this, $"Volume Character Controller '{name}' couldn't be initialized", true);
                return;
            }

            depthSamplerHeight = new RMVolumeDepthSampler(volumeRT3D,
                heightDepthIterations, volumeCanvasResolution, groundPixelThreshold);
            depthSamplerRadialCollision = new RMVolumeDepthSampler(volumeRT3D,
                radialDepthIterations, volumeCanvasResolution, obstaclePixelThreshold);

            targetVolumeBox = targetTex3DVolumeBox;

            IsInitialized = true;
        }

        public void InitializeCharacter(RMSdf_Tex3DVolumeBox targetTex3DVolumeBox)
        {
            if (IsInitialized)
            {
                RMDebug.Debug(this, $"Volume Character Controller '{name}' is already initialized. Call '{nameof(UpdateCharacter)}' to update the character with a new target volume painter/ painting canvas", true);
                return;
            }

            if(targetTex3DVolumeBox.VolumeTexture == null)
            {
                RMDebug.Debug(this, $"Volume Character Controller '{name}' can't be initialized with '{targetTex3DVolumeBox.name}'. The target volume box doesn't have any 3D texture");
                return;
            }

            CommonVolumeResolution resolution;
            if(targetTex3DVolumeBox.VolumeTexture is RenderTexture rt3D)
            {
                if(!GetCommonVolumeResolutionFromRT3D(rt3D, out resolution))
                    return;
                InitializeCharacter(targetTex3DVolumeBox, rt3D, resolution);
            }
            else if(targetTex3DVolumeBox.VolumeTexture is Texture3D tex3D)
            {
                if (!GetCommonVolumeResolutionFromTex3D(tex3D, out resolution))
                    return;
                InitializeCharacter(targetTex3DVolumeBox, ConvertTexture3DToRenderTexture3D(tex3D), resolution);
            }
            else
            {
                RMDebug.Debug(this, $"Volume Character Controller '{name}' can't be initialized with '{targetTex3DVolumeBox.name}'. The target volume box volume texture is not a Render Texture 3D nor Texture3D", true);
                return;
            }
        }

        public void UpdateCharacter(RMVolumeVoxelPainter existingVoxelPainter)
        {
            if (existingVoxelPainter == null)
            {
                RMDebug.Debug(this, "Target volume voxel painter is null!", true);
                return;
            }
            if (!existingVoxelPainter.IsInitialized)
            {
                RMDebug.Debug(this, "Target volume voxel painter is not initialized! Initialize the voxel volume painter first with all the required parameters", true);
                return;
            }
            UpdateCharacter(existingVoxelPainter.TargetTex3DVolumeBox, existingVoxelPainter.WorkingVolumeCanvas3D, existingVoxelPainter.CurrentCommonVolumeResolution);
        }

        public void UpdateCharacter(RMSdf_Tex3DVolumeBox targetTex3DVolumeBox, RenderTexture volumeRT3D, CommonVolumeResolution volumeCanvasResolution)
        {
            if (!IsInitialized)
            {
                RMDebug.Debug(this, $"Volume Character Controller '{name}' is not initialized. Call '{nameof(InitializeCharacter)}' first", false);
                return;
            }

            int currentResolution = GetCommonVolumeResolution(volumeCanvasResolution);
            if (!CompareRT3DDimensions(volumeRT3D, currentResolution))
            {
                RMDebug.Debug(this, $"Volume Character Controller '{name}' couldn't be updated", true);
                return;
            }

            depthSamplerHeight.Update(volumeRT3D,
                new RMVolumeDepthSampler.RayTraceData(volumeCanvasResolution, groundPixelThreshold, heightDepthIterations));
            depthSamplerRadialCollision.Update(volumeRT3D,
                new RMVolumeDepthSampler.RayTraceData(volumeCanvasResolution, obstaclePixelThreshold, radialDepthIterations));

            targetVolumeBox = targetTex3DVolumeBox;
        }

        public void MoveCharacter(Vector3 moveInputDirection)
        {
            if (!IsInitialized)
            {
                RMDebug.Debug(this, $"Volume Character Controller '{name}' is not initialized. Call '{nameof(InitializeCharacter)}' first");
                return;
            }

            moveInputDirection *= Time.deltaTime * movementSpeed;

            HandleHeight();
            HandleRadialCollision(ref moveInputDirection);

            transform.position += moveInputDirection + playerHeightVelocity;
        }

        public void JumpCharacter()
        {
            if (IsGrounded)
                playerHeightVelocity.y += Mathf.Sqrt(jumpForce / 10f * gravityForce);
        }

        private Vector3 GetPointOnCircle(int index)
        {
            float radians = Mathf.Deg2Rad * (index * AngleIncrement);
            Vector3 point = transform.position;
            point.x += playerRadius * Mathf.Cos(radians);
            point.z += playerRadius * Mathf.Sin(radians);
            return point;
        }

        private void HandleRadialCollision(ref Vector3 moveDir)
        {
            Vector3 accumVec = Vector3.zero;
            for (int i = 0; i < radialColliderResolution; i++)
            {
                Vector3 dir = (transform.position - GetPointOnCircle(i)).normalized;
                if (depthSamplerRadialCollision.SampleVolumeDepth(out Vector3 _, transform.position + (Vector3.up * PlayerHeightThird), dir, targetVolumeBox, playerRadius))
                    accumVec += movementSpeed * Time.deltaTime * (-dir);
            }
            IsCollidingWithObstacle = accumVec.sqrMagnitude != 0;
            if (IsCollidingWithObstacle)
                moveDir = Vector3.Lerp(moveDir, accumVec, Time.deltaTime * (MAX_RESOLUTION - radialColliderResolution));
        }

        private void HandleHeight()
        {
            playerHeightVelocity.y = Mathf.Clamp(playerHeightVelocity.y, -0.3f, 5);

            if (depthSamplerRadialCollision.SampleVolumeDepth(out Vector3 _, transform.position + (Vector3.up * PlayerHeightHalf), Vector3.up, targetVolumeBox, 0.1f))
                playerHeightVelocity.y = 0;

            bool hit = depthSamplerHeight.SampleVolumeDepth(out Vector3 surfaceHit, transform.position, Vector3.down, targetVolumeBox, playerHeight);

            if (hit)
            {
                Vector3 p = transform.position;
                p.y = Mathf.Lerp(p.y, surfaceHit.y + PlayerHeightHalf, Time.deltaTime * heightSmoothing);
                transform.position = p;
                if (IsGrounded != hit)
                    playerHeightVelocity.y = 0;
            }
            else
                playerHeightVelocity.y += -gravityForce * Time.deltaTime;

            IsGrounded = hit;
        }

        private void OnDestroy()
        {
            depthSamplerHeight?.Dispose();
            depthSamplerRadialCollision?.Dispose();
            if(workingVolumeRT3D != null)
                workingVolumeRT3D.Release();
            workingVolumeRT3D = null;
            targetVolumeBox = null;
        }
    }
}