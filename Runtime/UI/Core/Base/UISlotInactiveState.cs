using System;

namespace GGemCo2DCore
{
    /// <summary>
    /// 비활성 슬롯을 식별하는 최소 상태 값입니다.
    /// 아이콘 uid, 개수, 레벨 같은 아이콘 정보는 저장하지 않고,
    /// 어떤 윈도우의 몇 번째 슬롯인지만 보관합니다.
    /// </summary>
    [Serializable]
    public class UISlotInactiveState
    {
        /// <summary>
        /// 비활성 상태를 적용할 윈도우 uid입니다.
        /// None이면 이 값을 가진 UIWindow 인스턴스에 귀속된 설정으로 처리합니다.
        /// </summary>
        public UIWindowConstants.WindowUid windowUid = UIWindowConstants.WindowUid.None;

        /// <summary>
        /// 비활성 상태를 적용할 슬롯 인덱스입니다.
        /// </summary>
        public int slotIndex;
    }
}
