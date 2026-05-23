using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// <see cref="CharacterBase"/>의 캐릭터 Body 충돌 정책 연결을 담당하는 partial 구현입니다.
    /// </summary>
    public partial class CharacterBase
    {
        private CharacterCollisionController _collisionController;

        /// <summary>
        /// 현재 캐릭터의 Body 충돌 컨트롤러를 반환합니다.
        /// </summary>
        public CharacterCollisionController CollisionController => _collisionController;

        /// <summary>
        /// 캐릭터 Body 충돌 컨트롤러를 보장하고 초기화합니다.
        /// </summary>
        private void EnsureCharacterCollisionController()
        {
            _collisionController = GetComponent<CharacterCollisionController>();
            if (_collisionController == null)
            {
                _collisionController = gameObject.AddComponent<CharacterCollisionController>();
            }

            _collisionController.Initialize(this, colliderMapObject);
        }

        /// <summary>
        /// 캐릭터 타입, Body Collider 상태에 맞춰 충돌 레이어와 캐시를 갱신합니다.
        /// </summary>
        public void RefreshCharacterBodyCollision()
        {
            if (_collisionController == null)
            {
                EnsureCharacterCollisionController();
                return;
            }

            _collisionController.Refresh();
        }

        /// <summary>
        /// 요청된 이동량을 캐릭터 Body 충돌 정책에 맞게 보정합니다.
        /// </summary>
        /// <param name="requestedDelta">월드 기준 요청 이동량입니다.</param>
        /// <param name="resolvedDelta">충돌을 고려해 보정된 이동량입니다.</param>
        /// <returns>일부라도 이동 가능하면 true, 완전히 차단되면 false입니다.</returns>
        public bool TryResolveCharacterBodyMove(Vector3 requestedDelta, out Vector3 resolvedDelta)
        {
            resolvedDelta = requestedDelta;

            if (requestedDelta.sqrMagnitude <= 0.000001f)
                return true;

            if (_collisionController == null)
            {
                EnsureCharacterCollisionController();
            }

            return _collisionController == null || _collisionController.TryResolveMove(requestedDelta, out resolvedDelta);
        }

        /// <summary>
        /// 현재 Body Collider가 다른 캐릭터와 겹친 상태이면 정책에 따라 부드럽게 분리합니다.
        /// </summary>
        /// <param name="multiplier">이번 호출에서 적용할 분리 강도 배율입니다.</param>
        /// <returns>분리 이동을 적용했으면 true입니다.</returns>
        public bool TrySeparateCharacterBodyOverlaps(float multiplier = 1f)
        {
            if (_collisionController == null)
            {
                EnsureCharacterCollisionController();
            }

            return _collisionController != null && _collisionController.TrySeparateOverlaps(multiplier);
        }

        /// <summary>
        /// 점프 착지 등 특정 순간에 짧은 시간 동안 강화된 겹침 해소를 요청합니다.
        /// </summary>
        public void RequestLandingCharacterBodySeparation()
        {
            if (_collisionController == null)
            {
                EnsureCharacterCollisionController();
            }

            _collisionController?.RequestLandingSeparation();
        }
    }
}
