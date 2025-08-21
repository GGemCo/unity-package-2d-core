using System.Collections;
using UnityEngine.Localization;

namespace GGemCo2DCore
{
    public sealed class LocalizationLoadStep : GameLoadStepBase
    {
        private readonly LocalizationManager _loc;
        private readonly string _localeCode;

        public LocalizationLoadStep(int order,
            string localizedKey,
            LocalizationManager localizationManager,
            string localeCode)
            : base("core.localization", order, localizedKey)
        {
            _loc = localizationManager;
            _localeCode = localeCode;
        }

        public override IEnumerator Run()
        {
            // 내부 루틴 동안 진행률이 없다면 0.5로 임시 표기 후 완료 시 1.0
            progress = 0.5f;
            yield return _loc.ChangeLocaleRoutine(_localeCode);
            progress = 1f;
        }
    }
}