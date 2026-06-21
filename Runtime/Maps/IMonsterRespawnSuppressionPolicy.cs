using System.Collections.Generic;
using System;

namespace GGemCo2DCore
{
    /// <summary>
    /// 외부 패키지가 특정 맵의 몬스터 리스폰 억제 여부를 제공할 때 구현하는 정책입니다.
    /// </summary>
    public interface IMonsterRespawnSuppressionPolicy
    {
        /// <summary>
        /// 지정한 맵에서 일반 몬스터 리스폰을 억제해야 하는지 반환합니다.
        /// </summary>
        /// <param name="mapUid">검사할 맵 UID입니다.</param>
        /// <returns>리스폰을 억제해야 하면 <see langword="true"/>입니다.</returns>
        bool ShouldSuppress(int mapUid);
    }

    /// <summary>
    /// 외부 몬스터 리스폰 억제 정책을 관리합니다.
    /// </summary>
    public static class MonsterRespawnSuppressionPolicyRegistry
    {
        private static readonly List<IMonsterRespawnSuppressionPolicy> Policies =
            new List<IMonsterRespawnSuppressionPolicy>();

        /// <summary>
        /// 리스폰 억제 정책을 등록합니다.
        /// </summary>
        /// <param name="policy">등록할 정책입니다.</param>
        public static void Register(IMonsterRespawnSuppressionPolicy policy)
        {
            if (policy == null || Policies.Contains(policy))
            {
                return;
            }

            Policies.Add(policy);
        }

        /// <summary>
        /// 리스폰 억제 정책 등록을 해제합니다.
        /// </summary>
        /// <param name="policy">등록 해제할 정책입니다.</param>
        public static void Unregister(IMonsterRespawnSuppressionPolicy policy)
        {
            if (policy == null)
            {
                return;
            }

            Policies.Remove(policy);
        }

        /// <summary>
        /// 등록된 정책 중 하나라도 리스폰 억제를 요청하는지 확인합니다.
        /// </summary>
        /// <param name="mapUid">검사할 맵 UID입니다.</param>
        /// <returns>하나 이상의 정책이 억제를 요청하면 <see langword="true"/>입니다.</returns>
        public static bool ShouldSuppress(int mapUid)
        {
            for (int i = 0; i < Policies.Count; i++)
            {
                try
                {
                    if (Policies[i]?.ShouldSuppress(mapUid) == true)
                    {
                        return true;
                    }
                }
                catch (Exception exception)
                {
                    GcLogger.LogException(exception);
                }
            }

            return false;
        }
    }
}
