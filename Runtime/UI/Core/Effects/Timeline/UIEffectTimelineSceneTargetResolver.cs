namespace GGemCo2DCore
{
    /// <summary>
    /// 현재 씬에 배치된 <see cref="UIEffectTimelineTargetRegistry"/>에서 targetKey를 찾는 기본 Resolver입니다.
    /// </summary>
    public sealed class UIEffectTimelineSceneTargetResolver : IUIEffectTimelineTargetResolver
    {
        /// <summary>
        /// 현재 씬의 모든 UI 효과 타겟 레지스트리에서 targetKey를 조회합니다.
        /// </summary>
        /// <param name="targetKey">조회할 대상 키입니다.</param>
        /// <param name="target">조회된 UI 효과 대상입니다.</param>
        /// <returns>대상을 찾았으면 true입니다.</returns>
        public bool TryResolve(string targetKey, out UIEffectTarget target)
        {
            target = null;
            if (string.IsNullOrWhiteSpace(targetKey))
            {
                return false;
            }

            var registries = CompatObjectFind.FindAll<UIEffectTimelineTargetRegistry>();
            foreach (var registry in registries)
            {
                if (registry != null && registry.TryResolve(targetKey, out target))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
