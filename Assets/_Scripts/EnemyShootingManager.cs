using System.Collections.Generic;
using SonicBloom.Koreo;
using UnityEngine;
using Chronos;

public class EnemyShootingManager : MonoBehaviour
{
    public static EnemyShootingManager Instance { get; private set; }

    private List<StaticEnemyShooting> staticEnemyShootings = new List<StaticEnemyShooting>();

    [SerializeField, EventID]
    private string eventID;
    private int shootCounter = 0;

    private Timeline managerTimeline;

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
            ConditionalDebug.Log($"Registered StaticEnemyShooting: {shooting.name}. Total count: {staticEnemyShootings.Count}");
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

    private void OnMusicalEnemyShoot(KoreographyEvent evt)
    {
        if (managerTimeline != null && managerTimeline.timeScale != 0f)
        {
            ConditionalDebug.Log($"OnMusicalEnemyShoot triggered. Registered shootings: {staticEnemyShootings.Count}");
            foreach (var shooting in staticEnemyShootings)
            {
                if (shooting != null)
                {
                    shooting.Shoot();
                    ConditionalDebug.Log($"Shoot called on {shooting.name}");
                }
                else
                {
                    ConditionalDebug.LogWarning("Null StaticEnemyShooting found in list");
                }
            }
        }
        else
        {
            ConditionalDebug.Log("OnMusicalEnemyShoot triggered, but managerTimeline is null or its timeScale is 0");
        }
    }
}