using System.Collections;

namespace GGemCo2DCore
{
    public sealed class LocalizationLoadStep : GameLoadStepBase
    {
        private readonly LocalizationManagerBase _loc;
        private readonly string _localeCode;

        public LocalizationLoadStep(string id, int order,
            string localizedKey,
            LocalizationManagerBase localizationManager,
            string localeCode)
            : base(id, order, localizedKey)
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