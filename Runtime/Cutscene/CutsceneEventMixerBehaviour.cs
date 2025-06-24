using System;
using UnityEngine.Playables;

namespace GGemCo2DCore
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