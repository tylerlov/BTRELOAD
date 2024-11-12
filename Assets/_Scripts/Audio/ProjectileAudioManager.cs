using UnityEngine;
using FMODUnity;
using FMOD.Studio;
using System.Collections.Generic;

[DefaultExecutionOrder(-100)]
public class ProjectileAudioManager : MonoBehaviour
{
    private static ProjectileAudioManager _instance;
    public static ProjectileAudioManager Instance => _instance;

    [SerializeField] private EventReference movingSoundEvent;
    
    // Reduced pool size and increased update interval
    private const int MAX_ACTIVE_SOUNDS = 3; // Drastically reduce simultaneous sounds
    private const float UPDATE_INTERVAL = 0.25f; // Reduce update frequency
    private const float AUDIO_CUTOFF_DISTANCE = 20f; // Reduce audio range
    private const float MIN_VELOCITY_THRESHOLD = 5f; // Only play sound above this velocity
    
    private Dictionary<int, EventInstance> activeProjectileSounds;
    private Queue<EventInstance> soundPool;
    private float nextUpdateTime;
    
    // Track highest velocity projectiles
    private struct ProjectileAudioData
    {
        public Vector3 Position;
        public float Velocity;
        public int ProjectileId;
    }
    private List<ProjectileAudioData> pendingAudioUpdates = new List<ProjectileAudioData>();

    void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        activeProjectileSounds = new Dictionary<int, EventInstance>(MAX_ACTIVE_SOUNDS);
        soundPool = new Queue<EventInstance>(MAX_ACTIVE_SOUNDS);
        
        // Pre-initialize minimal pool
        for (int i = 0; i < MAX_ACTIVE_SOUNDS; i++)
        {
            CreateNewSoundInstance();
        }
    }

    private void CreateNewSoundInstance()
    {
        if (!movingSoundEvent.IsNull)
        {
            var instance = RuntimeManager.CreateInstance(movingSoundEvent);
            instance.start();
            instance.setVolume(0);
            soundPool.Enqueue(instance);
        }
    }

    public void UpdateProjectileSound(Vector3 position, float velocity, int projectileId, bool isHoming)
    {
        // Skip if not homing - this is the key change
        if (!isHoming) return;
        
        // Skip if too soon or velocity too low
        if (Time.time < nextUpdateTime || velocity < MIN_VELOCITY_THRESHOLD) return;

        // Skip if too far from camera
        if (Vector3.Distance(position, Camera.main.transform.position) > AUDIO_CUTOFF_DISTANCE)
        {
            ReleaseProjectileSound(projectileId);
            return;
        }

        // Add to pending updates
        pendingAudioUpdates.Add(new ProjectileAudioData 
        { 
            Position = position, 
            Velocity = velocity, 
            ProjectileId = projectileId 
        });
    }

    private void LateUpdate()
    {
        if (Time.time < nextUpdateTime) return;
        
        // Sort pending updates by velocity (descending)
        pendingAudioUpdates.Sort((a, b) => b.Velocity.CompareTo(a.Velocity));
        
        // Process only the top few fastest projectiles
        int processCount = Mathf.Min(pendingAudioUpdates.Count, MAX_ACTIVE_SOUNDS);
        
        // Release sounds for projectiles no longer in top N
        foreach (var kvp in new Dictionary<int, EventInstance>(activeProjectileSounds))
        {
            bool stillActive = false;
            for (int i = 0; i < processCount; i++)
            {
                if (pendingAudioUpdates[i].ProjectileId == kvp.Key)
                {
                    stillActive = true;
                    break;
                }
            }
            if (!stillActive)
            {
                ReleaseProjectileSound(kvp.Key);
            }
        }
        
        // Update sounds for top projectiles
        for (int i = 0; i < processCount; i++)
        {
            var data = pendingAudioUpdates[i];
            
            if (!activeProjectileSounds.TryGetValue(data.ProjectileId, out EventInstance sound))
            {
                if (soundPool.Count > 0)
                {
                    sound = soundPool.Dequeue();
                    activeProjectileSounds[data.ProjectileId] = sound;
                }
                else continue;
            }

            if (sound.isValid())
            {
                sound.set3DAttributes(RuntimeUtils.To3DAttributes(data.Position));
                sound.setParameterByName("Velocity", data.Velocity);
                sound.setVolume(1);
            }
        }
        
        pendingAudioUpdates.Clear();
        nextUpdateTime = Time.time + UPDATE_INTERVAL;
    }

    public void ReleaseProjectileSound(int projectileId)
    {
        if (activeProjectileSounds.TryGetValue(projectileId, out EventInstance sound))
        {
            sound.setVolume(0);
            if (soundPool.Count < MAX_ACTIVE_SOUNDS)
            {
                soundPool.Enqueue(sound);
            }
            else
            {
                sound.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
                sound.release();
            }
            activeProjectileSounds.Remove(projectileId);
        }
    }

    private void OnDestroy()
    {
        foreach (var sound in soundPool)
        {
            if (sound.isValid())
            {
                sound.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
                sound.release();
            }
        }

        foreach (var sound in activeProjectileSounds.Values)
        {
            if (sound.isValid())
            {
                sound.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
                sound.release();
            }
        }
    }
}
