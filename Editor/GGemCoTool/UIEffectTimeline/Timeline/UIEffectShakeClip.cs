using System;
using GGemCo2DCore;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// UI RectTransform 흔들림을 적용하는 Timeline Clip입니다.
    /// </summary>
    [Serializable]
    public sealed class UIEffectShakeClip : UIEffectClipBase
    {
        public override UIEffectTimelineEventType EventType => UIEffectTimelineEventType.Shake;

        public float strength = 8f;
        public int vibrato = 14;
        public UIEffectShakeAxis axis = UIEffectShakeAxis.XY;
        public UIEffectShakeDirectionMode directionMode = UIEffectShakeDirectionMode.RandomHorizontal;
    }
}
