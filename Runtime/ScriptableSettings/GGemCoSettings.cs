using System;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// GGemCo 메인 설정
    /// </summary>
    public enum InputSystemType
    {
        None,
        OldInputManager,
        NewInputSystem,
        Both
    }


    /// <summary>
    /// 맵에 배치된 몬스터가 모두 사망했을 때 실행할 맵 종료 정책 설정입니다.
    /// 전역 ScriptableObject에는 정책값만 보관하고, 실제 감시와 실행은 런타임 컨트롤러가 담당합니다.
    /// </summary>
    [Serializable]
    public sealed class MapClearExitPolicySettings
    {
        [Tooltip("현재 맵의 모든 몬스터가 사망하면 맵 종료 연출을 실행할지 여부입니다.")]
        public bool enabled;

        [Tooltip("정책이 활성화된 현재 맵에서 몬스터 사망 후 리젠 예약을 막을지 여부입니다.")]
        public bool suppressMonsterRespawn = true;

        [Tooltip("플레이어가 처치한 몬스터 사망 이벤트만 맵 클리어 판정에 사용할지 여부입니다.")]
        public bool requirePlayerKill;

        [Tooltip("맵 클리어가 확정되었을 때 플레이어의 자동 이동 요청과 잔여 조작 액션을 취소할지 여부입니다.")]
        public bool cancelAutoMoveOnClear = true;

        [Tooltip("맵 입장 시 살아있는 몬스터가 없는 맵은 자동 종료 정책에서 제외할지 여부입니다.")]
        public bool ignoreMapWithoutInitialMonsters = true;

        [Tooltip("모든 몬스터 사망 후 맵 종료 연출을 시작하기 전 대기 시간입니다.")]
        [Min(0f)] public float exitDelaySeconds = 0.5f;

        [Tooltip("맵 종료 Fade Out 시간입니다.")]
        [Min(0f)] public float fadeOutDurationSeconds = 0.3f;

        [Tooltip("맵 클리어 후 월드맵 UI를 자동으로 열지 여부입니다.")]
        public bool openWorldMap = true;

        [Tooltip("월드맵 UI를 연 뒤 맵 종료 Fade 화면을 즉시 정리할지 여부입니다.\n꺼두면 ScreenFadeData의 holdFinalState 설정에 따라 검정 화면이 유지될 수 있습니다.")]
        public bool clearFadeAfterWorldMapOpen = true;

        [Tooltip("맵 종료 시 사용할 화면 Fade 설정입니다.")]
        public ScreenFadeData fadeOutData = CreateDefaultFadeOutData();

        /// <summary>
        /// 기존 설정 자산에 새 필드가 추가되었을 때 안전한 기본값을 보정합니다.
        /// </summary>
        public void EnsureDefaults()
        {
            if (fadeOutData == null)
            {
                fadeOutData = CreateDefaultFadeOutData();
            }

            exitDelaySeconds = Mathf.Max(0f, exitDelaySeconds);
            fadeOutDurationSeconds = Mathf.Max(0f, fadeOutDurationSeconds);
        }

        /// <summary>
        /// 기본 맵 종료 정책 설정 객체를 생성합니다.
        /// </summary>
        /// <returns>기본값이 적용된 맵 종료 정책 설정입니다.</returns>
        public static MapClearExitPolicySettings CreateDefault()
        {
            return new MapClearExitPolicySettings
            {
                enabled = false,
                suppressMonsterRespawn = true,
                requirePlayerKill = false,
                cancelAutoMoveOnClear = true,
                ignoreMapWithoutInitialMonsters = true,
                exitDelaySeconds = 0.5f,
                fadeOutDurationSeconds = 0.3f,
                openWorldMap = true,
                clearFadeAfterWorldMapOpen = true,
                fadeOutData = CreateDefaultFadeOutData(),
            };
        }

        /// <summary>
        /// 맵 종료 연출에 사용할 기본 Fade Out 데이터를 생성합니다.
        /// </summary>
        /// <returns>투명 화면에서 검정 화면으로 전환하는 기본 Fade 데이터입니다.</returns>
        private static ScreenFadeData CreateDefaultFadeOutData()
        {
            return new ScreenFadeData
            {
                color = Color.black,
                fromAlpha = 0f,
                toAlpha = 1f,
                holdFinalState = true,
                useUnscaledTime = true,
                easing = Easing.EaseType.Linear,
                renderMode = ScreenFadeRenderMode.OverlayUi,
                sortingLayerName = nameof(ConfigSortingLayer.Keys.UI),
                orderInLayer = 0,
                planeDistance = 10f,
            };
        }
    }

    [CreateAssetMenu(fileName = ConfigScriptableObject.Main.FileName, menuName = ConfigScriptableObject.Main.MenuName, order = ConfigScriptableObject.Main.Ordering)]
    public class GGemCoSettings : ScriptableObject
    {
        [Header("기본 설정")]
        [Tooltip("디폴트 FPS")]
        public int defaultFps;
        
        [Tooltip("스파인2D 사용 여부.\n체크 시 #define GGEMCO_USE_SPINE 이 추가되며, Spine2D 패키지가 필요합니다.\n(Unity 6에서는 Build Profile에서 확인 가능)")]
        public bool useSpine2d;

        [Tooltip("입력 시스템 타입 선택 (Old Input Manager, New Input System, Both)")]
        public InputSystemType inputSystemType;

        [Tooltip("몬스터의 기본 리젠 시간 (초 단위)")]
        public float defaultMonsterRegenTimeSec;

        [Tooltip("공격 시 동시에 피해를 줄 수 있는 최대 몬스터 수")]
        public int maxEnemyValue;

        [Tooltip("몬스터가 죽은 뒤, 삭제되기까지의 지연 시간 (초 단위)")]
        public float delayDestroyMonster;

        [Tooltip("드랍된 아이템이 사라지는 시간 (초 단위). -1이면 사라지지 않습니다.")]
        public int dropItemDestroyTimeSec;

        [Header("데미지 텍스트 설정")]
        [Tooltip("데미지 텍스트 Canvas의 Render Mode")]
        public RenderMode damageTextCanvasRenderMode;
        [Tooltip("데미지 텍스트의 기본 폰트 크기")]
        public float damageTextFontSize;

        [Tooltip("데미지 텍스트 애니메이션 이징 타입")]
        public Easing.EaseType damageTextEasingType;

        [Tooltip("데미지 텍스트가 위로 이동하는 애니메이션 시간 (초 단위)")]
        public float damageTextMoveUpTime;

        [Tooltip("데미지 텍스트가 위로 이동하는 거리")]
        public float damageTextMoveUpDistance;

        [Tooltip("데미지 텍스트가 Fade Out 되는 시간 (초 단위)")]
        public float damageTextFadeOutTime;

        [Tooltip("데미지 텍스트 출력 시, X축 랜덤 좌표 범위")]
        public float damageTextRandomXRange;

        [Header("데미지 텍스트 색상")]
        [Tooltip("몬스터 피격시 색상")]
        public Color textColorDamageMonster = Color.white;
        [Tooltip("플레이어 피격시 색상")]
        public Color textColorDamagePlayer = Color.red;
        [Tooltip("플레이어 HP 증가시 색상")]
        public Color textColorHeal = Color.green;
        
        [Header("캐릭터 방향")] 
        [Tooltip("캐릭터의 방향 타입")]
        public ConfigCommon.FacingDirectionType facingDirectionType;
        
        [Header("인 게임 시간")] 
        [Tooltip("사용/미사용")]
        public bool useInGameTime;

        [Header("Debug HUD")]
        [SerializeField, DebugOption("Debug HUD 전체 사용 여부")]
        private bool enableDebugHud;
        public bool EnableDebugHud => DebugOptionRuntimeUtility.Resolve(enableDebugHud);
        

        [Tooltip("FPS HUD 사용 여부")]
        public bool enableFpsHud;

        [Tooltip("Memory HUD 사용 여부")]
        public bool enableMemoryHud;

        [Tooltip("Physics2D HUD 사용 여부")]
        public bool enablePhysics2DHud;

        [Tooltip("Tilemap DrawCall HUD 사용 여부")]
        public bool enableTilemapDrawCallHud;

        [Tooltip("HUD 폰트 크기")]
        public int debugHudFontSize;

        [Tooltip("HUD 패딩")]
        public Vector2 debugHudPadding = new Vector2(8f, 8f);

        [Tooltip("HUD 배경 색상")]
        public Color debugHudBackgroundColor = new Color(0f, 0f, 0f, 0.55f);

        [Tooltip("FPS HUD 갱신 주기(초)")]
        public float debugHudFpsUpdateInterval;

        [Tooltip("FPS HUD 샘플 히스토리 크기")]
        public int debugHudFpsHistorySize;

        [Tooltip("Memory HUD 갱신 주기(초)")]
        public float debugHudMemoryUpdateInterval;

        [Tooltip("Physics2D HUD 갱신 주기(초)")]
        public float debugHudPhysics2DUpdateInterval;

        [Tooltip("Tilemap HUD 갱신 주기(초)")]
        public float debugHudTilemapUpdateInterval;

        [Tooltip("Tilemap HUD에서 카메라 뷰 기준으로만 스캔할지 여부")]
        public bool debugHudTilemapCameraViewOnly;

        [Tooltip("Tilemap HUD에서 비활성 Tilemap을 포함할지 여부")]
        public bool debugHudTilemapIncludeInactive;

        [Tooltip("Tilemap HUD 한 축당 최대 스캔 셀 수")]
        public int debugHudTilemapCellScanBudgetPerAxis;

        [Header("자동 이동")] 
        [Tooltip("플레이어 자동 이동 시스템 사용 여부")]
        public bool enableAutoMove;

        [Tooltip("자동 이동 중 입력 잠금이 활성화되어도, 이동 입력만 막고(수동 이동 불가)\n공격/점프/대시/상호작용 입력은 허용합니다.")]
        public bool autoMoveLockMovementOnly;

        [Tooltip("맵 로드 후 화면이 다시 노출되면 자동 이동을 시작할지 여부\n- 연출/튜토리얼용 예시 옵션입니다.")]
        public bool autoMoveStartOnMapLoad;

        [Tooltip("autoMoveStartOnMapLoad가 true일 때 사용할 방향(좌/우)")]
        public AutoMoveDirection autoMoveStartDirection;

        [Tooltip("autoMoveStartOnMapLoad가 true이고 무한 이동이 아닐 때, 유지 시간(초)")]
        public float autoMoveStartDuration;
        [Tooltip("자동 이동 취소 정책")]
        public AutoMoveCancelPolicy autoMoveCancelPolicy;
        [Tooltip("전투 중 타겟을 지나쳤을 때, 타겟 방향으로 자동 복귀할지 여부입니다.")]
        public bool enableCombatTargetRecovery;
        [Tooltip("타겟 지나침 판정에 사용할 X축 오차 허용값입니다.")]
        public float combatTargetPassedEpsilon;
        [Tooltip("전투 타겟 복귀 완료로 판단할 X축 거리입니다.")]
        public float combatTargetRecoveryStopDistance;
        [Tooltip("전투 타겟 복귀 종료 후 재진입을 막는 시간입니다.")]
        public float combatTargetRecoveryCooldownSeconds;
        [Tooltip("전투 타겟 복귀가 완료된 뒤에도 Direction 자동 이동을 계속 유지할지 여부입니다. 끄면 복귀 완료 시 자동 이동을 종료합니다.")]
        public bool continueAutoMoveAfterCombatTargetRecovered;

        [Tooltip("전투 타겟을 지나쳤을 때 Direction 자동 이동의 런타임 진행 방향을 반전할지 여부입니다.")]
        public bool flipCombatAutoMoveDirectionOnTargetPassed;

        [Tooltip("플레이어 공격 범위 안에 있는 몬스터가 공중 상태이면 자동 이동 정지 대상에서 제외할지 여부입니다.")]
        public bool ignoreAirborneAttackAreaForAutoMoveSuspend;

        [Tooltip("자동 이동 중 전투가 종료되고 현재 타겟이 사라졌을 때, 현재 맵의 다음 생존 몬스터 방향으로 자동 이동을 이어갈지 여부입니다.")]
        public bool enableAutoMoveNextCombatTargetSearch;

        [Tooltip("다음 전투 타겟을 찾을 최대 거리입니다. 0 이하이면 거리 제한 없이 현재 맵 전체에서 검색합니다.")]
        public float autoMoveNextCombatTargetSearchRange;

        [Tooltip("다음 전투 타겟 검색 시 Culling 등으로 비활성화된 몬스터도 포함할지 여부입니다.")]
        public bool autoMoveNextCombatTargetIncludeInactive = true;
        
        [Header("맵 종료 정책")]
        [Tooltip("맵에 배치된 모든 몬스터가 사망했을 때 Fade Out 후 월드맵 UI를 여는 정책 설정입니다.")]
        public MapClearExitPolicySettings mapClearExitPolicy = MapClearExitPolicySettings.CreateDefault();
        
        [Header("VFX")]
        [Tooltip("VFX Fade In 시간")]
        public float vfxFadeInSec = 0.3f;
        [Tooltip("VFX Fade Out 시간")]
        public float vfxFadeOutSec = 0.3f;
        [Tooltip("VFX Fade In Easing")]
        public Easing.EaseType vfxFadeInEase = Easing.EaseType.EaseOutQuad;
        [Tooltip("VFX Fade Out Easing")]
        public Easing.EaseType vfxFadeOutEase = Easing.EaseType.EaseInQuad;
        
        /// <summary>
        /// 기존 값이 비어있을 때만 기본값을 설정
        /// </summary>
        private void OnEnable()
        {
            if (defaultMonsterRegenTimeSec <= 0) defaultMonsterRegenTimeSec = 7.0f;
            if (maxEnemyValue <= 0f) maxEnemyValue = 10;
            if (delayDestroyMonster <= 0f) delayDestroyMonster = 2;
            if (damageTextMoveUpTime <= 0f) damageTextMoveUpTime = 0.3f;
            if (damageTextFadeOutTime <= 0f) damageTextFadeOutTime = 0.1f;
            if (damageTextMoveUpDistance <= 0f) damageTextMoveUpDistance = 50.0f;
            if (damageTextRandomXRange <= 0f) damageTextRandomXRange = 10.0f;
            if (debugHudFontSize <= 0) debugHudFontSize = 12;
            if (debugHudFpsUpdateInterval <= 0f) debugHudFpsUpdateInterval = 0.25f;
            if (debugHudFpsHistorySize <= 0) debugHudFpsHistorySize = 100;
            if (debugHudMemoryUpdateInterval <= 0f) debugHudMemoryUpdateInterval = 1.0f;
            if (debugHudPhysics2DUpdateInterval <= 0f) debugHudPhysics2DUpdateInterval = 0.5f;
            if (debugHudTilemapUpdateInterval <= 0f) debugHudTilemapUpdateInterval = 1.0f;
            if (debugHudTilemapCellScanBudgetPerAxis <= 0) debugHudTilemapCellScanBudgetPerAxis = 4096;
            if (combatTargetPassedEpsilon <= 0f) combatTargetPassedEpsilon = 0.05f;
            if (combatTargetRecoveryStopDistance <= 0f) combatTargetRecoveryStopDistance = 0.35f;
            if (combatTargetRecoveryCooldownSeconds <= 0f) combatTargetRecoveryCooldownSeconds = 0.2f;
            if (mapClearExitPolicy == null) mapClearExitPolicy = MapClearExitPolicySettings.CreateDefault();
            mapClearExitPolicy.EnsureDefaults();
        }

        /// <summary>
        /// 처음 생성 시 한 번만 실행됨
        /// </summary>
        private void Reset()
        {
            defaultFps = 30;
            useSpine2d = false;
            inputSystemType = InputSystemType.None;

            defaultMonsterRegenTimeSec = 7.0f;
            maxEnemyValue = 10;
            delayDestroyMonster = 2f;
            dropItemDestroyTimeSec = 10;

            damageTextCanvasRenderMode = RenderMode.WorldSpace;
            damageTextFontSize = 20f;
            damageTextEasingType = Easing.EaseType.Linear;
            damageTextMoveUpTime = 0.3f;
            damageTextMoveUpDistance = 50.0f;
            damageTextFadeOutTime = 0.1f;
            damageTextRandomXRange = 10.0f;
            
            facingDirectionType = ConfigCommon.FacingDirectionType.TwoWay;
            
            useInGameTime = false;

            enableDebugHud = false;
            enableFpsHud = false;
            enableMemoryHud = false;
            enablePhysics2DHud = false;
            enableTilemapDrawCallHud = false;
            debugHudFontSize = 12;
            debugHudPadding = new Vector2(8f, 8f);
            debugHudBackgroundColor = new Color(0f, 0f, 0f, 0.55f);
            debugHudFpsUpdateInterval = 0.25f;
            debugHudFpsHistorySize = 100;
            debugHudMemoryUpdateInterval = 1.0f;
            debugHudPhysics2DUpdateInterval = 0.5f;
            debugHudTilemapUpdateInterval = 1.0f;
            debugHudTilemapCameraViewOnly = true;
            debugHudTilemapIncludeInactive = false;
            debugHudTilemapCellScanBudgetPerAxis = 4096;

            enableAutoMove = false;
            autoMoveLockMovementOnly = false;
            autoMoveStartOnMapLoad = false;
            autoMoveStartDirection = AutoMoveDirection.Right;
            combatTargetPassedEpsilon = 0.05f;
            combatTargetRecoveryStopDistance = 0.35f;
            combatTargetRecoveryCooldownSeconds = 0.2f;
            continueAutoMoveAfterCombatTargetRecovered = true;
            flipCombatAutoMoveDirectionOnTargetPassed = true;
            ignoreAirborneAttackAreaForAutoMoveSuspend = true;
            enableAutoMoveNextCombatTargetSearch = false;
            autoMoveNextCombatTargetSearchRange = 0f;
            autoMoveNextCombatTargetIncludeInactive = true;

            mapClearExitPolicy = MapClearExitPolicySettings.CreateDefault();
        }
    }
}
