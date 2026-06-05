using System;
using GGemCo2DCore;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// UIEffectTimelineEditorWindow의 Play Mode Preview에서만 사용하는 targetKey 해석기입니다.
    /// RuntimeSequence에는 Hierarchy 오브젝트 참조를 저장하지 않고, 테스트 재생 시점에만 선택 대상을 우선 적용합니다.
    /// </summary>
    internal sealed class UIEffectTimelinePreviewTargetResolver : IUIEffectTimelineTargetResolver
    {
        private readonly UIEffectTarget _overrideTarget;
        private readonly string _overrideTargetKey;
        private readonly bool _overrideAllTargets;
        private readonly IUIEffectTimelineTargetResolver _fallbackResolver;

        /// <summary>
        /// Preview targetKey 해석기를 생성합니다.
        /// </summary>
        /// <param name="overrideTarget">프리뷰에서 우선 사용할 UI 효과 대상입니다.</param>
        /// <param name="overrideTargetKey">특정 targetKey만 Override할 때 사용할 키입니다.</param>
        /// <param name="overrideAllTargets">모든 targetKey를 프리뷰 대상으로 대체할지 여부입니다.</param>
        /// <param name="fallbackResolver">Override 대상이 없거나 조건에 맞지 않을 때 사용할 기본 해석기입니다.</param>
        public UIEffectTimelinePreviewTargetResolver(
            UIEffectTarget overrideTarget,
            string overrideTargetKey,
            bool overrideAllTargets,
            IUIEffectTimelineTargetResolver fallbackResolver)
        {
            _overrideTarget = overrideTarget;
            _overrideTargetKey = overrideTargetKey;
            _overrideAllTargets = overrideAllTargets;
            _fallbackResolver = fallbackResolver;
        }

        /// <summary>
        /// 프리뷰 Override 규칙을 먼저 적용하고, 조건에 맞지 않으면 기본 Resolver로 위임합니다.
        /// </summary>
        /// <param name="targetKey">조회할 대상 키입니다.</param>
        /// <param name="target">조회된 UI 효과 대상입니다.</param>
        /// <returns>대상을 찾았으면 true입니다.</returns>
        public bool TryResolve(string targetKey, out UIEffectTarget target)
        {
            if (_overrideTarget != null && CanUseOverrideTarget(targetKey))
            {
                target = _overrideTarget;
                return true;
            }

            if (_fallbackResolver != null)
            {
                return _fallbackResolver.TryResolve(targetKey, out target);
            }

            target = null;
            return false;
        }

        /// <summary>
        /// 현재 targetKey에 Preview Override 대상을 적용할 수 있는지 검사합니다.
        /// </summary>
        /// <param name="targetKey">조회할 대상 키입니다.</param>
        /// <returns>Override 대상을 사용할 수 있으면 true입니다.</returns>
        private bool CanUseOverrideTarget(string targetKey)
        {
            if (_overrideAllTargets)
            {
                return true;
            }

            return !string.IsNullOrWhiteSpace(_overrideTargetKey)
                   && string.Equals(targetKey, _overrideTargetKey, StringComparison.Ordinal);
        }
    }
}
