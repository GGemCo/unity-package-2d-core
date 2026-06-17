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

        [Header("Scale")]
        [Tooltip("스케일 시작값입니다. useCurrentScaleAsFrom이 켜져 있으면 런타임 현재 스케일을 우선 사용합니다.")]
        public Vector3 fromScale = Vector3.one;

        [Tooltip("스케일 종료값입니다.")]
        public Vector3 toScale = Vector3.one;

        [Tooltip("켜면 fromScale 대신 효과 시작 시점의 현재 localScale을 시작값으로 사용합니다.")]
        public bool useCurrentScaleAsFrom;

        [Tooltip("스케일 효과 진행률에 적용할 이징 타입입니다.")]
        public Easing.EaseType scaleEaseType = Easing.EaseType.EaseOutCubic;
    }
}
