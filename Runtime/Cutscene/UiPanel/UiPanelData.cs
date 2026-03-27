using System;
using UnityEngine;

namespace GGemCo2DCore
{
    [Serializable]
    public class UiPanelData
    {
        [Header("Identity")]
        [Tooltip("같은 panelId를 사용하면 이전에 만든 패널을 재사용합니다.")]
        public string panelId = "Panel";
        [Tooltip("패널이 없을 때 자동 생성할지 여부입니다.")]
        public bool createIfMissing = true;
        [Tooltip("클립 종료 시 패널을 제거할지 여부입니다.")]
        public bool destroyOnStop;
        [Tooltip("클립 종료 시 패널을 숨길지 여부입니다.")]
        public bool hideOnStop;

        [Header("Layout")]
        [Tooltip("RectTransform.anchorMin 값입니다.")]
        public Vec2 anchorMin = new Vec2(new Vector2(0.5f, 0.5f));
        [Tooltip("RectTransform.anchorMax 값입니다.")]
        public Vec2 anchorMax = new Vec2(new Vector2(0.5f, 0.5f));
        [Tooltip("RectTransform.pivot 값입니다.")]
        public Vec2 pivot = new Vec2(new Vector2(0.5f, 0.5f));
        [Tooltip("시작 anchoredPosition 값입니다.")]
        public Vec2 fromAnchoredPosition;
        [Tooltip("종료 anchoredPosition 값입니다.")]
        public Vec2 toAnchoredPosition;
        [Tooltip("시작 sizeDelta 값입니다.")]
        public Vec2 fromSizeDelta = new Vec2(new Vector2(400f, 200f));
        [Tooltip("종료 sizeDelta 값입니다.")]
        public Vec2 toSizeDelta = new Vec2(new Vector2(400f, 200f));
        [Tooltip("형제 인덱스입니다. 음수이면 변경하지 않습니다.")]
        public int siblingIndex = -1;

        [Header("Render")]
        [Tooltip("UI Panel을 어떤 Canvas 계층에 렌더링할지 결정합니다.")]
        public ScreenFadeRenderMode renderMode = ScreenFadeRenderMode.OverlayUi;
        [Tooltip("패널별로 독립 Canvas 정렬을 사용할지 여부입니다. Off면 siblingIndex만 사용합니다.")]
        public bool useIndependentCanvasSorting;
        [Tooltip("독립 정렬 사용 시 적용할 Sorting Layer 이름입니다.")]
        public string sortingLayerName = nameof(ConfigSortingLayer.Keys.UI);
        [Tooltip("독립 정렬 사용 시 적용할 Order in Layer 값입니다.")]
        public int orderInLayer = 0;
        [Tooltip("Screen Space - Camera Canvas 의 Plane Distance 값입니다.")]
        public float planeDistance = 10f;

        [Header("Visual")]
        [Tooltip("패널 시작 색상입니다.")]
        public Color fromColor = Color.black;
        [Tooltip("패널 종료 색상입니다.")]
        public Color toColor = Color.black;
        [Tooltip("패널 시작 알파값입니다.")]
        [Range(0f, 1f)] public float fromAlpha = 0f;
        [Tooltip("패널 종료 알파값입니다.")]
        [Range(0f, 1f)] public float toAlpha = 1f;
        [Tooltip("패널이 Raycast를 받을지 여부입니다.")]
        public bool raycastTarget;

        [Header("Playback")]
        [Tooltip("패널 상태 보간 easing 입니다.")]
        public Easing.EaseType easing = Easing.EaseType.Linear;
        [Tooltip("Time.timeScale과 무관하게 진행할지 여부입니다.")]
        public bool useUnscaledTime = true;
    }
}
