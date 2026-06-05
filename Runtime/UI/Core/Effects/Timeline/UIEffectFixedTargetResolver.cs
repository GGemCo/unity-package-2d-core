namespace GGemCo2DCore
{
    /// <summary>
    /// 동적으로 생성된 단일 <see cref="UIEffectTarget"/>을 모든 targetKey에 대해 고정 반환하는 Resolver입니다.
    /// </summary>
    public sealed class UIEffectFixedTargetResolver : IUIEffectTimelineTargetResolver
    {
        private readonly UIEffectTarget _target;

        /// <summary>
        /// 고정 대상 Resolver를 생성합니다.
        /// </summary>
        /// <param name="target">UI 효과를 적용할 런타임 대상입니다.</param>
        public UIEffectFixedTargetResolver(UIEffectTarget target)
        {
            _target = target;
        }

        /// <summary>
        /// targetKey 값과 무관하게 생성 시 전달된 대상을 반환합니다.
        /// </summary>
        /// <param name="targetKey">시퀀스 Payload에 기록된 대상 키입니다.</param>
        /// <param name="target">해석된 고정 대상입니다.</param>
        /// <returns>고정 대상이 유효하면 <see langword="true"/>입니다.</returns>
        public bool TryResolve(string targetKey, out UIEffectTarget target)
        {
            target = _target;
            return target != null;
        }
    }
}
