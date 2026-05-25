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
        }
    }
}
