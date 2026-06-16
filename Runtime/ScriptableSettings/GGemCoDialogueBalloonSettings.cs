using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 말풍선의 프로젝트 기본 배치 정책을 정의하는 ScriptableObject 설정입니다.
    /// </summary>
    [CreateAssetMenu(
        fileName = ConfigScriptableObject.DialogueBalloon.FileName,
        menuName = ConfigScriptableObject.DialogueBalloon.MenuName,
        order = ConfigScriptableObject.DialogueBalloon.Ordering)]
    public class GGemCoDialogueBalloonSettings : ScriptableObject
    {
        /// <summary>
        /// 입력 안내 이미지 기본 간격값입니다.
        /// </summary>
        public const float DefaultEnterIndicatorGapPx = 4f;

        /// <summary>
        /// 입력 안내 이미지 기본 깜빡임 속도(Hz)입니다.
        /// </summary>
        public const float DefaultEnterIndicatorBlinkHz = 2.5f;

        /// <summary>
        /// 입력 안내 이미지 기본 최소 알파값입니다.
        /// </summary>
        public const float DefaultEnterIndicatorMinAlpha = 0.2f;

        /// <summary>
        /// 타자 효과 사운드 기본 재생 간격(초)입니다.
        /// </summary>
        public const float DefaultTypewriterSoundIntervalSeconds = 0.04f;

        /// <summary>
        /// 타자 효과 사운드 1회 재생에 필요한 기본 글자 수입니다.
        /// </summary>
        public const int DefaultTypewriterSoundCharactersPerPlay = 1;

        [Header("말풍선 월드 위치 기본값")]
        [Tooltip("말풍선 기본 위치(캐릭터 X + 높이) 기준 프로젝트 전역 오프셋입니다.")]
        public Vector3 worldOffset = Vector3.zero;

        [Tooltip("월드 오프셋 X값의 화자 방향 연동 정책입니다.")]
        public DialogueBalloonWorldOffsetXPolicy worldOffsetXPolicy = DialogueBalloonWorldOffsetXPolicy.KeepOriginal;

        [Header("입력 안내 이미지 기본값")]
        [Tooltip("프로젝트 공통 입력 안내 이미지입니다. 비어 있으면 프리팹의 기존 이미지를 사용합니다.")]
        public Sprite enterIndicatorSprite;

        [Tooltip("대사 마지막 글자와 입력 안내 이미지 사이 기본 간격(px)입니다.")]
        public float enterIndicatorGapPx = DefaultEnterIndicatorGapPx;

        [Tooltip("입력 안내 이미지 기본 깜빡임 속도(Hz)입니다.")]
        public float enterIndicatorBlinkHz = DefaultEnterIndicatorBlinkHz;

        [Range(0f, 1f)]
        [Tooltip("입력 안내 이미지 기본 최소 알파값입니다.")]
        public float enterIndicatorMinAlpha = DefaultEnterIndicatorMinAlpha;

        [Header("타자 효과 사운드 기본값")]
        [Tooltip("true이면 말풍선 타자 효과로 글자가 노출될 때 사운드를 재생합니다.")]
        public bool useTypewriterSound;

        [Tooltip("타자 효과에 사용할 sound 테이블 대표 UID입니다. 0 이하면 재생하지 않습니다.")]
        public int typewriterSoundUid;

        [Tooltip("타자 효과 사운드의 최소 재생 간격(초)입니다.")]
        public float typewriterSoundIntervalSeconds = DefaultTypewriterSoundIntervalSeconds;

        [Tooltip("타자 효과 사운드를 한 번 재생하기 위해 필요한 노출 글자 수입니다.")]
        public int typewriterSoundCharactersPerPlay = DefaultTypewriterSoundCharactersPerPlay;

        [Tooltip("true이면 공백/줄바꿈 문자는 타자 효과 사운드 재생 기준에서 제외합니다.")]
        public bool skipTypewriterSoundOnWhitespace = true;

        /// <summary>
        /// 프로젝트 기본 오프셋 X 정책을 유효 범위로 보정해 반환합니다.
        /// </summary>
        /// <returns>유효한 프로젝트 기본 오프셋 X 정책입니다.</returns>
        public DialogueBalloonWorldOffsetXPolicy GetSafeWorldOffsetXPolicy()
        {
            return DialogueBalloonWorldOffsetUtility.GetSafeWorldOffsetXPolicy(worldOffsetXPolicy);
        }

        /// <summary>
        /// 입력 안내 이미지 간격을 안전한 값으로 보정해 반환합니다.
        /// </summary>
        /// <returns>0 이상인 간격(px)입니다.</returns>
        public float GetSafeEnterIndicatorGapPx()
        {
            return enterIndicatorGapPx >= 0f
                ? enterIndicatorGapPx
                : DefaultEnterIndicatorGapPx;
        }

        /// <summary>
        /// 입력 안내 이미지 깜빡임 속도를 안전한 값으로 보정해 반환합니다.
        /// </summary>
        /// <returns>0 이상인 깜빡임 속도(Hz)입니다.</returns>
        public float GetSafeEnterIndicatorBlinkHz()
        {
            return enterIndicatorBlinkHz >= 0f
                ? enterIndicatorBlinkHz
                : DefaultEnterIndicatorBlinkHz;
        }

        /// <summary>
        /// 입력 안내 이미지 최소 알파값을 안전한 범위(0~1)로 보정해 반환합니다.
        /// </summary>
        /// <returns>0~1 범위의 최소 알파값입니다.</returns>
        public float GetSafeEnterIndicatorMinAlpha()
        {
            return Mathf.Clamp01(enterIndicatorMinAlpha);
        }

        /// <summary>
        /// 타자 효과 사운드 재생 간격을 안전한 값으로 보정해 반환합니다.
        /// </summary>
        /// <returns>0 이상인 재생 간격(초)입니다.</returns>
        public float GetSafeTypewriterSoundIntervalSeconds()
        {
            return typewriterSoundIntervalSeconds >= 0f
                ? typewriterSoundIntervalSeconds
                : DefaultTypewriterSoundIntervalSeconds;
        }

        /// <summary>
        /// 사운드 1회 재생에 필요한 타자 효과 글자 수를 안전한 값으로 보정해 반환합니다.
        /// </summary>
        /// <returns>1 이상인 글자 수입니다.</returns>
        public int GetSafeTypewriterSoundCharactersPerPlay()
        {
            return typewriterSoundCharactersPerPlay > 0
                ? typewriterSoundCharactersPerPlay
                : DefaultTypewriterSoundCharactersPerPlay;
        }

        /// <summary>
        /// 에셋 생성 시 프로젝트 기본값을 초기화합니다.
        /// </summary>
        private void Reset()
        {
            worldOffset = Vector3.zero;
            worldOffsetXPolicy = DialogueBalloonWorldOffsetXPolicy.KeepOriginal;
            enterIndicatorSprite = null;
            enterIndicatorGapPx = DefaultEnterIndicatorGapPx;
            enterIndicatorBlinkHz = DefaultEnterIndicatorBlinkHz;
            enterIndicatorMinAlpha = DefaultEnterIndicatorMinAlpha;
            useTypewriterSound = false;
            typewriterSoundUid = 0;
            typewriterSoundIntervalSeconds = DefaultTypewriterSoundIntervalSeconds;
            typewriterSoundCharactersPerPlay = DefaultTypewriterSoundCharactersPerPlay;
            skipTypewriterSoundOnWhitespace = true;
        }
    }
}
