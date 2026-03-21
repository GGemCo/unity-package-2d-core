using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GGemCo2DCore
{
    public sealed class VfxPoolService
    {
        private sealed class PoolBucket
        {
            public readonly Stack<GameObject> Available = new Stack<GameObject>();
            public GameObject Prefab;
            public int MaxSize;
            public int TotalCount;
            public bool IsPrewarmed;
        }

        private readonly Transform _poolRoot;
        private readonly Dictionary<int, PoolBucket> _poolByUid = new Dictionary<int, PoolBucket>();

        public VfxPoolService(Transform parent)
        {
            var go = new GameObject("[VfxPool]");
            _poolRoot = go.transform;
            if (parent != null)
                _poolRoot.SetParent(parent);
            Object.DontDestroyOnLoad(go);
            go.SetActive(false);
        }

        public void Configure(StruckTableVfx info, GameObject prefab)
        {
            if (info == null || prefab == null || info.Uid <= 0)
                return;

            var bucket = GetOrCreateBucket(info.Uid);
            bucket.Prefab = prefab;
            bucket.MaxSize = Mathf.Max(0, info.PoolMaxSize);

            if (bucket.IsPrewarmed)
                return;

            int prewarmCount = Mathf.Max(0, info.PoolPrewarmCount);
            if (bucket.MaxSize > 0)
                prewarmCount = Mathf.Min(prewarmCount, bucket.MaxSize);

            for (int i = 0; i < prewarmCount; i++)
            {
                var instance = Object.Instantiate(prefab, _poolRoot);
                instance.SetActive(false);
                bucket.Available.Push(instance);
                bucket.TotalCount++;
            }

            bucket.IsPrewarmed = true;
        }

        public GameObject Acquire(int vfxUid, GameObject prefab)
        {
            if (prefab == null)
                return null;

            var bucket = GetOrCreateBucket(vfxUid);
            if (bucket.Prefab == null)
                bucket.Prefab = prefab;

            while (bucket.Available.Count > 0)
            {
                var pooled = bucket.Available.Pop();
                if (pooled == null)
                {
                    bucket.TotalCount = Mathf.Max(0, bucket.TotalCount - 1);
                    continue;
                }

                pooled.transform.SetParent(null, false);
                pooled.SetActive(false);
                return pooled;
            }

            var created = Object.Instantiate(bucket.Prefab != null ? bucket.Prefab : prefab);
            created.SetActive(false);
            bucket.TotalCount++;
            return created;
        }

        public void Release(int vfxUid, GameObject instance)
        {
            if (instance == null)
                return;

            var bucket = GetOrCreateBucket(vfxUid);
            int maxSize = bucket.MaxSize;
            if (maxSize > 0 && bucket.Available.Count >= maxSize)
            {
                bucket.TotalCount = Mathf.Max(0, bucket.TotalCount - 1);
                Object.Destroy(instance);
                return;
            }

            instance.SetActive(false);
            instance.transform.SetParent(_poolRoot, false);
            bucket.Available.Push(instance);
        }

        private PoolBucket GetOrCreateBucket(int vfxUid)
        {
            if (!_poolByUid.TryGetValue(vfxUid, out var bucket))
            {
                bucket = new PoolBucket();
                _poolByUid.Add(vfxUid, bucket);
            }

            return bucket;
        }
    }
}
