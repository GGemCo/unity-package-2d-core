using System;
using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    [CreateAssetMenu(fileName = ConfigScriptableObject.Monster.FileName, menuName = ConfigScriptableObject.Monster.MenuName, order = ConfigScriptableObject.Monster.Ordering)]
    public class GGemCoMonsterSettings : ScriptableObject
    {
        [Serializable]
        public class PrefabHpBar
        {
            [Tooltip("몬스터 등급")]
            public CharacterConstants.Grade grade;
            [Tooltip("몬스터 머리위에 보이는 HP 바.")]
            public GameObject prefabSlider;
        }
        
        [Header("Groggy")]
        [Tooltip("몬스터가 그로기 상태에 빠질때 적용할 어펙트 Uid")]
        public int monsterGroggyAffectUid;
        [Tooltip("슈퍼 아머가 0이 되었을 때, 그로기 상태가 됨. 그로기 상태를 유지하는 시간")]
        public float monsterGroggyAffectDuration;
        
        [Header("HP 바")]
        public List<PrefabHpBar> prefabHpBars;


        [Header("Hit Stop")]
        [Tooltip("자신이 타격을 성공시켰을 때 적용할 기본 경직 시간(초)")]
        [Min(0f)] public float defaultSelfHitStopSeconds = 0.03f;
        [Tooltip("피격 대상에게 적용할 기본 경직 시간(초)")]
        [Min(0f)] public float defaultReceiveHitStopSeconds = 0.05f;
        [Tooltip("경직 중 애니메이션을 현재 프레임에서 멈출지 여부")]
        public bool hitStopPauseAnimation = true;
        [Tooltip("경직 중 Rigidbody2D 물리를 멈출지 여부")]
        public bool hitStopFreezePhysics = true;
        [Tooltip("경직 중 DontControl 상태를 적용할지 여부")]
        public bool hitStopLockControl = true;
        [Tooltip("경직 중 DontMove 상태를 적용할지 여부")]
        public bool hitStopLockMovement = true;

        [Header("Sprite White Overlay")]
        [Tooltip("피격 시 Sprite White Overlay 효과를 사용할지 여부")]
        public bool useSpriteWhiteOverlay;
        [Tooltip("Sprite White Overlay에서 사용할 기본 호환 Material. 비워두면 기존 Material을 유지합니다.")]
        public Material spriteWhiteOverlayMaterial;
        [Tooltip("Sprite White Overlay 효과에 사용할 색상")]
        public Color spriteWhiteOverlayColor = Color.white;
        [Tooltip("피격 시 Sprite White Overlay 유지 시간(초)")]
        [Min(0.01f)]
        public float spriteWhiteOverlayFlashDuration = 0.08f;

        [Header("Battle HUD")]
        [Tooltip("몬스터 전투 HUD 사용 여부")]
        [SerializeField] private bool useBattleHud = true;
        [Tooltip("전투 HUD를 사용할 몬스터 등급(멀티 선택)")]
        [SerializeField] private int useBattleHudGradeMask = 0;

        [Header("사망 연출")]
        [Tooltip("사망 연출 사용 여부")]
        [SerializeField] private bool useCutsceneDie = true;
        [Tooltip("사망 연출 사용할 몬스터 등급(멀티 선택)")]
        [SerializeField] private int useCutsceneDieGradeMask = 0;
        [Tooltip("사망 연출 Cutscene Uid")]
        [SerializeField] private int cutsceneUidDie = 0;
        
        [Header("Supper Armor")]
        [Tooltip("스택이 깎인 이후, 회복을 시작하기까지 대기 시간(초)")]
        [Min(0f)] public float regenDelay = 0f;

        [Tooltip("회복 틱 간격(초)")]
        [Min(0.01f)] public float regenInterval = 0f;

        [Tooltip("틱당 회복량(스택)")]
        [Min(1)] public int regenPerTick = 0;

        public CharacterConstants.StaggerBreakResetMode breakResetMode = CharacterConstants.StaggerBreakResetMode.KeepZero;

        [Tooltip("같은 AttackId가 매우 짧은 시간에 여러 번 들어올 때 스택이 과도하게 깎이는 것을 방지(초). 0이면 비활성.")]
        [Min(0f)] public float perAttackConsumeCooldown = 0f;

        /// <summary>
        /// 몬스터 전투 HUD 사용 여부.
        /// </summary>
        public bool UseBattleHud => useBattleHud;

        /// <summary>
        /// 전투 HUD 적용 대상 등급(비트 마스크).
        /// - <see cref="CharacterConstants.Grade"/> enum index를 비트 위치로 사용합니다.
        /// </summary>
        public int UseBattleHudGradeMask => useBattleHudGradeMask;

        /// <summary>
        /// 지정한 몬스터 등급에서 전투 HUD를 사용할 수 있는지 확인합니다.
        /// </summary>
        /// <param name="grade">확인할 몬스터 등급입니다.</param>
        /// <returns>전투 HUD를 사용할 수 있으면 true입니다.</returns>
        public bool IsBattleHudEnabledFor(CharacterConstants.Grade grade)
        {
            if (!useBattleHud) return false;
            if (useBattleHudGradeMask == 0) return false;
            
            var flag = 1 << (int)grade;
            return (useBattleHudGradeMask & flag) != 0;
        }

        /// <summary>
        /// 지정한 몬스터 등급에 머리 위 HP 바 프리팹이 설정되어 있는지 확인합니다.
        /// </summary>
        /// <param name="grade">확인할 몬스터 등급입니다.</param>
        /// <returns>HP 바 프리팹이 설정되어 있으면 true입니다.</returns>
        public bool HasMonsterHpBar(CharacterConstants.Grade grade)
        {
            return GetMonsterHpBar(grade) != null;
        }

        /// <summary>
        /// 머리 위 Super Armor UI를 표시할 수 있는지 확인합니다.
        /// </summary>
        /// <param name="grade">확인할 몬스터 등급입니다.</param>
        /// <param name="maxSuperArmor">몬스터가 보유할 수 있는 최대 Super Armor 값입니다.</param>
        /// <returns>머리 위 Super Armor UI를 표시할 수 있으면 true입니다.</returns>
        public bool CanShowWorldSuperArmor(CharacterConstants.Grade grade, int maxSuperArmor)
        {
            return maxSuperArmor > 0 && HasMonsterHpBar(grade);
        }

        /// <summary>
        /// 전투 HUD 안의 Super Armor UI를 표시할 수 있는지 확인합니다.
        /// </summary>
        /// <param name="grade">확인할 몬스터 등급입니다.</param>
        /// <param name="maxSuperArmor">몬스터가 보유할 수 있는 최대 Super Armor 값입니다.</param>
        /// <returns>전투 HUD Super Armor UI를 표시할 수 있으면 true입니다.</returns>
        public bool CanShowBattleHudSuperArmor(CharacterConstants.Grade grade, int maxSuperArmor)
        {
            return maxSuperArmor > 0 && IsBattleHudEnabledFor(grade);
        }
        
        /// <summary>
        /// 몬스터 사망 연출 사용 여부.
        /// </summary>
        public bool UseCutsceneDie => useCutsceneDie;
        public int CutsceneUidDie => cutsceneUidDie;

        /// <summary>
        /// 몬스터 사망 연출 적용 대상 등급(비트 마스크).
        /// - <see cref="CharacterConstants.Grade"/> enum index를 비트 위치로 사용합니다.
        /// </summary>
        public int UseCutsceneDieGradeMask => useCutsceneDieGradeMask;

        public bool IsUseCutsceneDieEnabledFor(CharacterConstants.Grade grade)
        {
            if (!useCutsceneDie) return false;
            if (useCutsceneDieGradeMask == 0) return false;
            
            var flag = 1 << (int)grade;
            return (useCutsceneDieGradeMask & flag) != 0;
        }

        /// <summary>
        /// 지정한 몬스터 등급에 연결된 머리 위 HP 바 프리팹을 가져옵니다.
        /// </summary>
        /// <param name="grade">확인할 몬스터 등급입니다.</param>
        /// <returns>등급에 연결된 HP 바 프리팹입니다. 없으면 null입니다.</returns>
        public GameObject GetMonsterHpBar(CharacterConstants.Grade grade)
        {
            if (prefabHpBars == null) return null;
            foreach (var prefabHpBar in prefabHpBars)
            {
                if (prefabHpBar == null) continue;
                if (prefabHpBar.grade == grade) return prefabHpBar.prefabSlider;
            }
            return null;
        }
        
        /// <summary>
        /// 처음 생성 시 한 번만 실행됨
        /// </summary>
        private void Reset()
        {
            // 기본값: Common, Boss
            if (useBattleHudGradeMask == 0)
            {
                useBattleHudGradeMask = (1 << (int)CharacterConstants.Grade.Common)
                                     | (1 << (int)CharacterConstants.Grade.Boss);
            }
            if (useCutsceneDieGradeMask == 0)
            {
                useCutsceneDieGradeMask = (1 << (int)CharacterConstants.Grade.Elite)
                                          | (1 << (int)CharacterConstants.Grade.Boss);
            }

            defaultSelfHitStopSeconds = 0.03f;
            defaultReceiveHitStopSeconds = 0.05f;
            hitStopPauseAnimation = true;
            hitStopFreezePhysics = true;
            hitStopLockControl = true;
            hitStopLockMovement = true;

            useSpriteWhiteOverlay = false;
            spriteWhiteOverlayMaterial = null;
            spriteWhiteOverlayColor = Color.white;
            spriteWhiteOverlayFlashDuration = 0.08f;
        }
    }
}
