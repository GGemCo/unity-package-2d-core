using System;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 인게임에서 사용되는 이벤트 관리
    /// </summary>
    public static class GameEventManager
    {
        // 메타데이터 기반 이벤트
        public static event Action<MonsterKilledEventData> MonsterKilledEvent;
        public static event Action<ItemCollectedEventData> ItemCollectedEvent;
        public static event Action<DialogEventData> DialogStartEvent;
        public static event Action<DialogEventData> DialogEndEvent;
        public static event Action<MapEnteredEventData> MapEnteredEvent;
        public static event Action<CharacterDiedEventData> CharacterDiedEvent;

        // 신규 API (권장)
        public static void MonsterKilled(in MonsterKilledEventData data)
            => MonsterKilledEvent?.Invoke(data);

        public static void ItemCollected(in ItemCollectedEventData data)
            => ItemCollectedEvent?.Invoke(data);

        public static void DialogStart(in DialogEventData data)
            => DialogStartEvent?.Invoke(data);

        public static void DialogEnd(in DialogEventData data)
            => DialogEndEvent?.Invoke(data);

        /// <summary>
        /// 맵 입장 완료 이벤트를 발행합니다.
        /// </summary>
        /// <param name="data">입장한 맵 정보를 담은 이벤트 데이터입니다.</param>
        public static void MapEntered(in MapEnteredEventData data)
            => MapEnteredEvent?.Invoke(data);

        /// <summary>
        /// 캐릭터 사망 상태 전환 완료 이벤트를 발행합니다.
        /// </summary>
        /// <param name="data">사망한 캐릭터와 사망 원인을 담은 이벤트 데이터입니다.</param>
        public static void CharacterDied(in CharacterDiedEventData data)
            => CharacterDiedEvent?.Invoke(data);

        // ⚠️ 주의: static 이벤트를 명시적으로 null로 지우는 것은 권장하지 않습니다.
        // - 수명주기/조립 루트에서 +=/−= 균형을 유지하세요.
        // - 굳이 초기화가 필요할 땐, 별도의 Reset()을 테스트 전용으로 둡니다.
    }
}
