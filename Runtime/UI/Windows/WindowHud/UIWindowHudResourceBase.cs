using UnityEngine;

namespace GGemCo2DCore
{
    public enum UIWindowHudResourceType
    {
        Hp,
        HpTemp,
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

        public abstract void SetMaxValue(UIWindowHudResourceType hpTemp, long total);
    }
}