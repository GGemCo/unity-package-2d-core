using System;
using GGemCo2DCore;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// UI 효과 Timeline Clip의 공통 기반 클래스입니다.
    /// </summary>
    [Serializable]
    public abstract class UIEffectClipBase : PlayableAsset, ITimelineClipAsset
    {
        /// <summary>
        /// Timeline Clip이 지원하는 기능입니다.
        /// </summary>
        public ClipCaps clipCaps => ClipCaps.None;

        /// <summary>
        /// 효과를 적용할 targetKey입니다.
        /// </summary>
        [Header("Target")]
        [Tooltip("효과를 적용할 UI 대상 키입니다. RuntimeSequence 실행 시 UIEffectTargetRegistry에서 이 키로 대상을 찾습니다.")]
        public string targetKey;

        /// <summary>
        /// 효과 간 간섭을 제어할 채널입니다.
        /// </summary>
        [Header("Playback")]
        [Tooltip("같은 UI 대상 안에서 효과 간 간섭을 분리하기 위한 채널입니다.")]
        public UIEffectChannel channel = UIEffectChannel.Default;

        /// <summary>
        /// 같은 대상/채널에 효과가 이미 재생 중일 때의 처리 정책입니다.
        /// </summary>
        [Tooltip("같은 대상과 채널에 효과가 이미 재생 중일 때 새 효과를 처리하는 방식입니다.")]
        public UIEffectPlayPolicy playPolicy = UIEffectPlayPolicy.StopSameChannelAndPlay;

        /// <summary>
        /// 효과 진행에 사용할 이징 타입입니다.
        /// </summary>
        [Tooltip("클립 전체 진행률에 적용할 기본 이징 타입입니다.")]
        public Easing.EaseType easeType = Easing.EaseType.Linear;

        /// <summary>
        /// 이 Clip이 RuntimeSequence에서 변환될 이벤트 종류입니다.
        /// </summary>
        public abstract UIEffectTimelineEventType EventType { get; }

        /// <summary>
        /// Timeline 창에서 사용할 빈 Playable을 생성합니다.
        /// 실제 재생은 Bake된 RuntimeSequence를 통해 수행됩니다.
        /// </summary>
        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            return ScriptPlayable<UIEffectTimelinePlayableBehaviour>.Create(graph);
        }
    }
}
