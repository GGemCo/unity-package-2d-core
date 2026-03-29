using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// DialogueBalloon 오브젝트를 재사용하기 위한 풀(Pool) 클래스입니다.
    /// 비활성 상태의 오브젝트를 재사용하거나 필요 시 새로 생성합니다.
    /// </summary>
    public class DialogueBalloonPool
    {
        private readonly GameObject _prefab;
        private readonly Transform _parent;
        private readonly List<GameObject> _pool = new();

        /// <summary>
        /// 말풍선 풀을 생성합니다.
        /// </summary>
        /// <param name="parent">생성된 말풍선 오브젝트를 배치할 부모 Transform입니다.</param>
        public DialogueBalloonPool(Transform parent = null)
        {
            _prefab = ConfigResources.DialogueBalloon.Load();
            _parent = parent;
        }

        /// <summary>
        /// 사용 가능한 말풍선 오브젝트를 반환합니다.
        /// 비활성 오브젝트가 있으면 재사용하고, 없으면 새로 생성합니다.
        /// </summary>
        /// <returns>활성화된 말풍선 GameObject입니다.</returns>
        public GameObject Get()
        {
            GameObject balloon = null;
            for (int i = 0; i < _pool.Count; i++)
            {
                if (!_pool[i].activeSelf)
                    balloon = _pool[i];
            }
            if (balloon == null)
            {
                balloon = Object.Instantiate(_prefab, _parent);
                _pool.Add(balloon);
            }

            balloon.SetActive(true);
            return balloon;
        }

        /// <summary>
        /// 사용이 끝난 말풍선을 풀로 반환합니다.
        /// 실제로는 비활성화하여 재사용 가능 상태로 만듭니다.
        /// </summary>
        /// <param name="balloon">반환할 말풍선 오브젝트입니다.</param>
        public void Return(GameObject balloon)
        {
            if (balloon == null) return;

            balloon.SetActive(false);
        }

        /// <summary>
        /// 풀에 있는 모든 말풍선 오브젝트를 제거하고 메모리에서 해제합니다.
        /// </summary>
        public void DestroyAll()
        {
            foreach (var balloon in _pool)
            {
                if (balloon != null)
                {
                    Object.Destroy(balloon);
                }
            }

            _pool.Clear();
        }
    }
}