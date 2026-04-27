using System.Collections.Generic;

namespace GGemCo2DCore
{
    /// <summary>
    /// UIWindow별로 저장된 활성 슬롯 목록입니다.
    /// 비활성 슬롯 설정은 각 UIWindow의 Inspector 값을 기준으로 두고, 구매 등으로 열린 슬롯만 이 데이터에 저장합니다.
    /// </summary>
    public sealed class WindowSlotActivationSaveData
    {
        /// <summary>
        /// 저장 봉투에 기록할 Core UI 슬롯 활성화 섹션 키입니다.
        /// </summary>
        public const string SectionKey = "GGemCo.UI.WindowSlotActivation";

        /// <summary>
        /// UIWindow uid별 활성화된 슬롯 인덱스 목록입니다.
        /// </summary>
        public Dictionary<int, List<int>> ActiveSlotsByWindow = new();
    }
}
