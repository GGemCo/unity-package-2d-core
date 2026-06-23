using UnityEngine;

namespace GGemCo2DCore
{
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
            long runtimeToken = 0)
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
        }
    }
}
