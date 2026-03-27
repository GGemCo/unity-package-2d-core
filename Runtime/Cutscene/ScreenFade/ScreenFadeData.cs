using System;
using UnityEngine;

namespace GGemCo2DCore
{
    [Serializable]
    public class ScreenFadeData
    {
        [Header("Fade")]
        [Tooltip("페이드 색상입니다.")]
        public Color color = Color.black;
        [Tooltip("시작 알파값입니다.")]
        [Range(0f, 1f)] public float fromAlpha = 0f;
        [Tooltip("종료 알파값입니다.")]
        [Range(0f, 1f)] public float toAlpha = 1f;
        [Tooltip("클립 종료 후 마지막 알파 상태를 유지할지 여부입니다.")]
        public bool holdFinalState = true;
        [Tooltip("Time.timeScale과 무관하게 진행할지 여부입니다.")]
        public bool useUnscaledTime = true;
        [Tooltip("알파 보간 easing 입니다.")]
        public Easing.EaseType easing = Easing.EaseType.Linear;

        [Header("Render")]
        [Tooltip("Screen Fade를 어떤 Canvas 계층에 렌더링할지 결정합니다.")]
        public ScreenFadeRenderMode renderMode = ScreenFadeRenderMode.OverlayUi;
        [Tooltip("Screen Space - Camera 사용 시 적용할 Sorting Layer 이름입니다.")]
        public string sortingLayerName = nameof(ConfigSortingLayer.Keys.UI);
        [Tooltip("Screen Space - Camera 사용 시 적용할 Order in Layer 값입니다.")]
        public int orderInLayer = 0;
        [Tooltip("Screen Space - Camera Canvas 의 Plane Distance 값입니다.")]
        public float planeDistance = 10f;
    }
}
