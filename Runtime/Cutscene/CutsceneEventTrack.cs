using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace GGemCo.Scripts
{
    [Serializable]
    [TrackColor(1.0f, 1.0f, 1.0f)]
    [TrackClipType(typeof(CutsceneEventClip))]
    public class CutsceneEventTrack : TrackAsset
    {
        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        {
            return ScriptPlayable<CutsceneEventMixerBehaviour>.Create (graph, inputCount);
        }

        public override void GatherProperties(PlayableDirector director, IPropertyCollector driver)
        {
#if UNITY_EDITOR
#endif
            base.GatherProperties(director, driver);
        }
    }
    
}