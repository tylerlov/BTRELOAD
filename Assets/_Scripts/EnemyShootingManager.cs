using UnityEngine;
using System.Collections.Generic;
using SonicBloom.Koreo;

public class EnemyShootingManager : MonoBehaviour
{
    public static EnemyShootingManager Instance { get; private set; }

    private List<StaticEnemyShooting> staticEnemyShootings = new List<StaticEnemyShooting>();
    [SerializeField, EventID] private string eventID;
    private int shootCounter = 0;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Optional: Keep the manager across scenes
        }
        else
        {
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

    private void OnMusicalEnemyShoot(KoreographyEvent evt)
    {
        if (Time.timeScale != 0f)
        {
            foreach (var shooting in staticEnemyShootings)
            {
                shooting.Shoot();
            }
        }
    }
}