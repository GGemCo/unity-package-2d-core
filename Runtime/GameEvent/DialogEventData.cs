using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 대화 이벤트 페이로드
    /// </summary>
    public readonly struct DialogEventData
    {
        public readonly int NpcUid;
        public readonly int? PlayerVid;
        public readonly double TimeRealtimeSinceStartup;

        public DialogEventData(int npcUid, int? playerVid = null)
        {
            NpcUid = npcUid;
            PlayerVid = playerVid;
            TimeRealtimeSinceStartup = Time.realtimeSinceStartupAsDouble;
        }
    }
}