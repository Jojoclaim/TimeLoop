using System;
using System.Collections.Generic;
using UnityEngine;

public class ObjectPool<T> where T : class
{
    private readonly Stack<T> pool = new Stack<T>();
    private readonly Func<T> createFunc;
    private readonly Action<T> onGet;
    private readonly Action<T> onRelease;
    private readonly Action<T> onDestroy;
    private readonly int maxSize;

    public ObjectPool(Func<T> createFunc, Action<T> onGet = null,
        Action<T> onRelease = null, Action<T> onDestroy = null, int maxSize = 100)
    {
        this.createFunc = createFunc;
        this.onGet = onGet;
        this.onRelease = onRelease;
        this.onDestroy = onDestroy;
        this.maxSize = maxSize;
    }

    public T Get()
    {
        T item = pool.Count > 0 ? pool.Pop() : createFunc();
        onGet?.Invoke(item);
        return item;
    }

    public void Release(T item)
    {
        if (pool.Count < maxSize)
        {
            onRelease?.Invoke(item);
            pool.Push(item);
        }
        else
        {
            onDestroy?.Invoke(item);
        }
    }

    public void Clear()
    {
        while (pool.Count > 0)
        {
            T item = pool.Pop();
            onDestroy?.Invoke(item);
        }
    }
}