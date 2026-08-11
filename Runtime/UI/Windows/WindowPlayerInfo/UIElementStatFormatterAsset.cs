using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// UIElementStat의 값/투자 포인트 표현 규칙을 담당하는 포맷터 에셋입니다.
    /// 템플릿 토큰을 수정하여 프로젝트별 표시 규칙을 쉽게 커스터마이징할 수 있습니다.
    /// </summary>
    [CreateAssetMenu(
        fileName = "UIElementStatFormatter",
        menuName = "GGemCo/UI/PlayerInfo/UIElementStat Formatter")]
    public class UIElementStatFormatterAsset : ScriptableObject
    {
        [Header("Value Templates")]
        [Tooltip("preview가 없거나 변경되지 않았을 때 사용하는 템플릿. {current}는 PlayerInfo 표시 정책에 따른 값입니다.")]
        [SerializeField] private string normalValueTemplate = "{current}";

        [Tooltip("preview가 있으나 증감 방향과 무관하게 공통 표현을 사용할 때의 템플릿. {preview}는 표시 정책에 따른 미리보기 값입니다.")]
        [SerializeField] private string changedValueTemplate = "{current} → {preview}";

        [Tooltip("preview 값이 증가했을 때 사용하는 템플릿")]
        [SerializeField] private string increaseValueTemplate = "{current} → {preview} (+{deltaAbs})";

        [Tooltip("preview 값이 감소했을 때 사용하는 템플릿")]
        [SerializeField] private string decreaseValueTemplate = "{current} → {preview} (-{deltaAbs})";

        [Header("Base Templates")]
        [Tooltip("BaseText에 현재 TotalBase* 값을 표시할 때 사용하는 템플릿")]
        [SerializeField] private string normalBaseValueTemplate = "Base {currentBase}";

        [Tooltip("BaseText에 TotalBase* 미리보기 값을 표시할 때 사용하는 공통 템플릿")]
        [SerializeField] private string changedBaseValueTemplate = "Base {currentBase} → {previewBase}";

        [Tooltip("BaseText 미리보기 값이 증가했을 때 사용하는 템플릿")]
        [SerializeField] private string increaseBaseValueTemplate = "Base {currentBase} → {previewBase} (+{baseDeltaAbs})";

        [Tooltip("BaseText 미리보기 값이 감소했을 때 사용하는 템플릿")]
        [SerializeField] private string decreaseBaseValueTemplate = "Base {currentBase} → {previewBase} (-{baseDeltaAbs})";

        [Tooltip("BaseText가 있지만 표시할 TotalBase* 값이 없을 때 사용하는 텍스트")]
        [SerializeField] private string nonTargetBaseText = string.Empty;

        [Header("Invested Templates")]
        [Tooltip("투자 대상 스탯의 기본 투자 포인트 표현 템플릿")]
        [SerializeField] private string investedTemplate = "(+{draftInvested})";

        [Tooltip("투자 포인트 차이가 있을 때 사용하는 템플릿")]
        [SerializeField] private string investedChangedTemplate = "(+{draftInvested}, Δ{investedDeltaSigned})";

        [Tooltip("투자 대상이 아닌 스탯에 대한 투자 포인트 텍스트")]
        [SerializeField] private string nonTargetInvestedText = string.Empty;

        public virtual string FormatValue(in UIElementStatRenderData data)
        {
            string template = ResolveValueTemplate(data);
            return ReplaceCommonTokens(template, data);
        }

        /// <summary>
        /// BaseText에 표시할 TotalBase* 값을 포맷합니다.
        /// </summary>
        /// <param name="data">스탯 라인 렌더 데이터입니다.</param>
        /// <returns>BaseText에 표시할 TotalBase* 문자열입니다.</returns>
        public virtual string FormatBaseValue(in UIElementStatRenderData data)
        {
            if (!data.HasBaseValue)
                return nonTargetBaseText;

            string template = ResolveBaseValueTemplate(data);
            return ReplaceCommonTokens(template, data);
        }

        public virtual string FormatInvested(in UIElementStatRenderData data)
        {
            if (!data.IsStatPointTarget)
                return nonTargetInvestedText;

            string template = data.InvestedDelta != 0
                ? investedChangedTemplate
                : investedTemplate;

            return ReplaceCommonTokens(template, data);
        }

        private string ResolveValueTemplate(in UIElementStatRenderData data)
        {
            if (!data.HasPreview || !data.IsChanged)
                return normalValueTemplate;

            if (data.IsIncrease)
                return increaseValueTemplate;

            if (data.IsDecrease)
                return decreaseValueTemplate;

            return changedValueTemplate;
        }

        private string ResolveBaseValueTemplate(in UIElementStatRenderData data)
        {
            if (!data.HasBasePreview || !data.IsBaseChanged)
                return normalBaseValueTemplate;

            if (data.IsBaseIncrease)
                return increaseBaseValueTemplate;

            if (data.IsBaseDecrease)
                return decreaseBaseValueTemplate;

            return changedBaseValueTemplate;
        }

        private static string ReplaceCommonTokens(string template, in UIElementStatRenderData data)
        {
            if (string.IsNullOrEmpty(template))
                return string.Empty;

            return template
                .Replace("{label}", data.Label ?? string.Empty)
                .Replace("{current}", data.CurrentValue.ToString())
                .Replace("{preview}", data.PreviewValue.ToString())
                .Replace("{delta}", data.ValueDelta.ToString())
                .Replace("{deltaSigned}", FormatSigned(data.ValueDelta))
                .Replace("{deltaAbs}", System.Math.Abs(data.ValueDelta).ToString())
                .Replace("{draftInvested}", data.DraftInvested.ToString())
                .Replace("{investedDelta}", data.InvestedDelta.ToString())
                .Replace("{investedDeltaSigned}", FormatSigned(data.InvestedDelta))
                .Replace("{investedDeltaAbs}", System.Math.Abs(data.InvestedDelta).ToString())
                .Replace("{currentBase}", data.CurrentBaseValue.ToString())
                .Replace("{previewBase}", data.PreviewBaseValue.ToString())
                .Replace("{baseDelta}", data.BaseValueDelta.ToString())
                .Replace("{baseDeltaSigned}", FormatSigned(data.BaseValueDelta))
                .Replace("{baseDeltaAbs}", System.Math.Abs(data.BaseValueDelta).ToString());
        }

        private static string FormatSigned(long value)
        {
            return value >= 0 ? $"+{value}" : value.ToString();
        }
    }
}
