using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 몬스터 중심의 논리 감지 범위로 플레이어 후보를 수집하는 비할당 2D 센서입니다.
    /// </summary>
    /// <remarks>
    /// 기존 CharacterAttackRange Collider는 실제 일반 공격 피해 판정에만 사용하고,
    /// 선공 감지는 monster_combat_profile의 DetectionRangeX/Y를 기준으로 수행합니다.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class MonsterDetectionSensor2D : MonoBehaviour, IMonsterPoolLifecycle
    {
        private const int DetectionBufferCapacity = 32;
        private const float DefaultScanIntervalSeconds = 0.2f;

        private readonly Collider2D[] _results = new Collider2D[DetectionBufferCapacity];
        private Monster _owner;
        private Player _detectedPlayer;
        private ContactFilter2D _playerHitAreaFilter;
        private float _nextScanTime;

        /// <summary>
        /// 센서가 감지 후보를 전달할 소유 몬스터를 초기화합니다.
        /// </summary>
        /// <param name="owner">이 센서를 소유한 몬스터입니다.</param>
        public void Initialize(Monster owner)
        {
            _owner = owner;
            RebuildPlayerHitAreaFilter();
            ResetSensorState();
        }

        /// <summary>
        /// 설정된 주기에 맞춰 플레이어 감지 후보를 갱신합니다.
        /// </summary>
        private void Update()
        {
            if (Time.time < _nextScanTime)
            {
                return;
            }

            _nextScanTime = Time.time + DefaultScanIntervalSeconds;
            Scan();
        }

        /// <summary>
        /// 현재 감지 범위의 플레이어를 검색하고 진입/이탈 이벤트를 소유 몬스터에게 전달합니다.
        /// </summary>
        private void Scan()
        {
            if (_owner == null || !_owner.isActiveAndEnabled || _owner.IsStatusDead())
            {
                ClearDetectedPlayer(notifyOwner: false);
                return;
            }

            if (_owner.GetAttackType() != CharacterConstants.AttackType.AggroFirst)
            {
                // 선공 정책이 아닌 몬스터는 주기적인 물리 검색을 수행하지 않습니다.
                ClearDetectedPlayer(notifyOwner: true);
                return;
            }

            MonsterCombatRangeProfile profile = _owner.CombatRangeProfile;
            if (!profile.IsDetectionEnabled)
            {
                ClearDetectedPlayer(notifyOwner: true);
                return;
            }

            Player player = FindDetectedPlayer(profile);
            if (player != null)
            {
                bool changedTarget = _detectedPlayer != player;
                if (_detectedPlayer != null && changedTarget)
                {
                    Player previous = _detectedPlayer;
                    _detectedPlayer = null;
                    _owner.OnLostPlayerByDetectionRange(previous);
                }

                _detectedPlayer = player;
                bool needsDetectionThreat =
                    !_owner.HasThreatSource(player, MonsterThreatSource.DetectionRange);
                if (needsDetectionThreat)
                {
                    // 감지 원인만 별도로 등록하며, 피격/패트롤 Threat와 독립적으로 누적합니다.
                    _owner.OnDetectedPlayerByDetectionRange(player);
                }
                return;
            }

            if (_detectedPlayer == null)
            {
                return;
            }

            if (!profile.IsConfigured)
            {
                // 신규 프로필이 없는 기존 몬스터는 공격 Collider Trigger가 이탈해도 어그로를 유지하던 동작을 보존합니다.
                // 감지 후보 캐시만 정리하고, 전투 종료는 기존 BT/외부 정책에 맡깁니다.
                ClearDetectedPlayer(notifyOwner: false);
                return;
            }

            if (!_detectedPlayer.IsStatusDead())
            {
                bool isCurrentCombatTarget =
                    _owner.IsAggro() && _owner.CurrentCombatTarget == _detectedPlayer;

                if (profile.HasChaseLimit && isCurrentCombatTarget)
                {
                    if (ShouldKeepTrackedPlayerByChaseRange(profile, _detectedPlayer))
                    {
                        return;
                    }
                }
                else if (MonsterCombatRangeMath.IsWithinAxisAlignedRange(
                             _owner.transform,
                             _detectedPlayer.transform,
                             profile.DetectionExitRangeX,
                             profile.DetectionExitRangeY))
                {
                    return;
                }
            }

            ClearDetectedPlayer(notifyOwner: true);
        }

        /// <summary>
        /// 감지 이탈 범위를 벗어난 플레이어를 추적 거리 안에서 계속 유지할지 확인합니다.
        /// </summary>
        /// <param name="profile">현재 몬스터 전투 범위 프로필입니다.</param>
        /// <param name="player">현재 추적 중인 플레이어입니다.</param>
        /// <returns>현재 교전 타겟이며 ChaseRange 안에 있으면 <see langword="true"/>입니다.</returns>
        private bool ShouldKeepTrackedPlayerByChaseRange(
            MonsterCombatRangeProfile profile,
            Player player)
        {
            if (!profile.HasChaseLimit || player == null)
            {
                return false;
            }

            if (!_owner.IsAggro() || _owner.CurrentCombatTarget != player)
            {
                return false;
            }

            float distance2D = MonsterCombatRangeMath.GetDistance2D(_owner.transform, player.transform);
            return distance2D >= 0f && !profile.IsBeyondChaseRange(distance2D);
        }

        /// <summary>
        /// 감지 Box 안에서 살아 있는 플레이어를 검색합니다.
        /// </summary>
        /// <param name="profile">현재 몬스터 전투 범위 프로필입니다.</param>
        /// <returns>감지한 플레이어입니다. 찾지 못하면 <see langword="null"/>입니다.</returns>
        private Player FindDetectedPlayer(MonsterCombatRangeProfile profile)
        {
            Vector2 size = new Vector2(profile.DetectionRangeX * 2f, profile.DetectionRangeY * 2f);
            int hitCount = CompatPhysics2D.OverlapBoxNonAlloc(
                _owner.transform.position,
                size,
                0f,
                _playerHitAreaFilter,
                _results);

            for (int i = 0; i < hitCount; i++)
            {
                Collider2D hit = _results[i];
                if (hit == null)
                {
                    continue;
                }

                Player player = hit.GetComponentInParent<Player>();
                if (player == null || player.IsStatusDead())
                {
                    continue;
                }

                return player;
            }

            return null;
        }

        /// <summary>
        /// 플레이어 HitArea 레이어만 검색하도록 ContactFilter2D를 재구성합니다.
        /// </summary>
        private void RebuildPlayerHitAreaFilter()
        {
            string layerName = ConfigLayer.GetValue(ConfigLayer.Keys.HitAreaPlayer);
            int layerMask = LayerMask.GetMask(layerName);
            _playerHitAreaFilter = layerMask != 0
                ? CompatPhysics2D.CreateLayerFilter(layerMask, useTriggers: true)
                : CompatContactFilter2D.CreateNoFilter();
        }

        /// <summary>
        /// 현재 감지 중인 플레이어를 해제합니다.
        /// </summary>
        /// <param name="notifyOwner">소유 몬스터에 감지 이탈 이벤트를 전달할지 여부입니다.</param>
        private void ClearDetectedPlayer(bool notifyOwner)
        {
            Player previous = _detectedPlayer;
            _detectedPlayer = null;
            if (notifyOwner && previous != null && _owner != null)
            {
                _owner.OnLostPlayerByDetectionRange(previous);
            }
        }

        /// <summary>
        /// 풀에서 다시 대여될 때 이전 감지 상태를 초기화합니다.
        /// </summary>
        /// <param name="owner">대여된 몬스터입니다.</param>
        public void OnPoolRent(Monster owner)
        {
            _owner = owner;
            RebuildPlayerHitAreaFilter();
            ResetSensorState();
        }

        /// <summary>
        /// 풀로 반환되기 전에 이전 감지 상태를 제거합니다.
        /// </summary>
        /// <param name="owner">반환되는 몬스터입니다.</param>
        public void OnPoolReturn(Monster owner)
        {
            ClearDetectedPlayer(notifyOwner: false);
        }

        /// <summary>
        /// 다음 검색 시점과 감지 대상을 기본값으로 되돌립니다.
        /// </summary>
        /// <remarks>
        /// 많은 몬스터가 같은 프레임에 생성되어도 감지 쿼리가 한 프레임에 집중되지 않도록
        /// 인스턴스 ID를 기준으로 첫 검색 시점을 1회 분산합니다.
        /// </remarks>
        private void ResetSensorState()
        {
            _detectedPlayer = null;
            _nextScanTime = Time.time + ResolveInitialScanDelay();
        }

        /// <summary>
        /// 몬스터 인스턴스별로 고정된 첫 감지 지연 시간을 계산합니다.
        /// </summary>
        /// <returns>0 이상 감지 주기 미만의 초기 지연 시간입니다.</returns>
        private float ResolveInitialScanDelay()
        {
            int phase = Mathf.Abs(GetInstanceID() % 1000);
            return phase / 1000f * DefaultScanIntervalSeconds;
        }

        /// <summary>
        /// 컴포넌트가 비활성화될 때 감지 캐시만 제거합니다.
        /// </summary>
        /// <remarks>
        /// 컬링과 임시 비활성화가 전투 종료를 의미하지 않을 수 있으므로 소유 몬스터에는 이탈을 통지하지 않습니다.
        /// </remarks>
        private void OnDisable()
        {
            ClearDetectedPlayer(notifyOwner: false);
        }

#if UNITY_EDITOR
        /// <summary>
        /// 선택된 몬스터의 감지 진입/이탈 범위를 Scene 뷰에 표시합니다.
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            Monster owner = _owner != null ? _owner : GetComponent<Monster>();
            if (owner == null)
            {
                return;
            }

            MonsterCombatRangeProfile profile = owner.CombatRangeProfile;
            Vector3 center = owner.transform.position;

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(
                center,
                new Vector3(profile.DetectionRangeX * 2f, profile.DetectionRangeY * 2f, 0f));

            Gizmos.color = new Color(1f, 0.5f, 0f, 1f);
            Gizmos.DrawWireCube(
                center,
                new Vector3(profile.DetectionExitRangeX * 2f, profile.DetectionExitRangeY * 2f, 0f));
        }
#endif
    }
}
