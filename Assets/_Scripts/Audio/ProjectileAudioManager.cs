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
    private EventReference groupProjectileSoundEvent;
    
    private const float MAX_AUDIO_DISTANCE_SQR = 2500f;
    private const float UPDATE_INTERVAL = 0.1f;
    private const float DISTANCE_CULLING_CHECK_INTERVAL = 0.5f;
    private const int MAX_CONCURRENT_SOUNDS = 20;
    private const float MIN_VOLUME_THRESHOLD = 0.01f;

    private ObjectPool<EventInstance> eventPool;
    private EventInstance[] activeInstances;
    private Transform[] activeTransforms;
    private int[] activeIds;
    private int activeCount;
    private float[] volumeCache;
    
    private Vector3 lastPlayerPosition;
    private float nextUpdateTime;
    private float nextCullTime;
    private bool isSoundEnabled;
    private Transform playerTransform;

    [SerializeField]
    private Dictionary<string, EventReference> oneShotSoundEvents = new Dictionary<string, EventReference>();
    
    private Dictionary<string, ObjectPool<EventInstance>> oneShotEventPools = 
        new Dictionary<string, ObjectPool<EventInstance>>();

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
        activeInstances = new EventInstance[MAX_CONCURRENT_SOUNDS];
        activeTransforms = new Transform[MAX_CONCURRENT_SOUNDS];
        activeIds = new int[MAX_CONCURRENT_SOUNDS];
        volumeCache = new float[MAX_CONCURRENT_SOUNDS];
        activeCount = 0;

        if (groupProjectileSoundEvent.IsNull)
        {
            isSoundEnabled = false;
            return;
        }

        eventPool = new ObjectPool<EventInstance>(
            createFunc: () => RuntimeManager.CreateInstance(groupProjectileSoundEvent),
            actionOnGet: instance => {
                instance.start();
                instance.setVolume(0f);
            },
            actionOnRelease: instance => {
                if (instance.isValid())
                {
                    instance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
                    instance.setVolume(0f);
                }
            },
            actionOnDestroy: instance => {
                if (instance.isValid())
                {
                    instance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
                    instance.release();
                }
            },
            defaultCapacity: MAX_CONCURRENT_SOUNDS
        );

        isSoundEnabled = true;
    }

    private void Start()
    {
        playerTransform = Camera.main.transform;
        lastPlayerPosition = playerTransform.position;
        isSoundEnabled = !playerImpactSoundEvent.IsNull && !groupProjectileSoundEvent.IsNull;
    }

    private void Update()
    {
        if (!isSoundEnabled || activeCount == 0) return;

        float currentTime = Time.unscaledTime;
        
        if (currentTime >= nextUpdateTime)
        {
            BatchUpdateProjectileAudio();
            BatchCullDistantSounds();
            nextUpdateTime = currentTime + Mathf.Max(UPDATE_INTERVAL, Time.deltaTime);
        }
    }

    private void BatchUpdateProjectileAudio()
    {
        Vector3 playerPos = playerTransform.position;
        bool significantPlayerMovement = (playerPos - lastPlayerPosition).sqrMagnitude > 1f;
        
        if (!significantPlayerMovement && Time.frameCount % 2 != 0) return;

        for (int i = 0; i < activeCount; i++)
        {
            if (activeTransforms[i] == null) continue;

            var transform = activeTransforms[i];
            float distanceSqr = (transform.position - playerPos).sqrMagnitude;
            
            if (distanceSqr > MAX_AUDIO_DISTANCE_SQR)
            {
                UnregisterHomingProjectile(activeIds[i]);
                continue;
            }

            float volume = 1f - (distanceSqr / MAX_AUDIO_DISTANCE_SQR);
            
            if (Mathf.Abs(volume - volumeCache[i]) > MIN_VOLUME_THRESHOLD || significantPlayerMovement)
            {
                activeInstances[i].set3DAttributes(RuntimeUtils.To3DAttributes(transform));
                activeInstances[i].setVolume(volume);
                volumeCache[i] = volume;
            }
        }

        if (significantPlayerMovement)
        {
            lastPlayerPosition = playerPos;
        }
    }

    private void BatchCullDistantSounds()
    {
        Vector3 playerPos = playerTransform.position;
        
        for (int i = MAX_CONCURRENT_SOUNDS - 1; i >= 0; i--)
        {
            if (activeTransforms[i] == null) continue;

            if ((activeTransforms[i].position - playerPos).sqrMagnitude > MAX_AUDIO_DISTANCE_SQR)
            {
                UnregisterHomingProjectile(activeIds[i]);
            }
        }
    }

    public void RegisterHomingProjectile(int projectileId, Transform projectileTransform)
    {
        if (!isSoundEnabled || activeCount >= MAX_CONCURRENT_SOUNDS) return;

        for (int i = 0; i < MAX_CONCURRENT_SOUNDS; i++)
        {
            if (activeTransforms[i] == null)
            {
                activeIds[i] = projectileId;
                activeTransforms[i] = projectileTransform;
                activeInstances[i] = eventPool.Get();
                volumeCache[i] = 0f;
                activeCount++;
                return;
            }
        }
    }

    public void UnregisterHomingProjectile(int projectileId)
    {
        if (!isSoundEnabled || activeInstances == null) return;

        for (int i = 0; i < MAX_CONCURRENT_SOUNDS; i++)
        {
            if (activeIds[i] == projectileId && activeTransforms[i] != null)
            {
                if (eventPool != null && activeInstances[i].isValid())
                {
                    eventPool.Release(activeInstances[i]);
                }
                activeInstances[i] = default;
                activeTransforms[i] = null;
                activeIds[i] = 0;
                volumeCache[i] = 0f;
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
            if (!oneShotEventPools.ContainsKey(soundEvent))
            {
                InitializeOneShotPool(soundEvent);
            }

            var instance = oneShotEventPools[soundEvent].Get();
            instance.set3DAttributes(RuntimeUtils.To3DAttributes(position));
            instance.start();
            
            // Auto-release after playing
            StartCoroutine(ReleaseAfterPlay(soundEvent, instance));
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to play one-shot sound {soundEvent}: {e.Message}");
        }
    }

    private void InitializeOneShotPool(string soundEvent)
    {
        var eventRef = oneShotSoundEvents[soundEvent];
        oneShotEventPools[soundEvent] = new ObjectPool<EventInstance>(
            createFunc: () => RuntimeManager.CreateInstance(eventRef),
            actionOnGet: instance => instance.setVolume(1f),
            actionOnRelease: instance => {
                if (instance.isValid())
                {
                    instance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
                    instance.setVolume(0);
                }
            },
            actionOnDestroy: instance => {
                if (instance.isValid())
                {
                    instance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
                    instance.release();
                }
            },
            defaultCapacity: 5
        );
    }

    private IEnumerator ReleaseAfterPlay(string soundEvent, EventInstance instance)
    {
        PLAYBACK_STATE state;
        do
        {
            instance.getPlaybackState(out state);
            yield return new WaitForSeconds(0.1f);
        } while (state != PLAYBACK_STATE.STOPPED);

        if (oneShotEventPools.ContainsKey(soundEvent))
        {
            oneShotEventPools[soundEvent].Release(instance);
        }
    }

    public void PlayPlayerImpactSound(Vector3 position)
    {
        if (!isSoundEnabled) return;

        EventInstance instance = RuntimeManager.CreateInstance(playerImpactSoundEvent);
        instance.set3DAttributes(RuntimeUtils.To3DAttributes(position));
        instance.start();
        instance.release();
    }

    public void PlayEnemyImpactSound(Vector3 position)
    {
        if (!isSoundEnabled) return;
        
        EventInstance instance = RuntimeManager.CreateInstance(playerImpactSoundEvent);
        instance.set3DAttributes(RuntimeUtils.To3DAttributes(position));
        instance.start();
        instance.release();
    }

    public void PlayGroupProjectileSound(Vector3[] shooterPositions)
    {
        if (!isSoundEnabled || shooterPositions == null || shooterPositions.Length == 0) return;

        Vector3 averagePosition = Vector3.zero;
        foreach (var position in shooterPositions)
        {
            averagePosition += position;
        }
        averagePosition /= shooterPositions.Length;

        EventInstance instance = RuntimeManager.CreateInstance(groupProjectileSoundEvent);
        instance.set3DAttributes(RuntimeUtils.To3DAttributes(averagePosition));
        instance.start();
        instance.release();
    }

    private void OnDestroy()
    {
        foreach (var pool in oneShotEventPools.Values)
        {
            pool.Clear();
        }
        oneShotEventPools.Clear();

        if (eventPool != null)
        {
            eventPool.Clear();
        }

        // Clean up native arrays
        if (activeInstances != null)
        {
            for (int i = 0; i < activeInstances.Length; i++)
            {
                if (activeInstances[i].isValid())
                {
                    activeInstances[i].stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
                    activeInstances[i].release();
                }
            }
        }

        Instance = null;
    }
}
