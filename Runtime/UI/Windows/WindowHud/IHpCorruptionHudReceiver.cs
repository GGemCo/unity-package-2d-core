using System.Collections.Generic;

namespace GGemCo2DCore
{
    /// <summary>
    /// HUD가 속성 게이지 스냅샷 목록을 직접 수신할 때 구현하는 선택 인터페이스입니다.
    /// </summary>
    public interface IElementGaugeHudReceiver
    {
        /// <summary>
        /// 현재 속성별 게이지 스냅샷 목록을 HUD에 반영합니다.
        /// </summary>
        /// <param name="snapshots">속성별 게이지 스냅샷 목록입니다.</param>
        void SetElementGaugeSnapshots(IReadOnlyList<ElementGaugeSnapshot> snapshots);
    }
}
