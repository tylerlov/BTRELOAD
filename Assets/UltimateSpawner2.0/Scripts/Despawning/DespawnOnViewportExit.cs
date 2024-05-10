using UnityEngine;

namespace UltimateSpawner.Despawning
{
	public class DespawnOnViewportExit : Despawner
	{
        // Private
        private bool hasBeenViewed = false;

        // Public
        [Tooltip("The camera used to check for object visibility. The camera tagged 'MainCamera' will be used if this field is empty")]
        public Camera viewportCamera;

        [Tooltip("One or more renderers that are used for visibility checks. The bounds of each renderer must lie outside of the viewport in order for the despawner to activate")]
        public Renderer[] visibilityRenderers;

        // Methods
        public void Awake()
        {
            // Use the main camera if no other camera was specified in the inspector
            if (viewportCamera == null)
            {
                viewportCamera = Camera.main;
                if (viewportCamera == null)
                {
                    Debug.LogWarningFormat("{0} was not able to find the main camera in the scene", gameObject.name);
                    enabled = false;
                }
            }

            // Check for any renderers
            if(visibilityRenderers.Length == 0 || AreRenderersAssigned() == false)
            {
                Debug.LogWarningFormat("{0} does not have any renderers assigned for visibiltiy checked. The object will despawn immediatley!", gameObject.name);
            }
        }

        public void OnEnable()
        {
            // Check if the game object is being shown in the viewport. This has to be done in OnEnable() as the
            // game object may have been re-used from an object pool
            hasBeenViewed = AreRenderersInViewport();
        }

#if UNITY_EDITOR
        public void Reset()
        {
            // Find all active renderers in the hierarchy
            visibilityRenderers = GetComponentsInChildren<Renderer>();
        }
#endif

        public void Update()
        {
            // All renderer bounds must be in the viewport to consider that the game object is visible 
            if (AreRenderersInViewport())
            {
                // The game object is visible in the viewport
                if (hasBeenViewed == false)
                {
                    // This is the first frame in which the game object is being shown in the viewport
                    hasBeenViewed = true;
                }
                return;
            }

            // The game object is not visible in the viewport
            if (hasBeenViewed == true)
            {
                // Set despawn condition
                MarkDespawnConditionAsMet();

                // The game object was previously shown in the viewport, despawn it
                Despawn();
            }
        }

        public override void CloneFrom(Despawner cloneFrom)
        {
            DespawnOnViewportExit despawner = cloneFrom as DespawnOnViewportExit;
            if (despawner != null)
            {
                viewportCamera = despawner.viewportCamera;
                if (despawner.visibilityRenderers != null)
                    visibilityRenderers = (Renderer[])despawner.visibilityRenderers.Clone();
            }
        }

        // Checks if any of the game object renderers are contained within the camera viewport bounds
        private bool AreRenderersInViewport()
        {
            foreach (Renderer rend in visibilityRenderers)
            {
                if (IsVectorInViewport(rend.bounds.min) || IsVectorInViewport(rend.bounds.max))
                {
                    return true;
                }
            }
            return false;
        }

        private bool AreRenderersAssigned()
        {
            foreach(Renderer rend in visibilityRenderers)
            {
                if (rend != null)
                    return true;
            }
            return false;
        }

        // Checks if the world coordinates in the specified Vector3 are within the camera viewport bounds 
        private bool IsVectorInViewport(Vector3 v)
        {
            var viewportV = viewportCamera.WorldToViewportPoint(v);
            return !(viewportV.x < 0f) && !(viewportV.x > 1f) && !(viewportV.y < 0f) && !(viewportV.y > 1f);
        }
    }
}
