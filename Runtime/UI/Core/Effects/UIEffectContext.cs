using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// UI 효과 재생 시점의 문맥 정보를 전달합니다.
    /// </summary>
    public struct UIEffectContext
    {
        /// <summary>
        /// HUD 리소스 타입입니다.
        /// </summary>
        public UIWindowHudResourceType ResourceType;

        /// <summary>
        /// 이전 현재값입니다.
        /// </summary>
        public long PreviousCurrent;

        /// <summary>
        /// 이전 최대값입니다.
        /// </summary>
        public long PreviousTotal;

        /// <summary>
        /// 현재 현재값입니다.
        /// </summary>
        public long Current;

        /// <summary>
        /// 현재 최대값입니다.
        /// </summary>
        public long Total;

        /// <summary>
        /// 현재값 변화량입니다.
        /// </summary>
        public long DeltaCurrent;

        /// <summary>
        /// 최대값 변화량입니다.
        /// </summary>
        public long DeltaTotal;

        /// <summary>
        /// 초기값 동기화 여부입니다.
        /// </summary>
        public bool IsInitial;

        /// <summary>
        /// 현재값이 증가했는지 여부입니다.
        /// </summary>
        public bool HasCurrentIncrease => DeltaCurrent > 0;

        /// <summary>
        /// 현재값이 감소했는지 여부입니다.
        /// </summary>
        public bool HasCurrentDecrease => DeltaCurrent < 0;

        /// <summary>
        /// 최대값이 변경되었는지 여부입니다.
        /// </summary>
        public bool HasTotalChanged => DeltaTotal != 0;

        /// <summary>
        /// 표시용 정규화 비율입니다.
        /// </summary>
        public float Normalized => Total <= 0 ? 0f : Mathf.Clamp01((float)Current / Total);
    }
}
