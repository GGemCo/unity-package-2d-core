using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace GGemCo2DCore
{
    /// <summary>
    /// 컷신 이벤트 클립을 배치하고 재생하는 타임라인 트랙입니다.
    /// 이 트랙은 <see cref="CutsceneEventClip"/>만 허용하며, 재생 시 전용 믹서 Behaviour를 생성합니다.
    /// </summary>
    [Serializable]
    [TrackColor(1.0f, 1.0f, 1.0f)]
    [TrackClipType(typeof(CutsceneEventClip))]
    public class CutsceneEventTrack : TrackAsset
    {
        /// <summary>
        /// 트랙에 배치된 컷신 이벤트 클립들을 처리할 믹서 플레이어블을 생성합니다.
        /// 입력 클립 수에 맞는 <see cref="CutsceneEventMixerBehaviour"/>를 생성하여 타임라인 재생에 사용합니다.
        /// </summary>
        /// <param name="graph">믹서 플레이어블이 생성될 대상 그래프입니다.</param>
        /// <param name="go">이 트랙이 바인딩된 게임 오브젝트입니다.</param>
        /// <param name="inputCount">트랙에 연결된 입력 플레이어블의 개수입니다.</param>
        /// <returns>컷신 이벤트 트랙 처리를 위한 믹서 플레이어블입니다.</returns>
        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        {
            return ScriptPlayable<CutsceneEventMixerBehaviour>.Create(graph, inputCount);
        }
    }
}