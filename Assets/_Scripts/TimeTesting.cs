using System;
using System.Collections;
using System.Collections.Generic;
using Chronos;
using FMODUnity;
using SonicBloom.Koreo;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class TimeTesting : MonoBehaviour
{
    [EventID]
    public string eventIDHalftime;

    [Space]
    public StudioEventEmitter musicPlayback;

    // Start is called before the first frame update
    void Start()
    {
        Koreographer.Instance.RegisterForEvents(eventIDHalftime, UpdateTime);
    }

    private void Update() { }

    void UpdateTime(KoreographyEvent evt)
    {
        if (Time.timeScale != 0f && Keyboard.current[Key.H].wasPressedThisFrame)
        {
            StartCoroutine(onMusicalHalftime());
        }
    }

    private IEnumerator onMusicalHalftime()
    {
        ConditionalDebug.Log("Half time!");
        musicPlayback.EventInstance.setParameterByName("Slow", 1f);
        musicPlayback.EventInstance.setPitch(0.5f);
        Clock clock = Timekeeper.instance.Clock("Test");
        clock.localTimeScale = 0.1f;
        yield return new WaitForSeconds(2.791f);
        clock.localTimeScale = 1; // Normal
        musicPlayback.EventInstance.setPitch(1);
        musicPlayback.EventInstance.setParameterByName("Slow", 0f);
    }
}
