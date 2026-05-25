using System;
using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 플레이어 공격 영역에 몬스터가 진입했는지 여부를 관리합니다.
    /// - 여러 영역이 겹칠 수 있으므로, HitArea InstanceID를 기준으로 관리합니다.
    /// - 몬스터가 사망하거나 비활성화되어 TriggerExit가 호출되지 않는 경우를 대비해 조회 시점에 유효하지 않은 대상을 정리합니다.
    /// - Control 패키지(InputManager 등)는 이 상태만 조회하여 AutoMove Suspend 정책을 적용합니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerAttackAreaState : MonoBehaviour
    {
        /// <summary>
        /// 현재 플레이어 공격 영역 안에 살아있는 몬스터 HitArea가 남아 있는지 반환합니다.
        /// 조회 시점에 죽었거나 비활성화된 HitArea를 먼저 정리하여 AutoMove Suspend가 stale 상태로 유지되지 않도록 합니다.
        /// </summary>
        public bool IsInAttackArea
        {
            get
            {
                RemoveInvalidTargets();
                return HasTrackedAreas;
            }
        }

        /// <summary>
        /// IsInAttackArea 상태 변경 이벤트입니다.
        /// </summary>
        public event Action<bool> Changed;

        // 몬스터 HitArea 인스턴스 ID와 참조를 함께 저장합니다.
        private readonly Dictionary<int, CharacterHitArea> _overlapAreas = new Dictionary<int, CharacterHitArea>();
        private readonly List<int> _removeBuffer = new List<int>();

        private bool HasTrackedAreas => _overlapAreas.Count > 0;

        /// <summary>
        /// 몬스터 HitArea 영역에 진입했음을 기록합니다.
        /// </summary>
        /// <param name="area">진입한 HitArea 오브젝트 또는 HitArea를 포함한 오브젝트입니다.</param>
        public void Enter(GameObject area)
        {
            RemoveInvalidTargets();
            if (!TryResolveHitArea(area, out CharacterHitArea hitArea)) return;
            if (!IsValidHitArea(hitArea)) return;

            bool before = HasTrackedAreas;
            _overlapAreas[hitArea.gameObject.GetInstanceID()] = hitArea;
            NotifyIfChanged(before);
        }

        /// <summary>
        /// 몬스터 HitArea 영역에서 이탈했음을 기록합니다.
        /// </summary>
        /// <param name="area">이탈한 HitArea 오브젝트 또는 HitArea를 포함한 오브젝트입니다.</param>
        public void Exit(GameObject area)
        {
            if (area == null) return;

            bool before = HasTrackedAreas;
            int key = area.GetInstanceID();
            if (TryResolveHitArea(area, out CharacterHitArea hitArea))
            {
                key = hitArea.gameObject.GetInstanceID();
            }

            _overlapAreas.Remove(key);
            NotifyIfChanged(before);
        }

        /// <summary>
        /// 현재 추적 중인 HitArea 중 사망, 비활성화, Destroy 등으로 더 이상 공격 범위 정지 사유가 아닌 항목을 제거합니다.
        /// </summary>
        public void RemoveInvalidTargets()
        {
            if (_overlapAreas.Count <= 0) return;

            bool before = HasTrackedAreas;
            _removeBuffer.Clear();

            foreach (KeyValuePair<int, CharacterHitArea> pair in _overlapAreas)
            {
                if (IsValidHitArea(pair.Value)) continue;
                _removeBuffer.Add(pair.Key);
            }

            for (int i = 0; i < _removeBuffer.Count; i++)
            {
                _overlapAreas.Remove(_removeBuffer[i]);
            }

            _removeBuffer.Clear();
            NotifyIfChanged(before);
        }

        /// <summary>
        /// 강제로 상태를 초기화합니다.
        /// </summary>
        public void Clear()
        {
            bool before = HasTrackedAreas;
            _overlapAreas.Clear();
            NotifyIfChanged(before);
        }

        private void OnDisable()
        {
            // 플레이어 비활성화(맵 전환 등) 시 누락 상태 방지
            Clear();
        }

        /// <summary>
        /// 전달된 오브젝트에서 CharacterHitArea를 해석합니다.
        /// </summary>
        /// <param name="area">HitArea 오브젝트 또는 HitArea를 포함한 부모/자식 오브젝트입니다.</param>
        /// <param name="hitArea">해석된 HitArea입니다.</param>
        /// <returns>HitArea를 찾았으면 <see langword="true"/>를 반환합니다.</returns>
        private static bool TryResolveHitArea(GameObject area, out CharacterHitArea hitArea)
        {
            hitArea = null;
            if (area == null) return false;

            hitArea = area.GetComponent<CharacterHitArea>();
            if (hitArea != null) return true;

            hitArea = area.GetComponentInChildren<CharacterHitArea>();
            if (hitArea != null) return true;

            hitArea = area.GetComponentInParent<CharacterHitArea>();
            return hitArea != null;
        }

        /// <summary>
        /// 추적 중인 HitArea가 아직 AutoMove를 멈출 수 있는 유효한 몬스터 대상인지 확인합니다.
        /// </summary>
        /// <param name="hitArea">검증할 HitArea입니다.</param>
        /// <returns>살아있는 활성 대상이면 <see langword="true"/>를 반환합니다.</returns>
        private static bool IsValidHitArea(CharacterHitArea hitArea)
        {
            if (hitArea == null) return false;
            if (!hitArea.gameObject.activeInHierarchy) return false;

            CharacterBase target = hitArea.target;
            if (target == null) return false;
            if (!target.gameObject.activeInHierarchy) return false;
            if (target.IsStatusDead()) return false;

            return true;
        }

        /// <summary>
        /// 이전 상태와 현재 상태를 비교한 뒤 상태 변경 이벤트를 발행합니다.
        /// </summary>
        /// <param name="before">변경 전 IsInAttackArea 상태입니다.</param>
        private void NotifyIfChanged(bool before)
        {
            bool after = HasTrackedAreas;
            if (before == after) return;
            Changed?.Invoke(after);
        }
    }
}
