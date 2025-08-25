using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 기본 설정하기
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
        [Header("[기본 설정]")] 
        [Header("스파인2D 사용 여부. 사용할 경우 #define GGEMCO_USE_SPINE 이 추가됩니다. 스파인2D 패키지를 추가해주세요.")]
        // Unity 6 에서는 Build Profile 을 신규로 생성했을 경우, Build Profiles 메뉴에서 확인해야 한다.
        public bool useSpine2d;
        [Header("입력 시스템 설정")]
        public InputSystemType inputSystemType;

        [Header("디폴트 몬스터 리젠 시간(초)")] public float defaultMonsterRegenTimeSec;
        [Header("공격시 최대 피해 몬스터 개수")] public int maxEnemyValue;
        [Header("몬스터 죽었을때 없어지기까지 시간(초)")] public float delayDestroyMonster;
        [Header("드랍된 아이템 사라지는 시간(초)")] public int dropItemDestroyTimeSec;
        
        [Header("[데미지 텍스트]")]
        [Header("데미지 텍스트 폰트 크기")] public float damageTextFontSize;
        [Header("데미지 텍스트 애니메이션 Easing")] public Easing.EaseType damageTextEasingType;
        [Header("데미지 텍스트 애니메이션 시간(초)")] public float damageTextMoveUpTime;
        [Header("데미지 텍스트 애니메이션 거리")] public float damageTextMoveUpDistance;
        [Header("데미지 텍스트 Fade out 효과 시간(초)")] public float damageTextFadeOutTime;
        [Header("데미지 텍스트 출력시 랜덤 X 좌표 범위")] public float damageTextRandomXRange;

        // [Tooltip("이 값은 0~100 범위에서 설정할 수 있습니다.")]
        // [Range(0, 100)]
        // public int advancedLevel = 50;
        
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
            if (damageTextMoveUpDistance <= 0f) damageTextMoveUpDistance = 50.0f; // 추가된 이동 거리 설정
            if (damageTextRandomXRange <= 0f) damageTextRandomXRange = 10.0f; // X 좌표 랜덤 범위 추가
        }
        /// <summary>
        /// 처음 생성 시 한 번만 실행됨
        /// </summary>
        private void Reset()
        {
            useSpine2d = false;
            defaultMonsterRegenTimeSec = 7.0f;
            maxEnemyValue = 10;
            delayDestroyMonster = 2f;
            dropItemDestroyTimeSec = 10;
            inputSystemType = InputSystemType.None;
            
            damageTextFontSize = 20f;
            damageTextEasingType = Easing.EaseType.Linear;
            damageTextMoveUpTime = 0.3f;
            damageTextMoveUpDistance = 50.0f; // 추가된 이동 거리 설정
            damageTextFadeOutTime = 0.1f;
            damageTextRandomXRange = 10.0f; // X 좌표 랜덤 범위 추가
        }
    }
}