using System;
using GGemCo2DCore;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// UI Graphic 색상을 플래시하는 Timeline Clip입니다.
    /// </summary>
    [Serializable]
    public sealed class UIEffectFlashClip : UIEffectClipBase
    {
        public override UIEffectTimelineEventType EventType => UIEffectTimelineEventType.Flash;

        public Color flashColor = Color.white;
        [Range(0f, 1f)] public float peakAlpha = 0.8f;
        public int repeatCount = 1;
        public bool restoreOriginalColorOnComplete = true;
        public Easing.EaseType flashEaseType = Easing.EaseType.EaseOutCubic;
    }
}
