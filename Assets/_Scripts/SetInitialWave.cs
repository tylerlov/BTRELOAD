using System;
using System.Collections;
using UltimateSpawner;
using UnityEngine;
using UnityEngine.Events;

public class InitializeWave : MonoBehaviour
{
    [HideInInspector]
    public WaveSpawnController waveSpawnController;
    public int startWave = 99; // Set this to the wave number you want to start at

    void Start()
    {
        // Automatically grab the WaveSpawnController component from the same GameObject
        waveSpawnController = GetComponent<WaveSpawnController>();

        if (waveSpawnController != null)
        {
            ConditionalDebug.Log("Starting at wave index: " + (startWave));
            waveSpawnController.StartWave(startWave); // Ensure this is the correct index
        }
        else
        {
            ConditionalDebug.LogError("WaveSpawnController is not assigned or not found on the GameObject!");
        }
    }
}
