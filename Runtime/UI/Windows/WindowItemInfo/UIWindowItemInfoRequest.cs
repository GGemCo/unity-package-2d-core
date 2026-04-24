using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 아이템 정보창 표시 요청에 필요한 문맥 정보를 묶습니다.
    /// </summary>
    public sealed class UIWindowItemInfoRequest
    {
        /// <summary>
        /// 표시할 아이템 Uid 입니다.
        /// </summary>
        public int ItemUid { get; set; }

        /// <summary>
        /// 표시할 아이템 인스턴스 Id 입니다.
        /// </summary>
        public long InstanceId { get; set; }

        /// <summary>
        /// 정보창 위치 계산의 기준이 되는 아이콘 오브젝트입니다.
        /// </summary>
        public GameObject AnchorObject { get; set; }

        /// <summary>
        /// 정보창 위치 계산 방식입니다.
        /// </summary>
        public UIWindowItemInfo.PositionType PositionType { get; set; }

        /// <summary>
        /// 기준 아이콘 슬롯 크기입니다.
        /// </summary>
        public Vector2 IconSlotSize { get; set; }

        /// <summary>
        /// 위치 계산 시 사용할 피벗 값입니다.
        /// </summary>
        public Vector2? Pivot { get; set; }

        /// <summary>
        /// 위치 계산 시 사용할 절대 위치 값입니다.
        /// </summary>
        public Vector3? Position { get; set; }
    }

}