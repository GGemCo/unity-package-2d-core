using System;
using GGemCo2DCore;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// UI RectTransform 위치를 보간하는 Timeline Clip입니다.
    /// </summary>
    [Serializable]
    public sealed class UIEffectMoveClip : UIEffectClipBase
    {
        public override UIEffectTimelineEventType EventType => UIEffectTimelineEventType.Move;

        public Vector2 fromOffset;
        public Vector2 toOffset;
        public bool useCurrentPositionAsFrom;
        public bool relativeToInitialPosition = true;
        public bool snapToTargetOnComplete = true;
        public Easing.EaseType moveEaseType = Easing.EaseType.EaseOutCubic;
    }
}
