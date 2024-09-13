using System;
using System.Collections.Generic;
using UnityEngine;

public class EventManager : MonoBehaviour
{
    private Dictionary<string, Delegate> eventDictionary = new Dictionary<string, Delegate>();

    public static EventManager Instance { get; private set; }
    public static readonly string TransCamOnEvent = "TransCamOn";
    public static readonly string StartingTransitionEvent = "StartingTransition";

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

    public void AddListener<T>(string eventName, Action<T> listener)
    {
        if (!eventDictionary.ContainsKey(eventName))
        {
            eventDictionary[eventName] = null;
        }
        eventDictionary[eventName] = (Action<T>)eventDictionary[eventName] + listener;
    }

    public void RemoveListener<T>(string eventName, Action<T> listener)
    {
        if (eventDictionary.ContainsKey(eventName))
        {
            var currentDelegate = eventDictionary[eventName] as Action<T>;
            if (currentDelegate != null)
            {
                currentDelegate -= listener;
                if (currentDelegate == null)
                {
                    eventDictionary.Remove(eventName);
                }
                else
                {
                    eventDictionary[eventName] = currentDelegate;
                }
            }
        }
    }

    public void TriggerEvent<T>(string eventName, T arg)
    {
        if (eventDictionary.TryGetValue(eventName, out Delegate thisEvent))
        {
            var callback = thisEvent as Action<T>;
            callback?.Invoke(arg);
        }
    }

    // New method for triggering events without parameters
    public void TriggerEvent(string eventName)
    {
        if (eventDictionary.TryGetValue(eventName, out Delegate thisEvent))
        {
            var callback = thisEvent as Action;
            callback?.Invoke();
        }
    }

    // Keep the parameterless methods for backward compatibility
    public void AddListener(string eventName, Action listener)
    {
        AddListener<object>(eventName, (_) => listener());
    }

    public void RemoveListener(string eventName, Action listener)
    {
        RemoveListener<object>(eventName, (_) => listener());
    }

    public void UnsubscribeFromAllEvents(object subscriber)
    {
        List<string> keysToRemove = new List<string>();

        foreach (var kvp in eventDictionary)
        {
            var delegates = kvp.Value.GetInvocationList();
            bool delegateRemoved = false;

            foreach (var del in delegates)
            {
                if (del.Target == subscriber)
                {
                    eventDictionary[kvp.Key] = Delegate.Remove(eventDictionary[kvp.Key], del);
                    delegateRemoved = true;
                }
            }

            if (delegateRemoved && eventDictionary[kvp.Key] == null)
            {
                keysToRemove.Add(kvp.Key);
            }
        }

        foreach (var key in keysToRemove)
        {
            eventDictionary.Remove(key);
        }
    }
}
