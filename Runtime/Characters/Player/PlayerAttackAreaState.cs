using System;
using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 플레이어 공격 영역에 몬스터가 진입했는지 여부를 관리합니다.
    /// - 여러 영역이 겹칠 수 있으므로, overlap set(InstanceID)으로 관리합니다.
    /// - Control 패키지(InputManager 등)는 이 상태만 조회하여 AutoMove Suspend 정책을 적용합니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerAttackAreaState : MonoBehaviour
    {
        public bool IsInAttackArea => _overlapAreaIds.Count > 0;

        /// <summary>
        /// IsInAttackArea 상태 변경 이벤트입니다.
        /// </summary>
        public event Action<bool> Changed;

        // 몬스터 HitArea 인스턴스 ID를 저장합니다.
        private readonly HashSet<int> _overlapAreaIds = new HashSet<int>();

        /// <summary>
        /// 몬스터 HitArea 영역에 진입했음을 기록합니다.
        /// </summary>
        public void Enter(GameObject area)
        {
            if (area == null) return;

            bool before = IsInAttackArea;
            _overlapAreaIds.Add(area.GetInstanceID());
            NotifyIfChanged(before);
        }

        /// <summary>
        /// 몬스터 HitArea 영역에서 이탈했음을 기록합니다.
        /// </summary>
        public void Exit(GameObject area)
        {
            if (area == null) return;

            bool before = IsInAttackArea;
            _overlapAreaIds.Remove(area.GetInstanceID());
            NotifyIfChanged(before);
        }

        /// <summary>
        /// 강제로 상태를 초기화합니다.
        /// </summary>
        public void Clear()
        {
            bool before = IsInAttackArea;
            _overlapAreaIds.Clear();
            NotifyIfChanged(before);
        }

        private void OnDisable()
        {
            // 플레이어 비활성화(맵 전환 등) 시 누락 상태 방지
            Clear();
        }

        private void NotifyIfChanged(bool before)
        {
            bool after = IsInAttackArea;
            if (before == after) return;
            Changed?.Invoke(after);
        }
    }
}
