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

    [CreateAssetMenu(fileName = ConfigScriptableObject.Main.FileName, menuName = ConfigScriptableObject.Main.MenuName, order = ConfigScriptableObject.Main.Ordering)]
    public class GGemCoSettings : ScriptableObject
    {
        [Header("기본 설정")]
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

        [Tooltip("드랍된 아이템이 사라지는 시간 (초 단위)")]
        public int dropItemDestroyTimeSec;

        [Header("디버그 HUD")]
        [Tooltip("디버그 HUD 전체 활성화 여부")]
        public bool enableDebugHud;

        [Tooltip("FPS HUD 활성화 여부")]
        public bool enableFpsHud;

        [Tooltip("메모리 HUD 활성화 여부")]
        public bool enableMemoryHud;

        [Tooltip("Physics2D HUD 활성화 여부")]
        public bool enablePhysics2DHud;

        [Tooltip("Tilemap DrawCall HUD 활성화 여부")]
        public bool enableTilemapDrawCallHud;

        [Tooltip("HUD 폰트 크기")]
        public int debugHudFontSize;

        [Tooltip("HUD 박스 X 패딩")]
        public float debugHudPaddingX;

        [Tooltip("HUD 박스 Y 패딩")]
        public float debugHudPaddingY;

        [Tooltip("HUD 배경 색상")]
        public Color debugHudBackgroundColor;

        [Tooltip("FPS HUD 갱신 주기")]
        public float debugHudFpsUpdateInterval;

        [Tooltip("FPS HUD 프레임 히스토리 수")]
        public int debugHudFpsHistorySize;

        [Tooltip("메모리 HUD 갱신 주기")]
        public float debugHudMemoryUpdateInterval;

        [Tooltip("Physics2D HUD 갱신 주기")]
        public float debugHudPhysics2DUpdateInterval;

        [Tooltip("Tilemap DrawCall HUD 갱신 주기")]
        public float debugHudTilemapUpdateInterval;

        [Tooltip("Tilemap DrawCall HUD에서 카메라 뷰 범위만 스캔할지 여부")]
        public bool debugHudTilemapCameraViewOnly;

        [Tooltip("Tilemap DrawCall HUD에서 비활성 타일맵 포함 여부")]
        public bool debugHudTilemapIncludeInactive;

        [Tooltip("Tilemap DrawCall HUD 한 축당 최대 스캔 셀 수")]
        public int debugHudTilemapCellScanBudgetPerAxis;

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

        [Header("자동 이동")]
        [Tooltip("플레이어 자동 이동 시스템 사용 여부")]
        public bool enableAutoMove;

        [Tooltip("자동 이동 중 입력 잠금이 활성화되어도, 이동 입력만 막고(수동 이동 불가)\n공격/점프/대시/상호작용 입력은 허용합니다.")]
        public bool autoMoveLockMovementOnly;

        [Tooltip("맵 로드 직후(플레이어 스폰 직후) 자동 이동을 시작할지 여부\n- 연출/튜토리얼용 예시 옵션입니다.")]
        public bool autoMoveStartOnMapLoad;

        [Tooltip("autoMoveStartOnMapLoad가 true일 때 사용할 방향(좌/우)")]
        public AutoMoveDirection autoMoveStartDirection;

        [Tooltip("autoMoveStartOnMapLoad가 true이고 무한 이동이 아닐 때, 유지 시간(초)")]
        public float autoMoveStartDuration;

        [Tooltip("자동 이동 취소 정책")]
        public AutoMoveCancelPolicy autoMoveCancelPolicy;

        private void OnEnable()
        {
            if (defaultMonsterRegenTimeSec <= 0f) defaultMonsterRegenTimeSec = 7.0f;
            if (maxEnemyValue <= 0) maxEnemyValue = 10;
            if (delayDestroyMonster <= 0f) delayDestroyMonster = 2f;
            if (dropItemDestroyTimeSec <= 0) dropItemDestroyTimeSec = 10;

            if (debugHudFontSize <= 0) debugHudFontSize = 12;
            if (debugHudPaddingX < 0f) debugHudPaddingX = 8f;
            if (debugHudPaddingY < 0f) debugHudPaddingY = 8f;
            if (debugHudFpsUpdateInterval <= 0f) debugHudFpsUpdateInterval = 0.5f;
            if (debugHudFpsHistorySize <= 0) debugHudFpsHistorySize = 100;
            if (debugHudMemoryUpdateInterval <= 0f) debugHudMemoryUpdateInterval = 0.5f;
            if (debugHudPhysics2DUpdateInterval <= 0f) debugHudPhysics2DUpdateInterval = 0.5f;
            if (debugHudTilemapUpdateInterval <= 0f) debugHudTilemapUpdateInterval = 0.5f;
            if (debugHudTilemapCellScanBudgetPerAxis <= 0) debugHudTilemapCellScanBudgetPerAxis = 4096;

            if (damageTextMoveUpTime <= 0f) damageTextMoveUpTime = 0.3f;
            if (damageTextFadeOutTime <= 0f) damageTextFadeOutTime = 0.1f;
            if (damageTextMoveUpDistance <= 0f) damageTextMoveUpDistance = 50.0f;
            if (damageTextRandomXRange <= 0f) damageTextRandomXRange = 10.0f;
        }

        private void Reset()
        {
            useSpine2d = false;
            inputSystemType = InputSystemType.None;

            defaultMonsterRegenTimeSec = 7.0f;
            maxEnemyValue = 10;
            delayDestroyMonster = 2f;
            dropItemDestroyTimeSec = 10;

            enableDebugHud = false;
            enableFpsHud = false;
            enableMemoryHud = false;
            enablePhysics2DHud = false;
            enableTilemapDrawCallHud = false;
            debugHudFontSize = 12;
            debugHudPaddingX = 8f;
            debugHudPaddingY = 8f;
            debugHudBackgroundColor = new Color(0f, 0f, 0f, 0.55f);
            debugHudFpsUpdateInterval = 0.5f;
            debugHudFpsHistorySize = 100;
            debugHudMemoryUpdateInterval = 0.5f;
            debugHudPhysics2DUpdateInterval = 0.5f;
            debugHudTilemapUpdateInterval = 0.5f;
            debugHudTilemapCameraViewOnly = true;
            debugHudTilemapIncludeInactive = false;
            debugHudTilemapCellScanBudgetPerAxis = 4096;

            damageTextCanvasRenderMode = RenderMode.WorldSpace;
            damageTextFontSize = 20f;
            damageTextEasingType = Easing.EaseType.Linear;
            damageTextMoveUpTime = 0.3f;
            damageTextMoveUpDistance = 50.0f;
            damageTextFadeOutTime = 0.1f;
            damageTextRandomXRange = 10.0f;

            facingDirectionType = ConfigCommon.FacingDirectionType.TwoWay;
            useInGameTime = false;

            enableAutoMove = false;
            autoMoveLockMovementOnly = false;
            autoMoveStartOnMapLoad = false;
            autoMoveStartDirection = AutoMoveDirection.Right;
        }
    }
}
