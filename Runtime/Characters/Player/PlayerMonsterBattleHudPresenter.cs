using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 플레이어의 현재 교전 목록을 기준으로 몬스터 전투 HUD 표시 대상을 선택하고 UI를 갱신합니다.
    /// </summary>
    /// <remarks>
    /// 전역 몬스터 전투 HUD는 한 번에 하나의 몬스터만 표시할 수 있으므로,
    /// 개별 몬스터 UI 컨트롤러가 아니라 플레이어 교전 목록을 관찰하는 Presenter가 소유권을 가집니다.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class PlayerMonsterBattleHudPresenter : MonoBehaviour
    {
        private readonly List<Monster> _engagedMonsters = new List<Monster>(8);

        private Player _owner;
        private PlayerCombatEngagementTracker _tracker;
        private GGemCoMonsterSettings _monsterSettings;
        private UIWindowBattleHudMonster _battleHud;
        private Monster _currentMonster;
        private bool _isSubscribed;

        /// <summary>
        /// Presenter가 관찰할 플레이어와 전투 참여 목록을 초기화합니다.
        /// </summary>
        /// <param name="owner">전투 HUD의 기준이 되는 플레이어입니다.</param>
        /// <param name="tracker">플레이어의 현재 교전 목록입니다.</param>
        public void Initialize(Player owner, PlayerCombatEngagementTracker tracker)
        {
            if (_tracker != null && _tracker != tracker)
            {
                UnsubscribeTracker();
                HideHud();
            }

            _owner = owner;
            _tracker = tracker;
            RefreshRuntimeReferences();
            SubscribeTracker();
            RefreshHud();
        }

        /// <summary>
        /// 교전 목록과 설정을 기준으로 현재 표시해야 할 몬스터 HUD를 갱신합니다.
        /// </summary>
        /// <remarks>
        /// 이 함수는 교전 목록 변경 시점에 호출되며, 후보 목록 리스트를 재사용해 반복 할당을 피합니다.
        /// </remarks>
        public void RefreshHud()
        {
            RefreshRuntimeReferences();
            if (!isActiveAndEnabled ||
                _owner == null ||
                _tracker == null ||
                _monsterSettings == null ||
                !_monsterSettings.UseBattleHud ||
                _battleHud == null)
            {
                HideHud();
                return;
            }

            if (!TrySelectBattleHudMonster(out Monster selectedMonster))
            {
                HideHud();
                return;
            }

            if (_currentMonster == selectedMonster)
            {
                bool showSuperArmor = CanShowSuperArmor(selectedMonster);
                _battleHud.Show(true);
                _battleHud.Bind(selectedMonster, showSuperArmor);
                return;
            }

            _currentMonster = selectedMonster;
            bool shouldShowSuperArmor = CanShowSuperArmor(selectedMonster);
            _battleHud.Show(true);
            _battleHud.Bind(selectedMonster, shouldShowSuperArmor);
        }

        /// <summary>
        /// 현재 교전 중인 몬스터 중 전투 HUD에 표시할 최적 후보를 선택합니다.
        /// </summary>
        /// <param name="monster">선택된 몬스터입니다.</param>
        /// <returns>표시 가능한 몬스터를 찾았으면 <see langword="true"/>입니다.</returns>
        private bool TrySelectBattleHudMonster(out Monster monster)
        {
            monster = null;
            int count = _tracker.CopyEngagedMonsters(_engagedMonsters);
            if (count <= 0)
            {
                return false;
            }

            int bestGradePriority = int.MinValue;
            float bestDistanceSqr = float.PositiveInfinity;
            Vector3 ownerPosition = _owner != null ? _owner.transform.position : transform.position;

            for (int i = 0; i < _engagedMonsters.Count; i++)
            {
                Monster candidate = _engagedMonsters[i];
                if (!IsValidCandidate(candidate))
                {
                    continue;
                }

                int gradePriority = ResolveGradePriority(candidate.Grade);
                float distanceSqr = (candidate.transform.position - ownerPosition).sqrMagnitude;
                if (monster == null ||
                    gradePriority > bestGradePriority ||
                    (gradePriority == bestGradePriority && distanceSqr < bestDistanceSqr))
                {
                    monster = candidate;
                    bestGradePriority = gradePriority;
                    bestDistanceSqr = distanceSqr;
                }
            }

            return monster != null;
        }

        /// <summary>
        /// 지정한 몬스터가 전투 HUD 표시 후보인지 확인합니다.
        /// </summary>
        /// <param name="monster">확인할 몬스터입니다.</param>
        /// <returns>설정과 런타임 상태를 모두 만족하면 <see langword="true"/>입니다.</returns>
        private bool IsValidCandidate(Monster monster)
        {
            return monster != null &&
                   monster.gameObject.activeInHierarchy &&
                   !monster.IsStatusDead() &&
                   _monsterSettings != null &&
                   _monsterSettings.IsBattleHudEnabledFor(monster.Grade);
        }

        /// <summary>
        /// HUD 후보 정렬에 사용할 몬스터 등급 우선순위를 반환합니다.
        /// </summary>
        /// <param name="grade">몬스터 등급입니다.</param>
        /// <returns>값이 클수록 전투 HUD에 우선 표시됩니다.</returns>
        private static int ResolveGradePriority(CharacterConstants.Grade grade)
        {
            return grade switch
            {
                CharacterConstants.Grade.Boss => 300,
                CharacterConstants.Grade.Elite => 200,
                CharacterConstants.Grade.Common => 100,
                _ => 0,
            };
        }

        /// <summary>
        /// 지정한 몬스터의 전투 HUD 슈퍼아머 표시 여부를 계산합니다.
        /// </summary>
        /// <param name="monster">표시 대상 몬스터입니다.</param>
        /// <returns>슈퍼아머 HUD를 표시할 수 있으면 <see langword="true"/>입니다.</returns>
        private bool CanShowSuperArmor(Monster monster)
        {
            if (_monsterSettings == null || monster == null)
            {
                return false;
            }

            int maxSuperArmor = Mathf.Max(monster.TotalSuperArmor.Value, monster.CurrentSuperArmor.Value);
            return _monsterSettings.CanShowBattleHudSuperArmor(monster.Grade, maxSuperArmor);
        }

        /// <summary>
        /// 전투 HUD를 닫고 현재 바인딩된 몬스터를 해제합니다.
        /// </summary>
        private void HideHud()
        {
            _currentMonster = null;
            if (_battleHud == null)
            {
                RefreshRuntimeReferences();
            }

            if (_battleHud == null)
            {
                return;
            }

            _battleHud.Unbind();
            _battleHud.Show(false);
        }

        /// <summary>
        /// 현재 씬과 설정에서 필요한 런타임 참조를 다시 가져옵니다.
        /// </summary>
        private void RefreshRuntimeReferences()
        {
            _monsterSettings ??= AddressableLoaderSettings.Instance != null
                ? AddressableLoaderSettings.Instance.monsterSettings
                : null;

            SceneGame sceneGame = SceneGame.Instance;
            _battleHud = sceneGame != null && sceneGame.uIWindowManager != null
                ? sceneGame.uIWindowManager.GetUIWindowByUid<UIWindowBattleHudMonster>(UIWindowConstants.WindowUid.BattleHudMonster)
                : null;
        }

        /// <summary>
        /// 전투 참여 목록 변경 이벤트를 구독합니다.
        /// </summary>
        private void SubscribeTracker()
        {
            if (_isSubscribed || _tracker == null)
            {
                return;
            }

            _tracker.EngagementsChanged += OnEngagementsChanged;
            _isSubscribed = true;
        }

        /// <summary>
        /// 전투 참여 목록 변경 이벤트 구독을 해제합니다.
        /// </summary>
        private void UnsubscribeTracker()
        {
            if (!_isSubscribed || _tracker == null)
            {
                return;
            }

            _tracker.EngagementsChanged -= OnEngagementsChanged;
            _isSubscribed = false;
        }

        /// <summary>
        /// 전투 참여 목록이 바뀌었을 때 전투 HUD 표시 대상을 다시 선택합니다.
        /// </summary>
        private void OnEngagementsChanged()
        {
            RefreshHud();
        }

        private void OnDisable()
        {
            HideHud();
        }

        private void OnDestroy()
        {
            UnsubscribeTracker();
            HideHud();
        }
    }
}
