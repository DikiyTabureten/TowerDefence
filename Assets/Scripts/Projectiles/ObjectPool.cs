using System;
using System.Collections.Generic;
using UnityEngine;

namespace TowerDefense
{
    /// <summary>
    /// Generic Object Pool. Objects are created once (via the factory delegate) and then
    /// reused: Rent() activates an object, Return() deactivates it and stores it for later.
    /// </summary>
    public sealed class ObjectPool<T> where T : Component, IPoolable
    {
        private readonly Func<T> _factory;
        private readonly Stack<T> _available = new Stack<T>();
        private readonly HashSet<T> _inPool = new HashSet<T>();

        public int CountAll { get; private set; }
        public int CountInactive => _available.Count;

        public ObjectPool(Func<T> factory, int prewarmCount = 0)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));

            for (int i = 0; i < prewarmCount; i++)
                Store(Create());
        }

        public T Rent()
        {
            T item = _available.Count > 0 ? _available.Pop() : Create();
            _inPool.Remove(item);

            item.gameObject.SetActive(true);
            item.OnRent();
            return item;
        }

        public void Return(T item)
        {
            if (item == null) return;

            if (_inPool.Contains(item))
            {
                Debug.LogWarning($"{item.name} was returned to the pool twice.", item);
                return;
            }

            item.OnReturn();
            Store(item);
        }

        private T Create()
        {
            CountAll++;
            return _factory();
        }

        private void Store(T item)
        {
            item.gameObject.SetActive(false);
            _inPool.Add(item);
            _available.Push(item);
        }
    }
}
