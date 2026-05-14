using System;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 화면 전체 페이드 연출 실행에 필요한 런타임 요청 데이터입니다.
    /// Cutscene, Skill 등 서로 다른 시스템이 같은 Presenter를 안전하게 공유하기 위한 공용 규격입니다.
    /// </summary>
    [Serializable]
    public struct ScreenFadeRequest
    {
        /// <summary>
        /// 이 요청을 보낸 시스템 소유자입니다.
        /// </summary>
        public ScreenFadeOwner owner;

        /// <summary>
        /// 동일 소유자 안에서 요청 출처를 구분하기 위한 객체 참조입니다.
        /// 예를 들어 SkillExecutor 인스턴스를 넣으면 해당 실행기에서 만든 페이드만 정리할 수 있습니다.
        /// </summary>
        public UnityEngine.Object source;

        /// <summary>
        /// 페이드 색상입니다.
        /// </summary>
        public Color color;

        /// <summary>
        /// 시작 알파값입니다.
        /// </summary>
        [Range(0f, 1f)] public float fromAlpha;

        /// <summary>
        /// 종료 알파값입니다.
        /// </summary>
        [Range(0f, 1f)] public float toAlpha;

        /// <summary>
        /// 페이드 지속 시간(초)입니다.
        /// </summary>
        [Min(0f)] public float durationSeconds;

        /// <summary>
        /// 완료 후 목표 알파 상태를 유지할지 여부입니다.
        /// </summary>
        public bool holdFinalState;

        /// <summary>
        /// Time.timeScale과 무관하게 진행할지 여부입니다.
        /// </summary>
        public bool useUnscaledTime;

        /// <summary>
        /// 알파 보간에 사용할 Easing 타입입니다.
        /// </summary>
        public Easing.EaseType easing;

        /// <summary>
        /// 화면 페이드 Canvas의 렌더링 모드입니다.
        /// </summary>
        public ScreenFadeRenderMode renderMode;

        /// <summary>
        /// Screen Space - Camera 또는 Overlay Canvas 정렬에 사용할 Sorting Layer 이름입니다.
        /// </summary>
        public string sortingLayerName;

        /// <summary>
        /// Canvas 정렬 순서입니다.
        /// </summary>
        public int orderInLayer;

        /// <summary>
        /// Screen Space - Camera 모드에서 사용할 Plane Distance 값입니다.
        /// </summary>
        [Min(0.01f)] public float planeDistance;

        /// <summary>
        /// 이미 재생 중인 페이드가 있을 때의 교체 정책입니다.
        /// </summary>
        public ScreenFadeReplaceMode replaceMode;

        /// <summary>
        /// 전달된 페이드 데이터를 기반으로 기본 요청을 생성합니다.
        /// </summary>
        /// <param name="data">기본값으로 사용할 화면 페이드 데이터입니다.</param>
        /// <param name="durationSeconds">페이드 지속 시간(초)입니다.</param>
        /// <param name="owner">요청 소유자입니다.</param>
        /// <param name="source">요청 출처 객체입니다.</param>
        /// <returns>공용 화면 페이드 요청 데이터입니다.</returns>
        public static ScreenFadeRequest FromData(ScreenFadeData data, float durationSeconds, ScreenFadeOwner owner, UnityEngine.Object source)
        {
            var resolved = data ?? new ScreenFadeData();
            return new ScreenFadeRequest
            {
                owner = owner,
                source = source,
                color = resolved.color,
                fromAlpha = resolved.fromAlpha,
                toAlpha = resolved.toAlpha,
                durationSeconds = Mathf.Max(0f, durationSeconds),
                holdFinalState = resolved.holdFinalState,
                useUnscaledTime = resolved.useUnscaledTime,
                easing = resolved.easing,
                renderMode = resolved.renderMode,
                sortingLayerName = resolved.sortingLayerName,
                orderInLayer = resolved.orderInLayer,
                planeDistance = Mathf.Max(0.01f, resolved.planeDistance),
                replaceMode = ScreenFadeReplaceMode.ReplaceCurrent,
            };
        }

        /// <summary>
        /// Presenter 렌더 설정 적용에 사용할 <see cref="ScreenFadeData"/> 객체로 변환합니다.
        /// </summary>
        /// <returns>렌더 설정과 보간 설정이 복사된 화면 페이드 데이터입니다.</returns>
        public ScreenFadeData ToData()
        {
            return new ScreenFadeData
            {
                color = color,
                fromAlpha = Mathf.Clamp01(fromAlpha),
                toAlpha = Mathf.Clamp01(toAlpha),
                holdFinalState = holdFinalState,
                useUnscaledTime = useUnscaledTime,
                easing = easing,
                renderMode = renderMode,
                sortingLayerName = sortingLayerName,
                orderInLayer = orderInLayer,
                planeDistance = Mathf.Max(0.01f, planeDistance),
            };
        }
    }
}
