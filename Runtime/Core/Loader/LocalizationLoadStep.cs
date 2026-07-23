using System.Collections;

namespace GGemCo2DCore
{
    /// <summary>
    /// 게임 로딩 과정에서 초기 Locale을 결정하고 로컬라이징 데이터를 준비하는 단계입니다.
    /// </summary>
    public sealed class LocalizationLoadStep : GameLoadStepBase
    {
        private readonly LocalizationManagerBase _loc;
        private readonly string _localeCode;
        private readonly bool _resolveStartupLocale;

        /// <summary>
        /// 저장된 사용자 언어 또는 시스템 언어를 기준으로 초기 Locale을 적용하는 로딩 단계를 생성합니다.
        /// </summary>
        /// <param name="id">로딩 단계 식별자입니다.</param>
        /// <param name="order">로딩 단계 실행 순서입니다.</param>
        /// <param name="localizedKey">로딩 화면에 표시할 Localization 키입니다.</param>
        /// <param name="localizationManager">Locale 적용을 담당할 매니저입니다.</param>
        public LocalizationLoadStep(string id, int order,
            string localizedKey,
            LocalizationManagerBase localizationManager)
            : base(id, order, localizedKey)
        {
            _loc = localizationManager;
            _localeCode = string.Empty;
            _resolveStartupLocale = true;
        }

        /// <summary>
        /// 지정한 Locale 코드를 적용하는 로딩 단계를 생성합니다.
        /// </summary>
        /// <param name="id">로딩 단계 식별자입니다.</param>
        /// <param name="order">로딩 단계 실행 순서입니다.</param>
        /// <param name="localizedKey">로딩 화면에 표시할 Localization 키입니다.</param>
        /// <param name="localizationManager">Locale 적용을 담당할 매니저입니다.</param>
        /// <param name="localeCode">명시적으로 적용할 Locale 코드입니다.</param>
        /// <remarks>기존 외부 호출부와의 하위 호환성을 위해 유지하는 생성자입니다.</remarks>
        public LocalizationLoadStep(string id, int order,
            string localizedKey,
            LocalizationManagerBase localizationManager,
            string localeCode)
            : base(id, order, localizedKey)
        {
            _loc = localizationManager;
            _localeCode = localeCode;
            _resolveStartupLocale = false;
        }

        /// <summary>
        /// 초기 Locale을 적용하고 로딩 진행률을 완료 상태로 갱신합니다.
        /// </summary>
        /// <returns>Locale 적용 완료까지 대기하는 코루틴입니다.</returns>
        public override IEnumerator Run()
        {
            // 내부 루틴 동안 진행률이 없다면 0.5로 임시 표기 후 완료 시 1.0
            progress = 0.5f;

            if (_loc == null)
            {
                GcLogger.LogError("LocalizationLoadStep에 LocalizationManager가 연결되지 않았습니다.");
                progress = 1f;
                yield break;
            }

            if (_resolveStartupLocale)
            {
                yield return _loc.InitializeLocaleRoutine();
            }
            else
            {
                yield return _loc.ChangeLocaleRoutine(_localeCode);
            }

            progress = 1f;
        }
    }
}
