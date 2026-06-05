using System;
using GGemCo2DCore;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// UI RectTransform 스케일을 보간하는 Timeline Clip입니다.
    /// </summary>
    [Serializable]
    public sealed class UIEffectScaleClip : UIEffectClipBase
    {
        public override UIEffectTimelineEventType EventType => UIEffectTimelineEventType.Scale;

        public Vector3 fromScale = Vector3.one;
        public Vector3 toScale = Vector3.one;
        public bool useCurrentScaleAsFrom;
        public Easing.EaseType scaleEaseType = Easing.EaseType.EaseOutCubic;
    }
}
