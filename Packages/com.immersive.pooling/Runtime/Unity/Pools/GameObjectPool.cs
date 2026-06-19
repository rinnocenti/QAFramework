using System;
using System.Collections.Generic;
using Immersive.Pooling.Contracts;
using Immersive.Pooling.Unity.Instances;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Immersive.Pooling.Unity.Pools
{
    public sealed class GameObjectPool
    {
        private readonly Dictionary<GameObject, Entry> _entries = new Dictionary<GameObject, Entry>();
        private readonly Stack<GameObject> _available = new Stack<GameObject>();

        public GameObjectPool(GameObject prefab, Transform parent = null, int initialCapacity = 0)
        {
            Prefab = prefab ?? throw new ArgumentNullException(nameof(prefab));

            if (initialCapacity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(initialCapacity));
            }

            Parent = parent;

            Prewarm(initialCapacity);
        }

        public GameObject Prefab { get; }

        public Transform Parent { get; }

        public int AvailableCount => CountAvailableEntries();

        public int TakenCount => TotalCount - AvailableCount;

        public int TotalCount => _entries.Count;

        public void Prewarm(int count)
        {
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            for (var i = 0; i < count; i++)
            {
                CreateInstance(available: true);
            }
        }

        public GameObject Take()
        {
            while (_available.Count > 0)
            {
                var instance = _available.Pop();

                if (instance == null)
                {
                    _entries.Remove(instance);
                    continue;
                }

                if (!_entries.TryGetValue(instance, out var entry) || !entry.isAvailable)
                {
                    _entries.Remove(instance);
                    continue;
                }

                entry.isAvailable = false;
                BindReturnHandle(instance);
                ActivateAndNotify(instance);
                return instance;
            }

            var created = CreateInstance(available: false);
            BindReturnHandle(created);
            ActivateAndNotify(created);
            return created;
        }

        public bool Return(GameObject instance)
        {
            if (instance == null)
            {
                return false;
            }

            if (!_entries.TryGetValue(instance, out var entry))
            {
                return false;
            }

            if (entry.isAvailable)
            {
                return false;
            }

            NotifyPoolables(instance, takenFromPool: false);
            MoveToParent(instance);
            instance.SetActive(false);

            entry.isAvailable = true;
            MarkReturnHandle(instance);
            _available.Push(instance);
            return true;
        }

        public void Clear()
        {
            var entries = new List<GameObject>(_entries.Keys);

            for (var i = 0; i < entries.Count; i++)
            {
                DestroyInstance(entries[i]);
            }

            _available.Clear();
            _entries.Clear();
        }

        private GameObject CreateInstance(bool available)
        {
            var instance = Parent == null
                ? Object.Instantiate(Prefab)
                : Object.Instantiate(Prefab, Parent);

            MoveToParent(instance);
            instance.SetActive(false);
            EnsureReturnHandle(instance);

            _entries.Add(instance, new Entry { isAvailable = available });

            if (available)
            {
                _available.Push(instance);
            }

            return instance;
        }

        private void ActivateAndNotify(GameObject instance)
        {
            instance.SetActive(true);
            NotifyPoolables(instance, takenFromPool: true);
        }

        private void NotifyPoolables(GameObject instance, bool takenFromPool)
        {
            var components = instance.GetComponentsInChildren<MonoBehaviour>(true);

            for (var i = 0; i < components.Length; i++)
            {
                if (components[i] is IPoolable poolable)
                {
                    if (takenFromPool)
                    {
                        poolable.OnTakenFromPool();
                    }
                    else
                    {
                        poolable.OnReturnedToPool();
                    }
                }
            }
        }

        private void MoveToParent(GameObject instance)
        {
            if (Parent != null)
            {
                instance.transform.SetParent(Parent, false);
            }
        }

        private void DestroyInstance(GameObject instance)
        {
            if (instance == null)
            {
                return;
            }

            ClearReturnHandle(instance);

            if (Application.isPlaying)
            {
                Object.Destroy(instance);
                return;
            }

            Object.DestroyImmediate(instance);
        }

        private sealed class Entry
        {
            public bool isAvailable;
        }

        private static PoolReturnHandle EnsureReturnHandle(GameObject instance)
        {
            var handle = instance.GetComponent<PoolReturnHandle>();

            if (handle != null)
            {
                return handle;
            }

            return instance.AddComponent<PoolReturnHandle>();
        }

        private void BindReturnHandle(GameObject instance)
        {
            var handle = EnsureReturnHandle(instance);
            handle.Bind(this);
        }

        private static void MarkReturnHandle(GameObject instance)
        {
            var handle = instance.GetComponent<PoolReturnHandle>();

            if (handle != null)
            {
                handle.MarkReturned();
            }
        }

        private static void ClearReturnHandle(GameObject instance)
        {
            var handle = instance.GetComponent<PoolReturnHandle>();

            if (handle != null)
            {
                handle.ClearBinding();
            }
        }

        private int CountAvailableEntries()
        {
            var count = 0;

            foreach (var entry in _entries.Values)
            {
                if (entry.isAvailable)
                {
                    count++;
                }
            }

            return count;
        }
    }
}
