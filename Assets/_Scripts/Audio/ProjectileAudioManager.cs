using UnityEngine;
using FMODUnity;
using FMOD.Studio;
using System.Collections.Generic;
using System.Collections;

[DefaultExecutionOrder(-100)]
public class ProjectileAudioManager : MonoBehaviour
{
    public static ProjectileAudioManager Instance { get; private set; }

    [SerializeField]
    private EventReference playerImpactSoundEvent;
        
    [SerializeField]
    private EventReference enemyImpactSoundEvent;
        
    [SerializeField]
    private EventReference groupProjectileSoundEvent;
    
    private const float UPDATE_INTERVAL = 0.1f;
    private const int MAX_CONCURRENT_SOUNDS = 20;

    private Transform[] activeTransforms;
    private EventInstance[] activeInstances;
    private int[] activeIds;
    private int activeCount;
    
    private float nextUpdateTime;
    private bool isSoundEnabled;
    private Transform playerTransform;

    [SerializeField]
    private Dictionary<string, EventReference> oneShotSoundEvents = new Dictionary<string, EventReference>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeAudioSystem();
        }
        else Destroy(gameObject);
    }

    private void InitializeAudioSystem()
    {
        if (AudioManager.Instance == null)
        {
            Debug.LogError("AudioManager instance not found!");
            enabled = false;
            return;
        }

        activeInstances = new EventInstance[MAX_CONCURRENT_SOUNDS];
        activeTransforms = new Transform[MAX_CONCURRENT_SOUNDS];
        activeIds = new int[MAX_CONCURRENT_SOUNDS];
        activeCount = 0;

        isSoundEnabled = !groupProjectileSoundEvent.IsNull;
        
        ValidateAudioEvents();
    }

    private void ValidateAudioEvents()
    {
        if (groupProjectileSoundEvent.IsNull)
        {
            Debug.LogWarning("Group projectile sound event not set!");
        }

        if (playerImpactSoundEvent.IsNull)
        {
            Debug.LogWarning("Player impact sound event not set!");
        }

        if (enemyImpactSoundEvent.IsNull)
        {
            Debug.LogWarning("Enemy impact sound event not set!");
        }

        foreach (var kvp in oneShotSoundEvents)
        {
            if (kvp.Value.IsNull)
            {
                Debug.LogWarning($"One-shot sound event '{kvp.Key}' not set!");
            }
        }
    }

    public void RegisterHomingProjectile(int projectileId, Transform projectileTransform)
    {
        if (!isSoundEnabled || activeCount >= MAX_CONCURRENT_SOUNDS || projectileTransform == null) return;

        for (int i = 0; i < MAX_CONCURRENT_SOUNDS; i++)
        {
            if (activeTransforms[i] == null)
            {
                try
                {
                    var instance = AudioManager.Instance.GetOrCreateInstance(groupProjectileSoundEvent.Path);
                    if (!instance.isValid())
                    {
                        Debug.LogError($"Failed to create valid instance for projectile {projectileId}");
                        return;
                    }

                    activeIds[i] = projectileId;
                    activeTransforms[i] = projectileTransform;
                    activeInstances[i] = instance;
                    instance.start();
                    activeCount++;
                    return;
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Failed to register projectile {projectileId}: {e.Message}");
                    return;
                }
            }
        }
    }

    public void UnregisterHomingProjectile(int projectileId)
    {
        if (!isSoundEnabled || activeInstances == null || AudioManager.Instance == null) return;

        for (int i = 0; i < MAX_CONCURRENT_SOUNDS; i++)
        {
            if (activeIds[i] == projectileId && activeTransforms[i] != null)
            {
                if (activeInstances[i].isValid())
                {
                    try
                    {
                        AudioManager.Instance.ReleaseInstance(groupProjectileSoundEvent.Path, activeInstances[i]);
                    }
                    catch (System.Exception)
                    {
                        // If release fails, just stop and release the instance directly
                        activeInstances[i].stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
                        activeInstances[i].release();
                    }
                }
                activeInstances[i] = default;
                activeTransforms[i] = null;
                activeIds[i] = 0;
                activeCount--;
                return;
            }
        }
    }

    public void PlayOneShotSound(string soundEvent, Vector3 position)
    {
        if (!isSoundEnabled || !oneShotSoundEvents.ContainsKey(soundEvent)) return;

        try
        {
            var instance = AudioManager.Instance.GetOrCreateInstance(oneShotSoundEvents[soundEvent].Path);
            instance.set3DAttributes(RuntimeUtils.To3DAttributes(position));
            instance.start();
            
            StartCoroutine(ReleaseAfterPlay(oneShotSoundEvents[soundEvent].Path, instance));
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to play one-shot sound {soundEvent}: {e.Message}");
        }
    }

    private IEnumerator ReleaseAfterPlay(string eventPath, EventInstance instance)
    {
        PLAYBACK_STATE state;
        do
        {
            instance.getPlaybackState(out state);
            yield return new WaitForSeconds(0.1f);
        } while (state != PLAYBACK_STATE.STOPPED);

        AudioManager.Instance.ReleaseInstance(eventPath, instance);
    }

    private void Update()
    {
        if (!isSoundEnabled || activeCount == 0) return;

        float currentTime = Time.unscaledTime;
        if (currentTime >= nextUpdateTime)
        {
            UpdateProjectilePositions();
            nextUpdateTime = currentTime + UPDATE_INTERVAL;
        }
    }

    private void UpdateProjectilePositions()
    {
        for (int i = 0; i < MAX_CONCURRENT_SOUNDS; i++)
        {
            if (activeTransforms[i] == null || !activeInstances[i].isValid()) continue;

            try
            {
                activeInstances[i].set3DAttributes(RuntimeUtils.To3DAttributes(activeTransforms[i].position));
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to update position for projectile {activeIds[i]}: {e.Message}");
                UnregisterHomingProjectile(activeIds[i]);
            }
        }
    }

    private void OnDestroy()
    {
        if (!isSoundEnabled || activeInstances == null) return;

        for (int i = 0; i < MAX_CONCURRENT_SOUNDS; i++)
        {
            if (activeInstances[i].isValid())
            {
                AudioManager.Instance.ReleaseInstance(groupProjectileSoundEvent.Path, activeInstances[i]);
                activeInstances[i] = default;
            }
        }
    }

    public void PlayGroupProjectileSound(Vector3[] positions)
    {
        if (!isSoundEnabled || groupProjectileSoundEvent.IsNull || positions == null || positions.Length == 0) return;

        try
        {
            // Calculate centroid of all positions
            Vector3 averagePosition = Vector3.zero;
            for (int i = 0; i < positions.Length; i++)
            {
                averagePosition += positions[i];
            }
            averagePosition /= positions.Length;

            var instance = AudioManager.Instance.GetOrCreateInstance(groupProjectileSoundEvent.Path);
            instance.set3DAttributes(RuntimeUtils.To3DAttributes(averagePosition));
            instance.start();
            
            StartCoroutine(ReleaseAfterPlay(groupProjectileSoundEvent.Path, instance));
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to play group projectile sound: {e.Message}");
        }
    }

    public void PlayGroupProjectileSound(Vector3 position)
    {
        PlayGroupProjectileSound(new Vector3[] { position });
    }

    public void PlayPlayerImpactSound(Vector3 position)
    {
        if (!isSoundEnabled || playerImpactSoundEvent.IsNull) return;

        try
        {
            var instance = AudioManager.Instance.GetOrCreateInstance(playerImpactSoundEvent.Path);
            instance.set3DAttributes(RuntimeUtils.To3DAttributes(position));
            instance.start();
            
            StartCoroutine(ReleaseAfterPlay(playerImpactSoundEvent.Path, instance));
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to play player impact sound: {e.Message}");
        }
    }

    public void PlayEnemyImpactSound(Vector3 position)
    {
        if (!isSoundEnabled || enemyImpactSoundEvent.IsNull) return;

        try
        {
            var instance = AudioManager.Instance.GetOrCreateInstance(enemyImpactSoundEvent.Path);
            instance.set3DAttributes(RuntimeUtils.To3DAttributes(position));
            instance.start();
            
            StartCoroutine(ReleaseAfterPlay(enemyImpactSoundEvent.Path, instance));
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to play enemy impact sound: {e.Message}");
        }
    }

    // ... rest of the methods using AudioManager.Instance for instance management ...
}
