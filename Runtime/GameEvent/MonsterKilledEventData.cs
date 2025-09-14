using UnityEngine;

namespace GGemCo2DCore
{
    // 몬스터 사망 이벤트 페이로드
    public readonly struct MonsterKilledEventData
    {
        public readonly int MapUid;
        public readonly int MonsterUid;
        public readonly int MonsterVid;
        public readonly GameObject Monster;      // null 가능
        public readonly GameObject Attacker;      // null 가능
        public readonly double TimeRealtimeSinceStartup;
        public readonly bool IsPlayerKiller;      // 구독부에서 분기용
        public readonly int? KillerUid;           // 선택 확장 필드(없으면 null)

        public MonsterKilledEventData(
            int mapUid, int monsterUid, int monsterVid, GameObject monster, GameObject attacker,
            bool isPlayerKiller, int? killerUid)
        {
            MapUid = mapUid;
            MonsterUid = monsterUid;
            MonsterVid = monsterVid;
            Monster = monster;
            Attacker = attacker;
            IsPlayerKiller = isPlayerKiller;
            KillerUid = killerUid;
            TimeRealtimeSinceStartup = Time.realtimeSinceStartupAsDouble;
        }
    }
}