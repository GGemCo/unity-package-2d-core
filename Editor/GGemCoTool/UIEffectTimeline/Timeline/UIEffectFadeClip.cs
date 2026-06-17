using System;
using GGemCo2DCore;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// UI 알파 값을 보간하는 Timeline Clip입니다.
    /// </summary>
    [Serializable]
    public sealed class UIEffectFadeClip : UIEffectClipBase
    {
        public override UIEffectTimelineEventType EventType => UIEffectTimelineEventType.Fade;

        [Header("Fade")]
        [Tooltip("페이드 시작 알파 값입니다. useCurrentAlphaAsFrom이 켜져 있으면 런타임 현재 알파를 우선 사용합니다.")]
        [Range(0f, 1f)] public float fromAlpha = 0f;

        [Tooltip("페이드 종료 알파 값입니다.")]
        [Range(0f, 1f)] public float toAlpha = 1f;

        [Tooltip("켜면 fromAlpha 대신 효과 시작 시점의 현재 알파 값을 시작값으로 사용합니다.")]
        public bool useCurrentAlphaAsFrom;

        [Header("CanvasGroup State")]
        [Tooltip("효과 완료 시 toAlpha가 0보다 크면 CanvasGroup.interactable을 켜고, 0이면 끕니다.")]
        public bool updateInteractableOnComplete = true;

        [Tooltip("효과 완료 시 toAlpha가 0보다 크면 CanvasGroup.blocksRaycasts를 켜고, 0이면 끕니다.")]
        public bool updateBlocksRaycastsOnComplete = true;

        [Tooltip("켜면 최종 알파가 0일 때 입력 상호작용과 Raycast 차단을 함께 끕니다.")]
        public bool disableInputWhenInvisible = true;
    }
}
