using UnityEngine;
using FMODUnity;
using FMOD.Studio;

public class ProjectileAudioManager : MonoBehaviour
{
    private static ProjectileAudioManager _instance;
    public static ProjectileAudioManager Instance => _instance;

    [SerializeField] private EventReference movingSoundEvent;
    private FMOD.Studio.EventInstance projectileMoveSound;
    
    void Awake()
    {
        if (_instance == null)
            _instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        // Create a single instance that can be reused
        if (!movingSoundEvent.IsNull)
        {
            projectileMoveSound = RuntimeManager.CreateInstance(movingSoundEvent);
        }
    }

    public void UpdateProjectileSound(Vector3 position, float velocity)
    {
        if (projectileMoveSound.isValid())
        {
            projectileMoveSound.set3DAttributes(RuntimeUtils.To3DAttributes(position));
            projectileMoveSound.setParameterByName("Velocity", velocity);
        }
    }

    private void OnDestroy()
    {
        if (projectileMoveSound.isValid())
        {
            projectileMoveSound.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            projectileMoveSound.release();
        }
    }
}
