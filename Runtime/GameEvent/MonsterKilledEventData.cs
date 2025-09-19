using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 몬스터 사망 이벤트 페이로드
    /// </summary>
    public readonly struct MonsterKilledEventData
    {
        public readonly CharacterConstants.DieReasonType dieReasonType;
        public readonly int mapUid;
        public readonly int monsterUid;
        public readonly int monsterVid;
        public readonly GameObject monster;      // null 가능
        public readonly GameObject attacker;      // null 가능
        public readonly double timeRealtimeSinceStartup;
        public readonly bool isPlayerKiller;      // 구독부에서 분기용
        public readonly int? killerUid;           // 선택 확장 필드(없으면 null)

        public MonsterKilledEventData(
            CharacterConstants.DieReasonType dieReasonType,
            int mapUid, int monsterUid, int monsterVid, GameObject monster, GameObject attacker,
            bool isPlayerKiller, int? killerUid)
        {
            this.dieReasonType = dieReasonType;
            this.mapUid = mapUid;
            this.monsterUid = monsterUid;
            this.monsterVid = monsterVid;
            this.monster = monster;
            this.attacker = attacker;
            this.isPlayerKiller = isPlayerKiller;
            this.killerUid = killerUid;
            timeRealtimeSinceStartup = Time.realtimeSinceStartupAsDouble;
        }
    }
}