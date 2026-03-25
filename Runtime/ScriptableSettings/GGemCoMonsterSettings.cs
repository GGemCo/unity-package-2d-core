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
        
        [Header("Stacks")]
        [HideInInspector]
        [Min(0)] public int maxIgnoreStacks = 0;
        
        [Header("Regen")]
        [Tooltip("스택이 깎인 이후, 회복을 시작하기까지 대기 시간(초)")]
        [HideInInspector]
        [Min(0f)] public float regenDelay = 0f;

        [Tooltip("회복 틱 간격(초)")]
        [HideInInspector]
        [Min(0.01f)] public float regenInterval = 0f;

        [Tooltip("틱당 회복량(스택)")]
        [HideInInspector]
        [Min(1)] public int regenPerTick = 0;

        [Header("Break")]
        [HideInInspector]
        public CharacterConstants.StaggerBreakResetMode breakResetMode = CharacterConstants.StaggerBreakResetMode.KeepZero;

        [Header("Optional - Anti multi-hit spam")]
        [Tooltip("같은 AttackId가 매우 짧은 시간에 여러 번 들어올 때 스택이 과도하게 깎이는 것을 방지(초). 0이면 비활성.")]
        [HideInInspector]
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

        public bool IsBattleHudEnabledFor(CharacterConstants.Grade grade)
        {
            if (!useBattleHud) return false;
            if (useBattleHudGradeMask == 0) return false;
            
            var flag = 1 << (int)grade;
            return (useBattleHudGradeMask & flag) != 0;
        }

        public GameObject GetMonsterHpBar(CharacterConstants.Grade grade)
        {
            foreach (var prefabHpBar in prefabHpBars)
            {
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

            useSpriteWhiteOverlay = false;
            spriteWhiteOverlayMaterial = null;
            spriteWhiteOverlayColor = Color.white;
            spriteWhiteOverlayFlashDuration = 0.08f;
        }
    }
}
