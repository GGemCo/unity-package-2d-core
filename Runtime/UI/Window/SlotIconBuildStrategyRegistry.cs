using System;
using System.Collections.Generic;

namespace GGemCo2DCore
{
    /// <summary>
    /// UIWindow 별 SlotIcon 빌드 전략 레지스트리.
    /// - 다른 패키지(Tcg, Simulation 등)에서 SlotIcon 전략을 등록할 수 있는 확장 포인트.
    /// </summary>
    public static class SlotIconBuildStrategyRegistry
    {
        // 필요하다면 UIWindow 말고 (window, iconType) 등 복합 키도 가능
        private static readonly Dictionary<UIWindowConstants.WindowUid, Func<UIWindow, ISlotIconBuildStrategy>> _factories
            = new Dictionary<UIWindowConstants.WindowUid, Func<UIWindow, ISlotIconBuildStrategy>>();

        /// <summary>
        /// 특정 WindowUid 에 대한 빌드 전략 팩토리 등록.
        /// 같은 키로 다시 등록하면 마지막 등록이 우선합니다.
        /// </summary>
        public static void Register(
            UIWindowConstants.WindowUid windowUid,
            Func<UIWindow, ISlotIconBuildStrategy> factory)
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            _factories[windowUid] = factory;
        }

        /// <summary>
        /// 등록된 전략 팩토리를 이용해서 실제 전략 인스턴스를 생성합니다.
        /// 전략이 없으면 null 반환.
        /// </summary>
        public static ISlotIconBuildStrategy Create(UIWindow window)
        {
            if (window == null)
                return null;

            if (_factories.TryGetValue(window.uid, out var factory))
            {
                return factory?.Invoke(window);
            }

            return null;
        }
    }
}