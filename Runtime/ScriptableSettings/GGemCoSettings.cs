using UnityEngine;

namespace GGemCo.Scripts
{
    [CreateAssetMenu(fileName = "GGemCoSettings", menuName = "GGemCo/Settings/GGemCoSettings", order = 1)]
    public class GGemCoSettings : ScriptableObject
    {
        [Header("기본 설정")] 
        [Header("스파인2D 사용 여부. 사용할 경우 #define GGEMCO_USE_SPINE 이 추가됩니다. 스파인2D 패키지를 추가해주세요.")]
        public bool useSpine2d;

        [Header("디폴트 몬스터 리젠 시간(초)")] public float defaultMonsterRegenTimeSec;
        [Header("공격시 최대 피해 몬스터 개수")] public int maxEnemyValue;
        [Header("몬스터 죽었을때 없어지기까지 시간(초)")] public float delayDestroyMonster;
        [Header("드랍된 아이템 사라지는 시간(초)")] public int dropItemDestroyTimeSec;
        
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
        }
    }
}