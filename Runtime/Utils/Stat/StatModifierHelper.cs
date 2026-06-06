using System.Collections.Generic;

namespace GGemCo2DCore
{
    /// <summary>
    /// Stat Modifier 누적 계산을 담당하는 Helper입니다.
    /// </summary>
    public static class StatModifierHelper
    {
        /// <summary>
        /// stat 테이블 ID를 기준으로 Stat 변경값을 누적합니다.
        /// </summary>
        /// <param name="flat">고정값 modifier 버킷입니다.</param>
        /// <param name="percent">비율값 modifier 버킷입니다.</param>
        /// <param name="targetId">stat 테이블 ID입니다. 예) BASE_ATK, STAT_ATK</param>
        /// <param name="op">적용할 연산 종류입니다.</param>
        /// <param name="value">누적할 수치입니다.</param>
        /// <remarks>
        /// TargetId는 호출 측에서 도메인에 맞게 검증한 뒤 전달하는 것을 전제로 합니다.
        /// BASE_*는 TotalBase* 계산에, STAT_*는 TotalStat* 계산에 반영됩니다.
        /// </remarks>
        public static void AccumulateStat(
            Dictionary<string, int> flat,
            Dictionary<string, float> percent,
            string targetId,
            ConfigCommon.SuffixType op,
            float value)
        {
            if (string.IsNullOrEmpty(targetId))
                return;

            switch (op)
            {
                case ConfigCommon.SuffixType.Plus:
                case ConfigCommon.SuffixType.None:
                    flat[targetId] = flat.GetValueOrDefault(targetId, 0) + (int)value;
                    break;

                case ConfigCommon.SuffixType.Minus:
                    flat[targetId] = flat.GetValueOrDefault(targetId, 0) - (int)value;
                    break;

                case ConfigCommon.SuffixType.Increase:
                    percent[targetId] = percent.GetValueOrDefault(targetId, 0f) + value;
                    break;

                case ConfigCommon.SuffixType.Decrease:
                    percent[targetId] = percent.GetValueOrDefault(targetId, 0f) - value;
                    break;
            }
        }

        /// <summary>
        /// Affect UID를 결과에 추가합니다.
        /// </summary>
        public static void AccumulateAffect(List<int> affectUids, string targetId)
        {
            if (string.IsNullOrEmpty(targetId))
                return;

            if (int.TryParse(targetId, out int affectUid) == false)
                return;

            if (affectUid <= 0)
                return;

            affectUids.Add(affectUid);
        }
    }
}