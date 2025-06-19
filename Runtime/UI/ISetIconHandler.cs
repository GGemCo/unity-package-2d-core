namespace GGemCo.Scripts
{
    /// <summary>
    /// 윈도우 - 아이콘 관리
    /// </summary>
    public interface ISetIconHandler
    {
        /// <summary>
        /// 윈도우에 아이콘 셋팅하기
        /// </summary>
        /// <param name="window"></param>
        /// <param name="slotIndex"></param>
        /// <param name="iconUid"></param>
        /// <param name="iconCount"></param>
        /// <param name="iconLevel"></param>
        /// <param name="isLearned"></param>
        void OnSetIcon(UIWindow window, int slotIndex, int iconUid, int iconCount, int iconLevel, bool isLearned);
        /// <summary>
        /// 윈도우에서 아이콘 지우기
        /// </summary>
        /// <param name="window"></param>
        /// <param name="slotIndex"></param>
        void OnDetachIcon(UIWindow window, int slotIndex);
    }
}