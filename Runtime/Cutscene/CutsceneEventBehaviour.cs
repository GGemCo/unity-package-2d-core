using System;
using System.Collections.Generic;
using UnityEngine.Playables;

namespace GGemCo.Scripts
{
    [Serializable]
    public class CutsceneEventBehaviour : PlayableBehaviour
    {
        public List<CutsceneEvent> events;
        private CutsceneData data;
        private bool[] fired;
        public override void OnGraphStart(Playable playable)
        {
        }
        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            // double t = playable.GetTime();
            // for (int i = 0; i < data.events.Count; i++)
            // {
            //     if (!fired[i] && data.events[i].time <= t)
            //     {
            //         fired[i] = true;
            //         GcLogger.Log(data.events[i]);
            //         // CutsceneManager.Instance.TriggerEvent(data.events[i]);
            //     }
            // }
        }
    }
}