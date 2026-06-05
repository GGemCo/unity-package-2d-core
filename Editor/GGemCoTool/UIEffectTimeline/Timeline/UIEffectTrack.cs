using GGemCo2DCore;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// Fade/Move/Scale/Shake/Flash UI 효과 Clip을 배치하는 Timeline Track입니다.
    /// </summary>
    [TrackColor(0.35f, 0.75f, 1f)]
    [TrackClipType(typeof(UIEffectFadeClip))]
    [TrackClipType(typeof(UIEffectMoveClip))]
    [TrackClipType(typeof(UIEffectScaleClip))]
    [TrackClipType(typeof(UIEffectShakeClip))]
    [TrackClipType(typeof(UIEffectFlashClip))]
    public sealed class UIEffectTrack : TrackAsset
    {
        /// <summary>
        /// UI 효과 Track의 Mixer Playable을 생성합니다.
        /// 실제 런타임 효과 실행은 Bake된 RuntimeSequence가 담당합니다.
        /// </summary>
        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        {
            return ScriptPlayable<UIEffectTimelinePlayableBehaviour>.Create(graph, inputCount);
        }
    }
}
