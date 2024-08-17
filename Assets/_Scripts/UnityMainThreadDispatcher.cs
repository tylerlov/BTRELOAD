using UnityEngine;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;

public class UnityMainThreadDispatcher : MonoBehaviour
{
    private static UnityMainThreadDispatcher _instance;
    private readonly Queue<Action> _executionQueue = new Queue<Action>();

    public static UnityMainThreadDispatcher Instance()
    {
        if (!_instance)
        {
            _instance = FindObjectOfType<UnityMainThreadDispatcher>();
            if (!_instance)
            {
                GameObject go = new GameObject("UnityMainThreadDispatcher");
                _instance = go.AddComponent<UnityMainThreadDispatcher>();
                DontDestroyOnLoad(go);
            }
        }
        return _instance;
    }

    private void Update()
    {
        lock(_executionQueue)
        {
            while (_executionQueue.Count > 0)
            {
                _executionQueue.Dequeue().Invoke();
            }
        }
    }

    public void Enqueue(Action action)
    {
        lock(_executionQueue)
        {
            _executionQueue.Enqueue(action);
        }
    }

    public Task EnqueueAsync(Action action)
    {
        var tcs = new TaskCompletionSource<bool>();
        Enqueue(() =>
        {
            action();
            tcs.SetResult(true);
        });
        return tcs.Task;
    }
}