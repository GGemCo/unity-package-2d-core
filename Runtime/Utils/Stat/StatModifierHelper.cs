using System.Collections.Generic;

namespace GGemCo2DCore
{
    /// <summary>
    /// Stat Modifier 누적 계산 Helper
    /// </summary>
    public static class StatModifierHelper
    {
        /// <summary>
        /// Stat 변경값을 누적합니다.
        /// </summary>
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