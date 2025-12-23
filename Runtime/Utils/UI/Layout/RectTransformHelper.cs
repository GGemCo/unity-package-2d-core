using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    public enum AnchorPresets
    {
        TopLeft,
        TopCenter,
        TopRight,

        MiddleLeft,
        MiddleCenter,
        MiddleRight,

        BottomLeft,
        BottomCenter,
        BottomRight,
        BottomStretch,

        VertStretchLeft,
        VertStretchCenter,
        VertStretchRight,

        HorStretchTop,
        HorStretchMiddle,
        HorStretchBottom,

        StretchAll
    }

    /// <summary>
    /// Rectransform Helper
    /// 기본 피벗까지 설정
    /// rectTransform.SetAnchor(AnchorPresets.TopLeft, 10, -10);
    /// 피벗은 건드리지 않고 anchor만 설정
    /// rectTransform.SetAnchor(AnchorPresets.TopLeft, 10, -10, setPivot: false);
    /// </summary>
    public static class RectTransformHelper
    {
        private struct AnchorInfo
        {
            public readonly Vector2 AnchorMin;
            public readonly Vector2 AnchorMax;
            public readonly Vector2 Pivot;

            public AnchorInfo(Vector2 min, Vector2 max, Vector2 pivot)
            {
                AnchorMin = min;
                AnchorMax = max;
                Pivot = pivot;
            }
        }

        private static readonly Dictionary<AnchorPresets, AnchorInfo> AnchorMap = new()
        {
            { AnchorPresets.TopLeft,          new AnchorInfo(new(0f, 1f),   new(0f, 1f),   new(0f, 1f)) },
            { AnchorPresets.TopCenter,        new AnchorInfo(new(0.5f, 1f), new(0.5f, 1f), new(0.5f, 1f)) },
            { AnchorPresets.TopRight,         new AnchorInfo(new(1f, 1f),   new(1f, 1f),   new(1f, 1f)) },

            { AnchorPresets.MiddleLeft,       new AnchorInfo(new(0f, 0.5f), new(0f, 0.5f), new(0f, 0.5f)) },
            { AnchorPresets.MiddleCenter,     new AnchorInfo(new(0.5f, 0.5f), new(0.5f, 0.5f), new(0.5f, 0.5f)) },
            { AnchorPresets.MiddleRight,      new AnchorInfo(new(1f, 0.5f), new(1f, 0.5f), new(1f, 0.5f)) },

            { AnchorPresets.BottomLeft,       new AnchorInfo(new(0f, 0f),   new(0f, 0f),   new(0f, 0f)) },
            { AnchorPresets.BottomCenter,     new AnchorInfo(new(0.5f, 0f), new(0.5f, 0f), new(0.5f, 0f)) },
            { AnchorPresets.BottomRight,      new AnchorInfo(new(1f, 0f),   new(1f, 0f),   new(1f, 0f)) },

            { AnchorPresets.HorStretchTop,    new AnchorInfo(new(0f, 1f),   new(1f, 1f),   new(0.5f, 1f)) },
            { AnchorPresets.HorStretchMiddle, new AnchorInfo(new(0f, 0.5f), new(1f, 0.5f), new(0.5f, 0.5f)) },
            { AnchorPresets.HorStretchBottom, new AnchorInfo(new(0f, 0f),   new(1f, 0f),   new(0.5f, 0f)) },

            { AnchorPresets.VertStretchLeft,  new AnchorInfo(new(0f, 0f),   new(0f, 1f),   new(0f, 0.5f)) },
            { AnchorPresets.VertStretchCenter,new AnchorInfo(new(0.5f, 0f), new(0.5f, 1f), new(0.5f, 0.5f)) },
            { AnchorPresets.VertStretchRight, new AnchorInfo(new(1f, 0f),   new(1f, 1f),   new(1f, 0.5f)) },

            { AnchorPresets.StretchAll,       new AnchorInfo(new(0f, 0f),   new(1f, 1f),   new(0.5f, 0.5f)) },
        };

        public static void SetAnchor(this RectTransform source, AnchorPresets preset, int offsetX = 0, int offsetY = 0, bool setPivot = true)
        {
            if (AnchorMap.TryGetValue(preset, out var anchorInfo))
            {
                source.anchorMin = anchorInfo.AnchorMin;
                source.anchorMax = anchorInfo.AnchorMax;
                if (setPivot)
                    source.pivot = anchorInfo.Pivot;

                source.anchoredPosition = new Vector2(offsetX, offsetY);
            }
            else
            {
                GcLogger.LogWarning($"[RectTransformHelper] Invalid AnchorPreset: {preset}");
            }
        }
        
        /// <summary>
        /// 앵커를 강제로 (Stretch, Stretch)로 맞춥니다.
        /// </summary>
        public static void EnsureStretch(this RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;   // (0,0)
            rt.anchorMax = Vector2.one;    // (1,1)
            // pivot은 보통 0.5,0.5 유지
        }

        /// <summary>Left 값을 px로 설정</summary>
        public static void SetLeft(this RectTransform rt, float left)
            => rt.offsetMin = new Vector2(left, rt.offsetMin.y);

        /// <summary>Right 값을 px로 설정</summary>
        public static void SetRight(this RectTransform rt, float right)
            => rt.offsetMax = new Vector2(-right, rt.offsetMax.y);

        /// <summary>Bottom 값을 px로 설정</summary>
        public static void SetBottom(this RectTransform rt, float bottom)
            => rt.offsetMin = new Vector2(rt.offsetMin.x, bottom);

        /// <summary>Top 값을 px로 설정</summary>
        public static void SetTop(this RectTransform rt, float top)
            => rt.offsetMax = new Vector2(rt.offsetMax.x, -top);

        /// <summary>한 번에 여백 설정 (좌, 우, 하, 상)</summary>
        public static void SetMargins(this RectTransform rt, float left, float right, float bottom, float top)
        {
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
        }

        // 읽기용
        public static float GetLeft(this RectTransform rt)   => rt.offsetMin.x;
        public static float GetBottom(this RectTransform rt) => rt.offsetMin.y;
        public static float GetRight(this RectTransform rt)  => -rt.offsetMax.x;
        public static float GetTop(this RectTransform rt)    => -rt.offsetMax.y;

        public static void SetMarginZero(GameObject gameObject)
        {
            var rt = gameObject.GetComponent<RectTransform>();
            if (!rt) return;
            SetMargins(rt, 0, 0, 0, 0);
        }
    }
}
