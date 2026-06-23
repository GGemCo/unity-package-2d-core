using System;
using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 맵에 남아 있는 단일 월드 드랍 아이템의 저장 정보를 보관합니다.
    /// </summary>
    [Serializable]
    public sealed class WorldItemDropSaveEntry
    {
        /// <summary>Core가 발급한 영속 드랍 식별자입니다.</summary>
        public long DropId;
        /// <summary>드랍 아이템 UID입니다.</summary>
        public int ItemUid;
        /// <summary>드랍 아이템 수량입니다.</summary>
        public long ItemCount;
        /// <summary>랜덤 옵션 등 고유 아이템 인스턴스 ID입니다.</summary>
        public long InstanceId;
        /// <summary>저장된 월드 X 좌표입니다.</summary>
        public float PositionX;
        /// <summary>저장된 월드 Y 좌표입니다.</summary>
        public float PositionY;
        /// <summary>저장된 월드 Z 좌표입니다.</summary>
        public float PositionZ;
        /// <summary>드랍을 생성한 상위 시스템의 출처 키입니다.</summary>
        public string SourceKey;
        /// <summary>상위 시스템이 사용하는 런타임 식별 토큰입니다.</summary>
        public long RuntimeToken;
        /// <summary>자동 소멸 시간을 적용하지 않을지 여부입니다.</summary>
        public bool DisableAutoDespawn;
        /// <summary>플레이어 획득 조건입니다.</summary>
        public WorldItemPickupPolicy PickupPolicy;
        /// <summary>자동 소멸까지 남은 시간이며, 음수이면 전역 설정값을 사용합니다.</summary>
        public float RemainingAutoDespawnSeconds = -1f;

        /// <summary>
        /// 저장된 개별 좌표를 Unity 월드 좌표로 변환합니다.
        /// </summary>
        /// <returns>복원할 월드 좌표입니다.</returns>
        public Vector3 GetWorldPosition()
        {
            return new Vector3(PositionX, PositionY, PositionZ);
        }

        /// <summary>
        /// Unity 월드 좌표를 저장 가능한 개별 좌표로 기록합니다.
        /// </summary>
        /// <param name="worldPosition">기록할 월드 좌표입니다.</param>
        public void SetWorldPosition(Vector3 worldPosition)
        {
            PositionX = worldPosition.x;
            PositionY = worldPosition.y;
            PositionZ = worldPosition.z;
        }
    }

    /// <summary>
    /// 맵 UID별 월드 드랍 아이템 목록과 다음 식별자를 보관합니다.
    /// </summary>
    [Serializable]
    public sealed class WorldItemDropData
    {
        /// <summary>다음 드랍에 발급할 식별자입니다.</summary>
        public long NextDropId = 1;
        /// <summary>맵 UID별 월드 드랍 저장 목록입니다.</summary>
        public Dictionary<int, List<WorldItemDropSaveEntry>> EntriesByMap = new();

        /// <summary>
        /// 중복되지 않는 다음 월드 드랍 식별자를 발급합니다.
        /// </summary>
        /// <returns>새로 발급된 양수 식별자입니다.</returns>
        public long CreateDropId()
        {
            long dropId = NextDropId > 0 ? NextDropId : 1;
            NextDropId = dropId >= long.MaxValue ? 1 : dropId + 1;
            return dropId;
        }

        /// <summary>
        /// 지정한 맵의 저장 목록을 반환하고, 없으면 새 목록을 생성합니다.
        /// </summary>
        /// <param name="mapUid">조회할 맵 UID입니다.</param>
        /// <returns>해당 맵의 수정 가능한 드랍 목록입니다.</returns>
        public List<WorldItemDropSaveEntry> GetOrCreateEntries(int mapUid)
        {
            EntriesByMap ??= new Dictionary<int, List<WorldItemDropSaveEntry>>();
            if (!EntriesByMap.TryGetValue(mapUid, out List<WorldItemDropSaveEntry> entries) ||
                entries == null)
            {
                entries = new List<WorldItemDropSaveEntry>();
                EntriesByMap[mapUid] = entries;
            }

            return entries;
        }

        /// <summary>
        /// 지정한 맵의 드랍 목록을 조회합니다.
        /// </summary>
        /// <param name="mapUid">조회할 맵 UID입니다.</param>
        /// <param name="entries">조회된 드랍 목록입니다.</param>
        /// <returns>유효한 목록이 존재하면 <see langword="true"/>입니다.</returns>
        public bool TryGetEntries(int mapUid, out List<WorldItemDropSaveEntry> entries)
        {
            entries = null;
            return EntriesByMap != null &&
                   EntriesByMap.TryGetValue(mapUid, out entries) &&
                   entries != null;
        }
    }
}
