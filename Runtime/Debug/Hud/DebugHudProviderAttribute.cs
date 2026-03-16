using System;

namespace GGemCo2DCore
{
    /// <summary>
    /// HUD Provider 자동 등록용 어트리뷰트입니다.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class DebugHudProviderAttribute : Attribute
    {
        public DebugHudProviderAttribute(int order)
        {
            Order = order;
        }

        public int Order { get; }
    }
}
