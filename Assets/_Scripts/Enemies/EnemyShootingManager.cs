using System.Collections.Generic;
using Chronos;
using SonicBloom.Koreo;
using UnityEngine;

public class EnemyShootingManager : MonoBehaviour
{
    public static EnemyShootingManager Instance { get; private set; }

    private List<StaticEnemyShooting> staticEnemyShootings = new List<StaticEnemyShooting>();

    [SerializeField, EventID]
    private string eventID;
    private int shootCounter = 0;

    private Timeline managerTimeline;

    private int koreographerEventCount = 0;
    private float lastLogTime = 0f;
    private const float LOG_INTERVAL = 5f; // Log every 5 seconds

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            managerTimeline = GetComponent<Timeline>();
            ConditionalDebug.Log("EnemyShootingManager initialized");
        }
        else
        {
            ConditionalDebug.Log("Duplicate EnemyShootingManager found, destroying");
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        Koreographer.Instance.RegisterForEvents(eventID, OnMusicalEnemyShoot);
    }

    private void OnDisable()
    {
        if (Koreographer.Instance != null)
        {
            Koreographer.Instance.UnregisterForEvents(eventID, OnMusicalEnemyShoot);
        }
    }

    public void RegisterStaticEnemyShooting(StaticEnemyShooting shooting)
    {
        if (!staticEnemyShootings.Contains(shooting))
        {
            staticEnemyShootings.Add(shooting);
        }
        else
        {
            ConditionalDebug.Log($"StaticEnemyShooting {shooting.name} already registered");
        }
    }

    public void UnregisterStaticEnemyShooting(StaticEnemyShooting shooting)
    {
        if (shooting != null && staticEnemyShootings.Contains(shooting))
        {
            staticEnemyShootings.Remove(shooting);
        }
    }

    public void UnregisterAllStaticEnemyShootingsFromKoreographer()
    {
        foreach (var shooting in staticEnemyShootings)
        {
            //shooting.UnregisterFromKoreographer();
        }
    }

    public float GetCurrentTime()
    {
        return managerTimeline != null ? managerTimeline.time : Time.time;
    }

    private void Update()
    {
        if (Time.time - lastLogTime >= LOG_INTERVAL)
        {
            ConditionalDebug.Log($"[EnemyShootingManager] Koreographer events in the last {LOG_INTERVAL} seconds: {koreographerEventCount}");
            ConditionalDebug.Log($"[EnemyShootingManager] Current registered StaticEnemyShooting count: {staticEnemyShootings.Count}");
            lastLogTime = Time.time;
            koreographerEventCount = 0;
        }
    }

    private void OnMusicalEnemyShoot(KoreographyEvent evt)
    {
        koreographerEventCount++;
        ConditionalDebug.Log($"[EnemyShootingManager] OnMusicalEnemyShoot triggered. Time: {Time.time}");
        if (managerTimeline != null && managerTimeline.timeScale != 0f)
        {
            ConditionalDebug.Log($"[EnemyShootingManager] Registered shootings: {staticEnemyShootings.Count}");
            int successfulShots = 0;
            foreach (var shooting in staticEnemyShootings)
            {
                if (shooting != null)
                {
                    shooting.Shoot();
                    successfulShots++;
                }
                else
                {
                    ConditionalDebug.LogWarning("[EnemyShootingManager] Null StaticEnemyShooting found in list");
                }
            }
            ConditionalDebug.Log($"[EnemyShootingManager] Successful shots: {successfulShots}/{staticEnemyShootings.Count}");
        }
        else
        {
            ConditionalDebug.Log("[EnemyShootingManager] Timeline is null or timeScale is 0");
        }
    }
}
