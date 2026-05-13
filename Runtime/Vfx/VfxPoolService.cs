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
        private readonly Dictionary<int, PoolBucket> _poolByKey = new Dictionary<int, PoolBucket>();

        public VfxPoolService()
        {
            var go = new GameObject($"{ConfigDefine.NameSDK}_VfxPool");
            _poolRoot = go.transform;

            Object.DontDestroyOnLoad(go);
            go.SetActive(false);
        }

        /// <summary>
        /// 지정한 VFX 풀 버킷을 설정하고 필요한 경우 사전 생성합니다.
        /// </summary>
        /// <param name="info">풀 크기와 사전 생성 수를 제공하는 VFX 런타임 데이터입니다.</param>
        /// <param name="prefab">풀에서 생성할 VFX 프리팹입니다.</param>
        /// <param name="poolKey">동일 VfxUid를 Behaviour 정책별로 분리하기 위한 풀 키입니다. 0이면 VfxUid를 사용합니다.</param>
        public void Configure(VfxRuntimeData info, GameObject prefab, int poolKey = 0)
        {
            if (info == null || prefab == null || info.Uid <= 0)
                return;

            var bucket = GetOrCreateBucket(ResolvePoolKey(info.Uid, poolKey));
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

        /// <summary>
        /// 지정한 풀 키에서 VFX 인스턴스를 가져옵니다.
        /// </summary>
        /// <param name="poolKey">가져올 풀 버킷 키입니다.</param>
        /// <param name="prefab">버킷이 비어 있을 때 생성할 프리팹입니다.</param>
        /// <returns>비활성화된 VFX 인스턴스입니다. 생성할 수 없으면 null을 반환합니다.</returns>
        public GameObject Acquire(int poolKey, GameObject prefab)
        {
            if (prefab == null)
                return null;

            var bucket = GetOrCreateBucket(poolKey);
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

        /// <summary>
        /// VFX 인스턴스를 지정한 풀 키의 버킷으로 반환합니다.
        /// </summary>
        /// <param name="poolKey">반환할 풀 버킷 키입니다.</param>
        /// <param name="instance">반환할 VFX 인스턴스입니다.</param>
        public void Release(int poolKey, GameObject instance)
        {
            if (instance == null)
                return;

            var bucket = GetOrCreateBucket(poolKey);
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

        /// <summary>
        /// 풀 키에 해당하는 버킷을 가져오거나 새로 생성합니다.
        /// </summary>
        /// <param name="poolKey">VFX 풀 버킷 키입니다.</param>
        /// <returns>풀 버킷입니다.</returns>
        private PoolBucket GetOrCreateBucket(int poolKey)
        {
            if (!_poolByKey.TryGetValue(poolKey, out var bucket))
            {
                bucket = new PoolBucket();
                _poolByKey.Add(poolKey, bucket);
            }

            return bucket;
        }

        /// <summary>
        /// 명시적인 풀 키가 없으면 기존처럼 VfxUid를 풀 키로 사용합니다.
        /// </summary>
        /// <param name="vfxUid">VFX 테이블 Uid입니다.</param>
        /// <param name="poolKey">요청에서 전달한 풀 키입니다.</param>
        /// <returns>실제로 사용할 풀 키입니다.</returns>
        private static int ResolvePoolKey(int vfxUid, int poolKey)
        {
            return poolKey != 0 ? poolKey : vfxUid;
        }
    }
}
