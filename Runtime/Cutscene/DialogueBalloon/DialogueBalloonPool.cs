using System.Collections.Generic;
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
        private readonly Dictionary<int, object> _ownerByBalloonId = new();

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
        /// <param name="owner">말풍선을 점유하는 owner입니다. <see langword="null"/>이면 owner 추적을 생략합니다.</param>
        /// <returns>활성화된 말풍선 GameObject입니다.</returns>
        public GameObject Get(object owner = null)
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
            UpdateOwnerState(balloon, owner);
            return balloon;
        }

        /// <summary>
        /// 사용이 끝난 말풍선을 풀로 반환합니다.
        /// 실제로는 비활성화하여 재사용 가능 상태로 만듭니다.
        /// </summary>
        /// <param name="balloon">반환할 말풍선 오브젝트입니다.</param>
        /// <param name="owner">회수를 요청한 owner입니다. <see langword="null"/>이면 owner 검증 없이 회수합니다.</param>
        public void Return(GameObject balloon, object owner = null)
        {
            if (balloon == null) return;
            if (!CanReturnByOwner(balloon, owner))
            {
                return;
            }

            balloon.SetActive(false);
            ClearOwnerState(balloon);
        }

        /// <summary>
        /// 지정한 owner가 점유 중인 말풍선을 모두 회수합니다.
        /// owner가 일치하는 말풍선만 회수하여 다른 연출의 말풍선을 침범하지 않도록 보호합니다.
        /// </summary>
        /// <param name="owner">회수할 owner입니다.</param>
        public void ReturnAllByOwner(object owner)
        {
            if (owner == null)
            {
                return;
            }

            for (int i = 0; i < _pool.Count; i++)
            {
                GameObject balloon = _pool[i];
                if (balloon == null)
                {
                    continue;
                }

                int balloonId = balloon.GetInstanceID();
                if (!_ownerByBalloonId.TryGetValue(balloonId, out object currentOwner) ||
                    !ReferenceEquals(currentOwner, owner))
                {
                    continue;
                }

                balloon.SetActive(false);
                _ownerByBalloonId.Remove(balloonId);
            }
        }

        /// <summary>
        /// 풀에 등록된 모든 말풍선을 즉시 비활성화합니다.
        /// 이전 컷신에서 회수되지 못한 잔여 말풍선을 새 컷신 시작 전에 안전하게 정리할 때 사용합니다.
        /// </summary>
        public void ReturnAll()
        {
            for (int i = 0; i < _pool.Count; i++)
            {
                GameObject balloon = _pool[i];
                if (balloon == null)
                {
                    continue;
                }

                balloon.SetActive(false);
            }

            _ownerByBalloonId.Clear();
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
            _ownerByBalloonId.Clear();
        }

        /// <summary>
        /// 말풍선 owner 점유 정보를 갱신합니다.
        /// </summary>
        /// <param name="balloon">점유 정보를 갱신할 말풍선입니다.</param>
        /// <param name="owner">새로운 owner입니다.</param>
        private void UpdateOwnerState(GameObject balloon, object owner)
        {
            if (balloon == null)
            {
                return;
            }

            int balloonId = balloon.GetInstanceID();
            if (owner == null)
            {
                _ownerByBalloonId.Remove(balloonId);
                return;
            }

            _ownerByBalloonId[balloonId] = owner;
        }

        /// <summary>
        /// 말풍선 owner 점유 정보를 제거합니다.
        /// </summary>
        /// <param name="balloon">점유 정보를 제거할 말풍선입니다.</param>
        private void ClearOwnerState(GameObject balloon)
        {
            if (balloon == null)
            {
                return;
            }

            _ownerByBalloonId.Remove(balloon.GetInstanceID());
        }

        /// <summary>
        /// owner 검증을 통과한 회수 요청인지 확인합니다.
        /// owner를 전달하지 않으면 항상 회수를 허용합니다.
        /// </summary>
        /// <param name="balloon">회수할 말풍선입니다.</param>
        /// <param name="owner">회수를 요청한 owner입니다.</param>
        /// <returns>회수를 진행해도 안전하면 <see langword="true"/>를 반환합니다.</returns>
        private bool CanReturnByOwner(GameObject balloon, object owner)
        {
            if (balloon == null || owner == null)
            {
                return true;
            }

            int balloonId = balloon.GetInstanceID();
            return !_ownerByBalloonId.TryGetValue(balloonId, out object currentOwner) ||
                   ReferenceEquals(currentOwner, owner);
        }
    }
}
