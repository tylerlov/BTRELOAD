using UnityEngine;
using UnityEngine.EventSystems;
using FMODUnity; // Add FMODUnity namespace
using FMOD.Studio;

namespace Michsky.UI.Reach
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Reach UI/Audio/UI Element Sound")]
    public class UIElementSound : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler
    {
        [Header("Resources")]
        public StudioEventEmitter fmodEventEmitter; // Changed from AudioSource to FMOD's StudioEventEmitter

        [Header("Custom SFX")]
        public EventReference hoverSFX; // Changed from AudioClip to EventReference
        public EventReference clickSFX; // Changed from AudioClip to EventReference

        [Header("Settings")]
        public bool enableHoverSound = true;
        public bool enableClickSound = true;

        void OnEnable()
        {
            if (UIManagerAudio.instance != null && fmodEventEmitter == null) 
            { 
                fmodEventEmitter = UIManagerAudio.instance.FmodEventEmitter; // Changed from audioSource to fmodEventEmitter
            }
        }

    public void OnPointerEnter(PointerEventData eventData)
        {
            if (enableHoverSound)
            {
                // Make sure hoverSFX is of type EventReference
                if (hoverSFX.IsNull) 
                { 
                    // Assuming UIManagerAudio has been updated to use EventReference
                    RuntimeManager.PlayOneShot(UIManagerAudio.instance.UIManagerAsset.hoverSound); 
                }
                else 
                { 
                    RuntimeManager.PlayOneShot(hoverSFX); 
                }
            }
        }

   public void OnPointerClick(PointerEventData eventData)
        {
            if (enableClickSound)
            {
                // Make sure clickSFX is of type EventReference
                if (clickSFX.IsNull) 
                { 
                    // Assuming UIManagerAudio has been updated to use EventReference
                    RuntimeManager.PlayOneShot(UIManagerAudio.instance.UIManagerAsset.clickSound); 
                }
                else 
                { 
                    RuntimeManager.PlayOneShot(clickSFX); 
                }
            }
        }
    }

}
