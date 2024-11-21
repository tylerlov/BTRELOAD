using System;
using System.Collections.Generic;
using UnityEngine;

public class ObjectPool<T>
{
    private readonly Stack<T> pool;
    private readonly Func<T> createFunc;
    private readonly Action<T> actionOnGet;
    private readonly Action<T> actionOnRelease;
    private readonly Action<T> actionOnDestroy;
    private readonly int maxSize;

    public ObjectPool(Func<T> createFunc, Action<T> actionOnGet = null, Action<T> actionOnRelease = null, 
        Action<T> actionOnDestroy = null, int defaultCapacity = 10, int maxSize = 10000)
    {
        this.pool = new Stack<T>(defaultCapacity);
        this.createFunc = createFunc ?? throw new ArgumentNullException(nameof(createFunc));
        this.actionOnGet = actionOnGet;
        this.actionOnRelease = actionOnRelease;
        this.actionOnDestroy = actionOnDestroy;
        this.maxSize = maxSize;
    }

    public T Get()
    {
        T item = pool.Count > 0 ? pool.Pop() : createFunc();
        actionOnGet?.Invoke(item);
        return item;
    }

    public void Release(T item)
    {
        if (item == null) return;

        actionOnRelease?.Invoke(item);

        if (pool.Count < maxSize)
        {
            pool.Push(item);
        }
        else
        {
            actionOnDestroy?.Invoke(item);
        }
    }

    public void Clear()
    {
        if (actionOnDestroy != null)
        {
            foreach (var item in pool)
            {
                actionOnDestroy(item);
            }
        }
        pool.Clear();
    }
}
