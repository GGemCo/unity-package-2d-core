using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 몬스터를 맵 Encounter 그룹에 등록하고 그룹 활성화 및 지원 어그로를 연결합니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MonsterEncounterMember : MonoBehaviour, IMonsterPoolLifecycle
    {
        private Monster _owner;
        private int _encounterId;
        private MonsterEncounterProfile _profile;
        private bool _registered;

        /// <summary>현재 등록된 맵 Encounter ID입니다.</summary>
        public int EncounterId => _encounterId;

        /// <summary>현재 적용된 Encounter 정책입니다.</summary>
        public MonsterEncounterProfile Profile => _profile;

        /// <summary>정적 레지스트리에 계속 남아 있을 수 있는 유효한 멤버인지 여부입니다.</summary>
        public bool CanRemainRegistered =>
            _registered &&
            _encounterId > 0 &&
            _owner != null;

        /// <summary>현재 Encounter 알림을 받아 전투를 시작할 수 있는지 여부입니다.</summary>
        public bool CanReceiveEncounterAlert =>
            CanRemainRegistered &&
            _owner.isActiveAndEnabled &&
            !_owner.IsStatusDead() &&
            !_owner.IsLeashReturnLocked;

        /// <summary>
        /// Encounter 멤버를 소유할 몬스터를 연결합니다.
        /// </summary>
        /// <param name="owner">Encounter 그룹에 참여할 몬스터입니다.</param>
        public void Initialize(Monster owner)
        {
            _owner = owner;
        }

        /// <summary>
        /// 맵 배치 데이터와 전투 프로필을 기준으로 Encounter 그룹을 갱신합니다.
        /// </summary>
        /// <param name="patrolData">Encounter ID가 포함된 맵 배치 데이터입니다.</param>
        /// <param name="profile">그룹 활성화와 지원 어그로 정책입니다.</param>
        public void Configure(PatrolData patrolData, MonsterEncounterProfile profile)
        {
            int nextEncounterId = patrolData != null ? Mathf.Max(0, patrolData.encounterId) : 0;
            if (_registered && _encounterId != nextEncounterId)
            {
                MonsterEncounterRegistry.Unregister(this);
                _registered = false;
            }

            _encounterId = nextEncounterId;
            _profile = profile;
            TryRegister();
        }

        /// <summary>
        /// Encounter 그룹 활성화 Threat를 소유 몬스터에게 전달합니다.
        /// </summary>
        /// <param name="target">그룹이 함께 교전할 대상입니다.</param>
        /// <returns>Encounter Threat가 적용되었으면 <see langword="true"/>입니다.</returns>
        public bool ReceiveEncounterActivation(CharacterBase target)
        {
            return CanReceiveEncounterAlert && _owner.OnDetectedTargetByEncounter(target, _profile.EncounterThreat);
        }

        /// <summary>
        /// Encounter 그룹 이탈 정책에 따라 해당 원인의 Threat를 제거합니다.
        /// </summary>
        /// <param name="target">Encounter Threat를 제거할 대상입니다.</param>
        /// <returns>Threat가 실제로 제거되었으면 <see langword="true"/>입니다.</returns>
        public bool RemoveEncounterActivation(CharacterBase target)
        {
            return _owner != null && _owner.OnLostTargetByEncounter(target);
        }

        /// <summary>
        /// 소유 몬스터가 새 Threat 대상을 얻으면 가까운 같은 그룹 동료에게 지원 알림을 전달합니다.
        /// </summary>
        /// <param name="target">동료에게 공유할 전투 대상입니다.</param>
        public void NotifyOwnerEngaged(CharacterBase target)
        {
            if (!CanReceiveEncounterAlert || target == null)
            {
                return;
            }

            MonsterEncounterRegistry.AlertAssistants(this, target);
        }

        private void TryRegister()
        {
            if (_registered || _encounterId <= 0 || _owner == null)
            {
                return;
            }

            _registered = true;
            MonsterEncounterRegistry.Register(this);
        }

        private void OnDestroy()
        {
            Unregister();
        }

        private void Unregister()
        {
            if (!_registered)
            {
                return;
            }

            MonsterEncounterRegistry.Unregister(this);
            _registered = false;
        }

        /// <inheritdoc />
        public void OnPoolRent(Monster owner)
        {
            Unregister();
            _owner = owner;
            _encounterId = 0;
        }

        /// <inheritdoc />
        public void OnPoolReturn(Monster owner)
        {
            Unregister();
            _encounterId = 0;
        }
    }
}
