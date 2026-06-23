using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 월드에 생성된 아이템을 플레이어가 획득할 수 있는 조건을 정의합니다.
    /// </summary>
    public enum WorldItemPickupPolicy
    {
        /// <summary>
        /// 플레이어 상태와 관계없이 기존 획득 규칙을 사용합니다.
        /// </summary>
        Default,

        /// <summary>
        /// 플레이어가 생존한 상태에서만 아이템을 획득할 수 있습니다.
        /// </summary>
        RequirePlayerAlive,
    }

    /// <summary>
    /// 월드 아이템을 화면에 배치하는 방식을 정의합니다.
    /// </summary>
    public enum WorldItemDropSpawnMode
    {
        /// <summary>
        /// 시작 위치에서 기존 포물선 드랍 애니메이션을 재생합니다.
        /// </summary>
        Animated,

        /// <summary>
        /// 저장된 최종 위치에 애니메이션 없이 즉시 배치합니다.
        /// </summary>
        RestoreAtPosition,
    }

    /// <summary>
    /// 월드에 아이템을 생성할 때 필요한 수량, 수명주기와 런타임 식별 정보를 전달합니다.
    /// </summary>
    public readonly struct WorldItemDropRequest
    {
        /// <summary>
        /// 아이템이 생성될 월드 좌표입니다.
        /// </summary>
        public readonly Vector3 WorldPosition;

        /// <summary>
        /// 생성할 아이템 UID입니다.
        /// </summary>
        public readonly int ItemUid;

        /// <summary>
        /// 생성할 아이템 수량입니다.
        /// 재화 아이템은 <see cref="System.Int64"/> 범위의 수량을 지원합니다.
        /// </summary>
        public readonly long ItemCount;

        /// <summary>
        /// 신규 인스턴스 아이템 생성 시 사용할 희귀도입니다.
        /// </summary>
        public readonly ItemConstants.Class Rarity;

        /// <summary>
        /// 신규 인스턴스 아이템 생성 시 사용할 드랍 레벨입니다.
        /// </summary>
        public readonly int DropLevel;

        /// <summary>
        /// 기존 아이템 인스턴스를 월드로 되돌릴 때 사용할 인스턴스 ID입니다.
        /// </summary>
        public readonly long ExistingInstanceId;

        /// <summary>
        /// 전역 직접 획득 설정과 관계없이 월드 아이템을 생성할지 여부입니다.
        /// </summary>
        public readonly bool ForceWorldDrop;

        /// <summary>
        /// 전역 드랍 아이템 자동 제거 시간을 이 아이템에 적용하지 않을지 여부입니다.
        /// </summary>
        public readonly bool DisableAutoDespawn;

        /// <summary>
        /// 상위 시스템이 드랍 목적을 구분하기 위해 사용하는 런타임 출처 키입니다.
        /// </summary>
        public readonly string SourceKey;

        /// <summary>
        /// 상위 시스템이 현재 유효한 드랍을 식별하기 위해 사용하는 런타임 토큰입니다.
        /// </summary>
        public readonly long RuntimeToken;

        /// <summary>
        /// 월드에 생성된 아이템의 플레이어 획득 조건입니다.
        /// </summary>
        public readonly WorldItemPickupPolicy PickupPolicy;

        /// <summary>
        /// 저장 데이터에서 복원할 기존 드랍 식별자입니다.
        /// 0 이하이면 Core가 새로운 식별자를 발급합니다.
        /// </summary>
        public readonly long ExistingDropId;

        /// <summary>
        /// 월드 아이템을 화면에 배치할 방식입니다.
        /// </summary>
        public readonly WorldItemDropSpawnMode SpawnMode;

        /// <summary>
        /// 복원할 자동 소멸 잔여 시간입니다.
        /// 0 미만이면 전역 설정값을 사용합니다.
        /// </summary>
        public readonly float RemainingAutoDespawnSeconds;

        /// <summary>
        /// 월드 아이템 드랍 요청을 생성합니다.
        /// </summary>
        /// <param name="worldPosition">아이템을 생성할 월드 좌표입니다.</param>
        /// <param name="itemUid">생성할 아이템 UID입니다.</param>
        /// <param name="itemCount">생성할 아이템 수량입니다.</param>
        /// <param name="rarity">신규 인스턴스 아이템의 희귀도입니다.</param>
        /// <param name="dropLevel">신규 인스턴스 아이템의 드랍 레벨입니다.</param>
        /// <param name="existingInstanceId">기존 아이템 인스턴스 ID입니다.</param>
        /// <param name="forceWorldDrop">직접 획득 설정을 무시하고 월드에 생성할지 여부입니다.</param>
        /// <param name="disableAutoDespawn">전역 자동 제거 시간을 적용하지 않을지 여부입니다.</param>
        /// <param name="sourceKey">상위 시스템의 런타임 출처 키입니다.</param>
        /// <param name="runtimeToken">현재 유효한 드랍을 식별하는 런타임 토큰입니다.</param>
        /// <param name="pickupPolicy">월드 아이템의 플레이어 획득 조건입니다.</param>
        /// <param name="existingDropId">저장 데이터에서 복원할 기존 드랍 식별자입니다.</param>
        /// <param name="spawnMode">월드 아이템 배치 방식입니다.</param>
        /// <param name="remainingAutoDespawnSeconds">복원할 자동 소멸 잔여 시간입니다.</param>
        public WorldItemDropRequest(
            Vector3 worldPosition,
            int itemUid,
            long itemCount,
            ItemConstants.Class rarity = ItemConstants.Class.Normal,
            int dropLevel = 0,
            long existingInstanceId = 0,
            bool forceWorldDrop = false,
            bool disableAutoDespawn = false,
            string sourceKey = null,
            long runtimeToken = 0,
            WorldItemPickupPolicy pickupPolicy = WorldItemPickupPolicy.Default,
            long existingDropId = 0,
            WorldItemDropSpawnMode spawnMode = WorldItemDropSpawnMode.Animated,
            float remainingAutoDespawnSeconds = -1f)
        {
            WorldPosition = worldPosition;
            ItemUid = itemUid;
            ItemCount = itemCount;
            Rarity = rarity;
            DropLevel = dropLevel;
            ExistingInstanceId = existingInstanceId;
            ForceWorldDrop = forceWorldDrop;
            DisableAutoDespawn = disableAutoDespawn;
            SourceKey = sourceKey;
            RuntimeToken = runtimeToken;
            PickupPolicy = pickupPolicy;
            ExistingDropId = existingDropId;
            SpawnMode = spawnMode;
            RemainingAutoDespawnSeconds = remainingAutoDespawnSeconds;
        }
    }
}
