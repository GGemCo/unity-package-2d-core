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
    }
}
