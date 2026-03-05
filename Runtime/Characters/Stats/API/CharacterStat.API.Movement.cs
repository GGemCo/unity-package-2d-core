namespace GGemCo2DCore
{
    /// <summary>
    /// CharacterStat - Movement 관련 공개 API/유틸리티 모음.
    /// (계산/발행 로직은 Modules 쪽에서만 담당합니다.)
    /// </summary>
    public partial class CharacterStat
    {
        /// <summary>
        /// 현재 이동속도를 반환합니다.
        /// </summary>
        /// <param name="isPercent">true이면 100 기준 퍼센트 값(예: 120 → 1.2)으로 변환합니다.</param>
        /// <returns>이동속도 값(퍼센트 변환 여부에 따라 스케일이 달라집니다).</returns>
        public float GetCurrentMoveSpeed(bool isPercent = true)
            => isPercent ? TotalMoveSpeed.Value / 100f : TotalMoveSpeed.Value;

        /// <summary>
        /// 베이스 이동속도를 변경한 뒤 스탯을 재계산합니다.
        /// </summary>
        public void SetCurrentMoveSpeed(int value)
        {
            if (value <= 0) return;
            BaseMoveSpeed = value;
            RecalculateStats();
        }
    }
}