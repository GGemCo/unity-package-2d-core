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
        
        [Header("캐릭터 방향")] 
        [Tooltip("캐릭터의 방향 타입")]
        public ConfigCommon.FacingDirectionType facingDirectionType;
        
        [Header("인 게임 시간")] 
        [Tooltip("사용/미사용")]
        public bool useInGameTime;
        
        /// <summary>
        /// 기존 값이 비어있을 때만 기본값을 설정
        /// </summary>
        private void OnEnable()
        {
            if (defaultMonsterRegenTimeSec <= 0) defaultMonsterRegenTimeSec = 7.0f;
            if (maxEnemyValue <= 0f) maxEnemyValue = 10;
            if (delayDestroyMonster <= 0f) delayDestroyMonster = 2;
            if (dropItemDestroyTimeSec <= 0f) dropItemDestroyTimeSec = 10;
            if (damageTextMoveUpTime <= 0f) damageTextMoveUpTime = 0.3f;
            if (damageTextFadeOutTime <= 0f) damageTextFadeOutTime = 0.1f;
            if (damageTextMoveUpDistance <= 0f) damageTextMoveUpDistance = 50.0f;
            if (damageTextRandomXRange <= 0f) damageTextRandomXRange = 10.0f;
        }

        /// <summary>
        /// 처음 생성 시 한 번만 실행됨
        /// </summary>
        private void Reset()
        {
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
        }
    }
}
