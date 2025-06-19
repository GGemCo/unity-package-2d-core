using System;
using UnityEngine.Playables;

namespace GGemCo.Scripts
{
    [Serializable]
    public class CutsceneEventMixerBehaviour : PlayableBehaviour
    {
        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
        }

        public override void OnPlayableDestroy(Playable playable)
        {
        }
        
    }
}