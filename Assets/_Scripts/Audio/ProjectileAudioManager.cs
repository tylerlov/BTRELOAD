using System;
using System.Collections.Generic;
using UnityEngine;
using FMODUnity;
using FMOD.Studio;

public class ProjectileAudioManager : MonoBehaviour
{
    public static ProjectileAudioManager Instance { get; private set; }

    // Dictionary to keep track of projectiles and their FMOD event instances
    private Dictionary<ProjectileStateBased, EventInstance> projectileAudioInstances =
        new Dictionary<ProjectileStateBased, EventInstance>();

    // FMOD Callback to handle event completion
    private static EVENT_CALLBACK Callback = new EVENT_CALLBACK(OnEventStopped);

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Registers an enemy projectile to play a looping FMOD event.
    /// </summary>
    /// <param name="projectile">The enemy projectile instance.</param>
    public void RegisterProjectile(ProjectileStateBased projectile)
    {
        if (projectile == null || projectile.isFromStaticEnemy || !projectile.IsSoundEnabled())
            return;

        // Avoid duplicate registrations
        if (projectileAudioInstances.ContainsKey(projectile))
            return;

        // Retrieve the FMOD event reference from the projectile
        EventReference movingSoundEvent = projectile.GetMovingSoundEvent();
        if (movingSoundEvent.IsNull)
            return;

        // Create and configure the FMOD event instance
        EventInstance eventInstance = RuntimeManager.CreateInstance(movingSoundEvent);
        eventInstance.set3DAttributes(RuntimeUtils.To3DAttributes(projectile.transform.position));

        // Register the callback to handle event stops
        eventInstance.setCallback(Callback, EVENT_CALLBACK_TYPE.STOPPED);

        // Start the event
        eventInstance.start();

        // Store the association
        projectileAudioInstances.Add(projectile, eventInstance);

        ConditionalDebug.Log($"[ProjectileAudioManager] Registered looping sound for {projectile.gameObject.name}");
    }

    /// <summary>
    /// Unregisters a projectile and stops its associated FMOD event.
    /// </summary>
    /// <param name="projectile">The projectile to unregister.</param>
    public void UnregisterProjectile(ProjectileStateBased projectile)
    {
        if (projectile == null)
            return;

        if (projectileAudioInstances.TryGetValue(projectile, out EventInstance eventInstance))
        {
            // Stop the event with no fade out
            eventInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            // Release the event instance
            eventInstance.release();

            // Remove from the dictionary
            projectileAudioInstances.Remove(projectile);

            ConditionalDebug.Log($"[ProjectileAudioManager] Unregistered sound for {projectile.gameObject.name}");
        }
    }

    /// <summary>
    /// FMOD callback invoked when an event is stopped.
    /// </summary>
    [AOT.MonoPInvokeCallback(typeof(EVENT_CALLBACK))]
    private static FMOD.RESULT OnEventStopped(EVENT_CALLBACK_TYPE type, IntPtr instancePtr, IntPtr parameterPtr)
    {
        if (type == EVENT_CALLBACK_TYPE.STOPPED)
        {
            // Find the projectile associated with this event instance
            EventInstance instance = new EventInstance(instancePtr);
            foreach (var kvp in Instance.projectileAudioInstances)
            {
                if (kvp.Value.handle == instance.handle)
                {
                    // Unregister the projectile
                    Instance.UnregisterProjectile(kvp.Key);
                    break;
                }
            }
        }
        return FMOD.RESULT.OK;
    }

    private void OnDestroy()
    {
        // Ensure all FMOD event instances are stopped and released
        foreach (var kvp in projectileAudioInstances)
        {
            kvp.Value.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            kvp.Value.release();
        }
        projectileAudioInstances.Clear();
    }
}