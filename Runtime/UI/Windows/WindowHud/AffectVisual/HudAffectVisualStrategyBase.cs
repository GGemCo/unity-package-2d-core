using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// HUD 자원 UI에 Affect 시각 상태를 적용하는 전략 베이스입니다.
    /// </summary>
    public abstract class HudAffectVisualStrategyBase : MonoBehaviour
    {
        protected UIWindowHudResourceBase Owner { get; private set; }

        public void Initialize(UIWindowHudResourceBase owner)
        {
            Owner = owner;
            OnInitialize(owner);
        }

        public abstract void Apply(HudAffectVisualProfileBase profile);
        public abstract void ResetToDefault(HudAffectVisualProfileBase defaultProfile);

        protected virtual void OnInitialize(UIWindowHudResourceBase owner)
        {
        }
    }
}
