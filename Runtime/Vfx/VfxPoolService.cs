using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GGemCo2DCore
{
    public sealed class VfxPoolService
    {
        private readonly Transform _poolRoot;
        private readonly Dictionary<int, Stack<GameObject>> _poolByUid = new Dictionary<int, Stack<GameObject>>();

        public VfxPoolService(Transform parent)
        {
            var go = new GameObject("[VfxPool]");
            _poolRoot = go.transform;
            if (parent != null)
                _poolRoot.SetParent(parent);
            Object.DontDestroyOnLoad(go);
            go.SetActive(false);
        }

        public GameObject Acquire(int vfxUid, GameObject prefab)
        {
            if (prefab == null)
                return null;

            if (_poolByUid.TryGetValue(vfxUid, out var stack))
            {
                while (stack.Count > 0)
                {
                    var pooled = stack.Pop();
                    if (pooled == null)
                        continue;

                    pooled.transform.SetParent(null, false);
                    pooled.SetActive(false);
                    return pooled;
                }
            }

            var created = Object.Instantiate(prefab);
            created.SetActive(false);
            return created;
        }

        public void Release(int vfxUid, GameObject instance)
        {
            if (instance == null)
                return;

            if (!_poolByUid.TryGetValue(vfxUid, out var stack))
            {
                stack = new Stack<GameObject>();
                _poolByUid.Add(vfxUid, stack);
            }

            instance.SetActive(false);
            instance.transform.SetParent(_poolRoot, false);
            stack.Push(instance);
        }
    }
}
