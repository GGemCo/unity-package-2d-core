using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace GGemCo2DCore
{
    /// <summary>
    /// 컷신 이벤트 목록을 타임라인 클립 형태로 제공하는 PlayableAsset입니다.
    /// 타임라인 재생 시 이벤트 정보를 포함한 <see cref="CutsceneEventBehaviour"/>를 생성합니다.
    /// </summary>
    [Serializable]
    public class CutsceneEventClip : PlayableAsset, ITimelineClipAsset
    {
        /// <summary>
        /// 이 클립이 지원하는 타임라인 기능을 반환합니다.
        /// 현재는 클립 간 블렌딩(Blending)을 지원합니다.
        /// </summary>
        public ClipCaps clipCaps
        {
            get { return ClipCaps.Blending; }
        }

        /// <summary>
        /// 이 클립에서 재생 중 처리할 컷신 이벤트 목록입니다.
        /// </summary>
        public List<CutsceneEvent> events = new List<CutsceneEvent>();

        /// <summary>
        /// 타임라인 그래프에서 사용할 플레이어블을 생성합니다.
        /// 현재 클립이 보유한 이벤트 목록을 <see cref="CutsceneEventBehaviour"/>에 전달합니다.
        /// </summary>
        /// <param name="graph">플레이어블이 생성될 대상 그래프입니다.</param>
        /// <param name="owner">이 플레이어블을 소유하는 게임 오브젝트입니다.</param>
        /// <returns>컷신 이벤트 처리를 수행하는 플레이어블 인스턴스입니다.</returns>
        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var behaviour = new CutsceneEventBehaviour { events = events };
            return ScriptPlayable<CutsceneEventBehaviour>.Create(graph, behaviour);
        }

        /// <summary>
        /// 클립에 컷신 이벤트를 추가합니다.
        /// 추가된 이벤트는 타임라인 재생 시 순차적으로 처리 대상에 포함됩니다.
        /// </summary>
        /// <param name="e">클립에 등록할 컷신 이벤트입니다.</param>
        public void SetEvent(CutsceneEvent e)
        {
            events.Add(e);
        }
    }
}