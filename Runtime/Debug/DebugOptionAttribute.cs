using System;

namespace GGemCo2DCore
{
    /// <summary>
    /// ScriptableObject 설정 클래스에서 디버그 전용 필드를 표시합니다.
    /// 릴리즈 빌드 검증기와 에디터 유틸리티는 이 특성이 붙은 bool 필드를 대상으로 동작합니다.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class DebugOptionAttribute : Attribute
    {
        /// <summary>
        /// 디버그 옵션 설명입니다.
        /// 검증 메시지와 메뉴 출력에서 함께 표시할 수 있습니다.
        /// </summary>
        public string Description { get; }

        public DebugOptionAttribute(string description = null)
        {
            Description = description;
        }
    }
}
