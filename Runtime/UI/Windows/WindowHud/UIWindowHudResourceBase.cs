using UnityEngine;

namespace GGemCo2DCore
{
    public enum UIWindowHudResourceType
    {
        Hp,
        Mp,
        Stamina
    }
    public abstract class UIWindowHudResourceBase : MonoBehaviour
    {
        /// <summary>
        /// current/total이 변경될 때마다 호출됩니다.
        /// 구현체는 내부 UI(Slider, Heart 등)를 갱신합니다.
        /// </summary>
        public abstract void SetValue(UIWindowHudResourceType type, long current, long total);

        /// <summary>
        /// (선택) 최대치(total) 중, 특정 원인(예: 패시브 스킬)로 인해 추가된 보너스 값을 전달합니다.
        /// - 기본 구현은 아무 동작도 하지 않습니다.
        /// - Heart UI처럼 "기본 HP"와 "보너스 HP"를 시각적으로 구분해야 할 때 오버라이드하여 사용합니다.
        /// </summary>
        public virtual void SetBonus(UIWindowHudResourceType type, long bonus)
        {
        }

        /// <summary>
        /// (선택) 아이템 사용 등으로 얻는 "소모형 추가 최대 HP(추가 하트)" 값을 전달합니다.
        /// - 기본 구현은 아무 동작도 하지 않습니다.
        /// - Heart UI처럼 ItemBonus 영역을 별도 스프라이트/레이어로 표시해야 할 때 오버라이드하여 사용합니다.
        /// </summary>
        public virtual void SetValueTemp(UIWindowHudResourceType type, long current, long total)
        {
        }

    }
}