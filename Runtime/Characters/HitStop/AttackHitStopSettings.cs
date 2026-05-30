using System;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 공격 명중 시 공격자와 피격 대상에게 적용할 HitStop 정책입니다.
    /// </summary>
    [Serializable]
    public struct AttackHitStopSettings
    {
        /// <summary>
        /// 공격자에게 HitStop을 적용할지 여부입니다.
        /// </summary>
        [Tooltip("공격자에게 HitStop을 적용할지 여부입니다.")]
        public bool useHitStopSelf;

        /// <summary>
        /// 공격자의 기본 HitStop 시간을 사용할지 여부입니다.
        /// </summary>
        [Tooltip("공격자의 기본 HitStop 시간을 사용할지 여부입니다.")]
        public bool useDefaultSelfHitStop;

        /// <summary>
        /// 공격자에게 직접 지정할 HitStop 시간입니다.
        /// </summary>
        [Min(0f)]
        [Tooltip("공격자에게 직접 지정할 HitStop 시간입니다.")]
        public float selfHitStopSeconds;

        /// <summary>
        /// 피격 대상에게 HitStop을 적용할지 여부입니다.
        /// </summary>
        [Tooltip("피격 대상에게 HitStop을 적용할지 여부입니다.")]
        public bool useHitStopTarget;

        /// <summary>
        /// 공격자의 기본 피격 HitStop 시간을 사용할지 여부입니다.
        /// </summary>
        [Tooltip("공격자의 기본 피격 HitStop 시간을 사용할지 여부입니다.")]
        public bool useDefaultTargetHitStop;

        /// <summary>
        /// 피격 대상에게 직접 지정할 HitStop 시간입니다.
        /// </summary>
        [Min(0f)]
        [Tooltip("피격 대상에게 직접 지정할 HitStop 시간입니다.")]
        public float targetHitStopSeconds;

        /// <summary>
        /// HitStop을 사용하지 않는 기본 설정을 반환합니다.
        /// </summary>
        public static AttackHitStopSettings Disabled => new AttackHitStopSettings
        {
            useHitStopSelf = false,
            useDefaultSelfHitStop = true,
            selfHitStopSeconds = 0.03f,
            useHitStopTarget = false,
            useDefaultTargetHitStop = true,
            targetHitStopSeconds = 0.05f
        };

        /// <summary>
        /// 공격자 또는 피격 대상 중 하나라도 HitStop 적용 대상으로 설정되어 있는지 여부입니다.
        /// </summary>
        public bool HasAnyHitStop => useHitStopSelf || useHitStopTarget;

        /// <summary>
        /// 공격자에게 적용할 최종 HitStop 시간을 계산합니다.
        /// </summary>
        /// <param name="config">공격자 기준 기본 HitStop 설정입니다.</param>
        /// <returns>공격자에게 적용할 HitStop 시간입니다.</returns>
        public float ResolveSelfSeconds(CharacterBase.HitStopConfig config)
        {
            return useDefaultSelfHitStop
                ? Mathf.Max(0f, config.DefaultSelfSeconds)
                : Mathf.Max(0f, selfHitStopSeconds);
        }

        /// <summary>
        /// 피격 대상에게 적용할 최종 HitStop 시간을 계산합니다.
        /// </summary>
        /// <param name="config">공격자 기준 기본 HitStop 설정입니다.</param>
        /// <returns>피격 대상에게 적용할 HitStop 시간입니다.</returns>
        public float ResolveTargetSeconds(CharacterBase.HitStopConfig config)
        {
            return useDefaultTargetHitStop
                ? Mathf.Max(0f, config.DefaultReceiveSeconds)
                : Mathf.Max(0f, targetHitStopSeconds);
        }
    }
}
