using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace GGemCo.Scripts
{
    [Serializable]
    public class CutsceneEventClip : PlayableAsset, ITimelineClipAsset
    {
        public ClipCaps clipCaps
        {
            get { return ClipCaps.Blending; }
        }
        
        public List<CutsceneEvent> events = new List<CutsceneEvent>();

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var behaviour = new CutsceneEventBehaviour { events = events };
            return ScriptPlayable<CutsceneEventBehaviour>.Create(graph, behaviour);
        }

        public void SetEvent(CutsceneEvent e)
        {
            events.Add(e);
            // this.type = e.type;
            // this.targetName = e.targetName;
            // this.dialogue = e.dialogue;
            // this.move = e.move;
            // this.animation = e.animation;
            // this.cameraMove = e.cameraMove;
        }
    }
}