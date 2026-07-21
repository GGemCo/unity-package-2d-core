using System;
using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 컬링으로 페이드 아웃된 몬스터가 다시 페이드 인될 때 Brain 런타임 복귀 방식을 정의합니다.
    /// </summary>
    public enum MonsterCullingBrainResumePolicy : byte
    {
        /// <summary>
        /// 기존 런타임 상태를 유지하고 BT를 이어서 평가합니다.
        /// </summary>
        Continue = 0,

        /// <summary>
        /// 다음 페이드 인 시점에 런타임을 초기화하고 처음부터 BT를 평가합니다.
        /// </summary>
        ResetOnNextFadeIn = 1,
    }

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

        [Header("피격 VFX")]
        [Tooltip("몬스터 피격 시 재생할 VFX 설정 목록입니다. 여러 항목을 등록하면 조건에 맞는 VFX를 순서대로 재생합니다.")]
        public List<IncomingHitVfxSettings> incomingHitVfxList = new();

        [Header("Battle HUD")]
        [Tooltip("몬스터 전투 HUD 사용 여부")]
        [SerializeField] private bool useBattleHud = true;
        [Tooltip("전투 HUD를 사용할 몬스터 등급(멀티 선택)")]
        [SerializeField] private int useBattleHudGradeMask = 0;

        [Header("Monster Debug")]
        [SerializeField, DebugOption("몬스터 디버그 기능 전체 On/Off")]
        private bool enableMonsterDebug;

        /// <summary>
        /// 몬스터 디버그 기능 전체 사용 여부입니다.
        /// </summary>
        public bool EnableMonsterDebug => DebugOptionRuntimeUtility.Resolve(enableMonsterDebug);

        [SerializeField, DebugOption("스폰된 몬스터의 레벨 텍스트 출력")]
        private bool enableMonsterSpawnLevelText;

        /// <summary>
        /// 스폰된 몬스터의 레벨 텍스트 표시 여부입니다.
        /// </summary>
        public bool EnableMonsterSpawnLevelText => EnableMonsterDebug && DebugOptionRuntimeUtility.Resolve(enableMonsterSpawnLevelText);

        [Tooltip("몬스터 레벨 디버그 텍스트의 머리 위 기준 위치 보정값입니다.")]
        public Vector3 monsterSpawnLevelTextOffset = new Vector3(0f, 1.25f, 0f);
        [Tooltip("몬스터 레벨 디버그 텍스트 폰트 크기입니다.")]
        [Min(1)] public int monsterSpawnLevelTextFontSize = 18;
        [Tooltip("몬스터 레벨 디버그 텍스트 색상입니다.")]
        public Color monsterSpawnLevelTextColor = Color.yellow;

        [SerializeField, DebugOption("스폰된 몬스터의 HP 숫자 텍스트 출력")]
        private bool enableMonsterSpawnHpText;

        /// <summary>
        /// 스폰된 몬스터의 HP 숫자 텍스트 표시 여부입니다.
        /// </summary>
        public bool EnableMonsterSpawnHpText => EnableMonsterDebug && DebugOptionRuntimeUtility.Resolve(enableMonsterSpawnHpText);

        [Tooltip("몬스터 HP 디버그 텍스트의 머리 위 기준 위치 보정값입니다.")]
        public Vector3 monsterSpawnHpTextOffset = new Vector3(0f, 0.95f, 0f);
        [Tooltip("몬스터 HP 디버그 텍스트 폰트 크기입니다.")]
        [Min(1)] public int monsterSpawnHpTextFontSize = 16;
        [Tooltip("몬스터 HP 디버그 텍스트 색상입니다.")]
        public Color monsterSpawnHpTextColor = Color.white;
        [Tooltip("몬스터 HP 디버그 텍스트 형식입니다. {0}=현재 HP, {1}=최대 HP")]
        public string monsterSpawnHpTextFormat = "{0} / {1}";

        [Header("사망 연출")]
        [Tooltip("사망 연출 사용 여부")]
        [SerializeField] private bool useCutsceneDie = true;
        [Tooltip("사망 연출 사용할 몬스터 등급(멀티 선택)")]
        [SerializeField] private int useCutsceneDieGradeMask = 0;
        [Tooltip("사망 연출 Cutscene Uid")]
        [SerializeField] private int cutsceneUidDie = 0;
        
        [Header("Culling Brain")]
        [Tooltip("컬링 Fade Out 이후 다음 Fade In에서 Brain 런타임을 어떻게 복귀할지 선택합니다.")]
        [SerializeField] private MonsterCullingBrainResumePolicy cullingBrainResumePolicy = MonsterCullingBrainResumePolicy.Continue;
        [Tooltip("컬링 복귀 시 Brain 초기화를 수행할 때 어그로 판정도 함께 초기화할지 여부입니다.")]
        [SerializeField] private bool resetAggroOnCullingBrainReset = false;
        [Tooltip("컬링 복귀 시 Brain 초기화를 수행할 때 몬스터 위치를 원래 리젠 좌표로 되돌릴지 여부입니다.")]
        [SerializeField] private bool resetToRegenPositionOnCullingBrainReset = false;

        [Header("Supper Armor")]
        [Tooltip("스택이 깎인 이후, 회복을 시작하기까지 대기 시간(초)")]
        [Min(0f)] public float regenDelay = 0f;

        [Tooltip("회복 틱 간격(초)")]
        [Min(0.01f)] public float regenInterval = 0f;

        [Tooltip("틱당 회복량(스택)")]
        [Min(1)] public int regenPerTick = 0;

        public CharacterConstants.StaggerBreakResetMode breakResetMode = CharacterConstants.StaggerBreakResetMode.KeepZero;
        [Tooltip("슈퍼아머 브레이크 시 breakResetMode를 적용할 몬스터 등급(멀티 선택). 0이면 모든 등급에 적용됩니다.")]
        [SerializeField] private int breakResetModeGradeMask = 0;

        [Tooltip("같은 AttackId가 매우 짧은 시간에 여러 번 들어올 때 스택이 과도하게 깎이는 것을 방지(초). 0이면 비활성.")]
        [Min(0f)] public float perAttackConsumeCooldown = 0f;

        [Tooltip("슈퍼 아머가 0이 되었을 때, 그로기 상태가 됨. 적용할 어펙트 Uid")]
        public int monsterGroggyAffectUid;
        [Tooltip("슈퍼 아머가 0이 되었을 때, 그로기 상태가 됨. 적용할 어펙트 유지 시간")]
        public float monsterGroggyAffectDuration;

        /// <summary>
        /// 슈퍼아머 브레이크 리셋 모드 적용 대상 등급(비트 마스크)입니다.
        /// - <see cref="CharacterConstants.Grade"/> enum index를 비트 위치로 사용합니다.
        /// - 0이면 하위 호환을 위해 모든 등급에 적용합니다.
        /// </summary>
        public int BreakResetModeGradeMask => breakResetModeGradeMask;

        /// <summary>
        /// 지정한 등급에 breakResetMode를 적용할지 여부를 반환합니다.
        /// </summary>
        /// <param name="grade">확인할 몬스터 등급입니다.</param>
        /// <returns>해당 등급에 breakResetMode를 적용하면 true입니다.</returns>
        public bool IsBreakResetModeEnabledFor(CharacterConstants.Grade grade)
        {
            // 기존 에셋(필드 미직렬화)과의 호환을 위해 0은 "전체 적용"으로 해석합니다.
            if (breakResetModeGradeMask == 0) return true;

            var flag = 1 << (int)grade;
            return (breakResetModeGradeMask & flag) != 0;
        }

        /// <summary>
        /// 지정한 등급에 맞는 슈퍼아머 브레이크 리셋 모드를 반환합니다.
        /// </summary>
        /// <param name="grade">모드를 판정할 몬스터 등급입니다.</param>
        /// <returns>적용 대상이면 설정 모드, 아니면 KeepZero를 반환합니다.</returns>
        public CharacterConstants.StaggerBreakResetMode ResolveBreakResetMode(CharacterConstants.Grade grade)
        {
            return IsBreakResetModeEnabledFor(grade)
                ? breakResetMode
                : CharacterConstants.StaggerBreakResetMode.KeepZero;
        }

        /// <summary>
        /// 몬스터 전투 HUD 사용 여부.
        /// </summary>
        public bool UseBattleHud => useBattleHud;

        /// <summary>
        /// 컬링 페이드 인 시 Brain 런타임 복귀 정책입니다.
        /// </summary>
        public MonsterCullingBrainResumePolicy CullingBrainResumePolicy => cullingBrainResumePolicy;

        /// <summary>
        /// 컬링 복귀 시 Brain 초기화와 함께 어그로 판정까지 초기화할지 여부입니다.
        /// </summary>
        public bool ResetAggroOnCullingBrainReset => resetAggroOnCullingBrainReset;

        /// <summary>
        /// 컬링 복귀 시 Brain 초기화와 함께 리젠 좌표로 위치를 되돌릴지 여부입니다.
        /// </summary>
        public bool ResetToRegenPositionOnCullingBrainReset => resetToRegenPositionOnCullingBrainReset;

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
        /// 스폰된 몬스터의 레벨 디버그 텍스트를 표시할 수 있는지 확인합니다.
        /// </summary>
        /// <returns>레벨 디버그 텍스트를 표시할 수 있으면 true입니다.</returns>
        public bool CanShowSpawnLevelDebugText()
        {
            return EnableMonsterSpawnLevelText;
        }

        /// <summary>
        /// 스폰된 몬스터의 HP 숫자 디버그 텍스트를 표시할 수 있는지 확인합니다.
        /// </summary>
        /// <returns>HP 숫자 디버그 텍스트를 표시할 수 있으면 true입니다.</returns>
        public bool CanShowSpawnHpDebugText()
        {
            return EnableMonsterSpawnHpText;
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
        /// Inspector 값이 변경되거나 에셋이 로드될 때 피격 VFX 레거시 데이터를 신규 구조로 보정합니다.
        /// </summary>
        /// <remarks>
        /// 이전 버전의 <see cref="IncomingHitVfxSettings"/>는 VFX 생성 필드를 직접 가지고 있었습니다.
        /// 현재는 <see cref="StruckAnimationEventVfx"/>를 함께 사용하므로, 기존 에셋을 열었을 때 값이 유실되지 않도록
        /// 숨김 레거시 필드 값을 신규 payload로 복사합니다.
        /// </remarks>
        private void OnValidate()
        {
            MigrateIncomingHitVfxList();
            NormalizeMonsterDebugSettings();
        }

        /// <summary>
        /// 설정 에셋이 로드될 때 몬스터 디버그 표시 설정의 기본 유효 범위를 보정합니다.
        /// </summary>
        private void OnEnable()
        {
            NormalizeMonsterDebugSettings();
        }

        /// <summary>
        /// 몬스터 피격 VFX 목록의 레거시 저장 데이터를 신규 VFX payload 구조로 변환합니다.
        /// </summary>
        private void MigrateIncomingHitVfxList()
        {
            if (incomingHitVfxList == null)
            {
                return;
            }

            // List 안의 struct는 값을 직접 수정할 수 없으므로 복사 후 다시 대입합니다.
            for (int i = 0; i < incomingHitVfxList.Count; i++)
            {
                IncomingHitVfxSettings migrated = incomingHitVfxList[i].MigrateLegacyVfxIfNeeded();
                incomingHitVfxList[i] = migrated;
            }
        }

        /// <summary>
        /// 몬스터 디버그 표시 설정이 유효한 범위를 벗어나지 않도록 보정합니다.
        /// </summary>
        private void NormalizeMonsterDebugSettings()
        {
            if (monsterSpawnLevelTextFontSize <= 0)
            {
                monsterSpawnLevelTextFontSize = 18;
            }

            if (monsterSpawnHpTextFontSize <= 0)
            {
                monsterSpawnHpTextFontSize = 16;
            }

            if (string.IsNullOrWhiteSpace(monsterSpawnHpTextFormat))
            {
                monsterSpawnHpTextFormat = "{0} / {1}";
            }
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
            incomingHitVfxList = new List<IncomingHitVfxSettings>();
            cullingBrainResumePolicy = MonsterCullingBrainResumePolicy.Continue;
            resetAggroOnCullingBrainReset = false;
            resetToRegenPositionOnCullingBrainReset = false;
            enableMonsterDebug = false;
            enableMonsterSpawnLevelText = false;
            monsterSpawnLevelTextOffset = new Vector3(0f, 1.25f, 0f);
            monsterSpawnLevelTextFontSize = 18;
            monsterSpawnLevelTextColor = Color.yellow;
            enableMonsterSpawnHpText = false;
            monsterSpawnHpTextOffset = new Vector3(0f, 0.95f, 0f);
            monsterSpawnHpTextFontSize = 16;
            monsterSpawnHpTextColor = Color.white;
            monsterSpawnHpTextFormat = "{0} / {1}";
        }
    }
}
