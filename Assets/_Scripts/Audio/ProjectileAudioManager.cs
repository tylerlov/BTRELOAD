using UnityEngine;
using FMODUnity;
using FMOD.Studio;
using System.Collections.Generic;
using System.Collections;

[DefaultExecutionOrder(-100)]
public class ProjectileAudioManager : MonoBehaviour
{
    public static ProjectileAudioManager Instance { get; private set; }

    [SerializeField] private EventReference groupProjectileSoundEvent;
    [SerializeField] private EventReference playerImpactSoundEvent;
    [SerializeField] private EventReference enemyImpactSoundEvent;
    [SerializeField] private Dictionary<string, EventReference> oneShotSoundEvents = new Dictionary<string, EventReference>();

    private const int MAX_CONCURRENT_SOUNDS = 32;
    private EventInstance[] activeInstances;
    private int[] activeIds;
    private int activeCount;
    private bool isSoundEnabled;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeAudioSystem();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeAudioSystem()
    {
        activeInstances = new EventInstance[MAX_CONCURRENT_SOUNDS];
        activeIds = new int[MAX_CONCURRENT_SOUNDS];
        activeCount = 0;

        isSoundEnabled = ValidateEventReference(groupProjectileSoundEvent);
        
        ValidateAudioEvents();
    }

    private bool ValidateEventReference(EventReference eventRef)
    {
        if (eventRef.IsNull)
        {
            return false;
        }

        FMOD.Studio.EventDescription eventDesc = RuntimeManager.GetEventDescription(eventRef);
        return eventDesc.isValid();
    }

    private void ValidateAudioEvents()
    {
        if (!ValidateEventReference(groupProjectileSoundEvent))
        {
            Debug.LogWarning("Group projectile sound event not set!");
        }

        if (!ValidateEventReference(playerImpactSoundEvent))
        {
            Debug.LogWarning("Player impact sound event not set!");
        }

        if (!ValidateEventReference(enemyImpactSoundEvent))
        {
            Debug.LogWarning("Enemy impact sound event not set!");
        }

        foreach (var kvp in oneShotSoundEvents)
        {
            if (!ValidateEventReference(kvp.Value))
            {
                Debug.LogWarning($"One-shot sound event '{kvp.Key}' not set!");
            }
        }
    }

    public void RegisterHomingProjectile(int projectileId, Transform projectileTransform)
    {
        if (!isSoundEnabled || projectileId < 0 || projectileTransform == null) return;

        for (int i = 0; i < MAX_CONCURRENT_SOUNDS; i++)
        {
            if (activeIds[i] == projectileId)
            {
                if (activeInstances[i].isValid())
                {
                    activeInstances[i].set3DAttributes(RuntimeUtils.To3DAttributes(projectileTransform.position));
                }
                return;
            }
        }

        try
        {
            for (int i = 0; i < MAX_CONCURRENT_SOUNDS; i++)
            {
                if (!activeInstances[i].isValid())
                {
                    var instance = RuntimeManager.CreateInstance(groupProjectileSoundEvent);
                    if (!instance.isValid())
                    {
                        Debug.LogError($"Failed to create valid instance for projectile {projectileId}");
                        return;
                    }

                    instance.set3DAttributes(RuntimeUtils.To3DAttributes(projectileTransform.position));
                    instance.start();
                    activeInstances[i] = instance;
                    activeIds[i] = projectileId;
                    activeCount++;
                    return;
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error creating sound instance: {e.Message}");
        }
    }

    public void UnregisterHomingProjectile(int projectileId)
    {
        if (!isSoundEnabled) return;

        for (int i = 0; i < MAX_CONCURRENT_SOUNDS; i++)
        {
            if (activeIds[i] == projectileId && activeInstances[i].isValid())
            {
                try
                {
                    activeInstances[i].stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
                    activeInstances[i].release();
                }
                catch (System.Exception)
                {
                    // Ignore release errors
                }
                activeIds[i] = -1;
                activeCount--;
                break;
            }
        }
    }

    public void PlayOneShotSound(string soundEvent, Vector3 position)
    {
        if (!isSoundEnabled || !oneShotSoundEvents.ContainsKey(soundEvent)) return;

        try
        {
            var instance = RuntimeManager.CreateInstance(oneShotSoundEvents[soundEvent]);
            if (!instance.isValid())
            {
                Debug.LogError($"Failed to create valid instance for one-shot sound {soundEvent}");
                return;
            }

            instance.set3DAttributes(RuntimeUtils.To3DAttributes(position));
            instance.start();
            
            StartCoroutine(ReleaseAfterPlay(instance));
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error playing one-shot sound: {e.Message}");
        }
    }

    private IEnumerator ReleaseAfterPlay(EventInstance instance)
    {
        if (!instance.isValid())
        {
            yield break;
        }

        PLAYBACK_STATE state;
        do
        {
            instance.getPlaybackState(out state);
            yield return new WaitForSeconds(0.1f);
        } while (state != PLAYBACK_STATE.STOPPED && instance.isValid());

        if (instance.isValid())
        {
            instance.release();
        }
    }

    private void Update()
    {
        if (!isSoundEnabled) return;

        // Clean up any invalid instances
        for (int i = 0; i < MAX_CONCURRENT_SOUNDS; i++)
        {
            if (activeIds[i] >= 0 && !activeInstances[i].isValid())
            {
                activeIds[i] = -1;
                activeCount--;
            }
        }
    }

    private void OnDisable()
    {
        StopAllSounds();
    }

    private void OnDestroy()
    {
        StopAllSounds();
    }

    private void StopAllSounds()
    {
        if (!isSoundEnabled) return;

        for (int i = 0; i < MAX_CONCURRENT_SOUNDS; i++)
        {
            if (activeInstances[i].isValid())
            {
                activeInstances[i].stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
                activeInstances[i].release();
                activeInstances[i].clearHandle();
                activeIds[i] = -1;
            }
        }
        activeCount = 0;
    }

    public void PlayGroupProjectileSound(Vector3[] positions)
    {
        if (!isSoundEnabled || !ValidateEventReference(groupProjectileSoundEvent) || positions == null || positions.Length == 0) return;

        try
        {
            Vector3 averagePosition = Vector3.zero;
            for (int i = 0; i < positions.Length; i++)
            {
                averagePosition += positions[i];
            }
            averagePosition /= positions.Length;

            var instance = RuntimeManager.CreateInstance(groupProjectileSoundEvent);
            if (!instance.isValid())
            {
                Debug.LogError("Failed to create valid instance for group projectile sound");
                return;
            }

            instance.set3DAttributes(RuntimeUtils.To3DAttributes(averagePosition));
            instance.start();
            
            StartCoroutine(ReleaseAfterPlay(instance));
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error playing group projectile sound: {e.Message}");
        }
    }

    public void PlayPlayerImpactSound(Vector3 position)
    {
        if (!isSoundEnabled || !ValidateEventReference(playerImpactSoundEvent)) return;

        try
        {
            var instance = RuntimeManager.CreateInstance(playerImpactSoundEvent);
            if (!instance.isValid())
            {
                Debug.LogError("Failed to create valid instance for player impact sound");
                return;
            }

            instance.set3DAttributes(RuntimeUtils.To3DAttributes(position));
            instance.start();
            
            StartCoroutine(ReleaseAfterPlay(instance));
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error playing player impact sound: {e.Message}");
        }
    }

    public void PlayEnemyImpactSound(Vector3 position)
    {
        if (!isSoundEnabled || !ValidateEventReference(enemyImpactSoundEvent)) return;

        try
        {
            var instance = RuntimeManager.CreateInstance(enemyImpactSoundEvent);
            if (!instance.isValid())
            {
                Debug.LogError("Failed to create valid instance for enemy impact sound");
                return;
            }

            instance.set3DAttributes(RuntimeUtils.To3DAttributes(position));
            instance.start();
            
            StartCoroutine(ReleaseAfterPlay(instance));
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error playing enemy impact sound: {e.Message}");
        }
    }
}
