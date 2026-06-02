using System.Collections.Generic;

namespace GGemCo2DCore
{
    /// <summary>
    /// UI 아이콘 Sprite Provider를 등록하고 요청에 맞는 Provider를 찾는 Registry입니다.
    /// </summary>
    public static class AddressableIconSpriteProviderRegistry
    {
        private static readonly List<IAddressableIconSpriteProvider> Providers = new();
        private static bool _defaultProvidersRegistered;

        /// <summary>
        /// UI 아이콘 Sprite Provider를 등록합니다.
        /// 같은 인스턴스가 이미 등록되어 있으면 중복 등록하지 않습니다.
        /// </summary>
        /// <param name="provider">등록할 Provider입니다.</param>
        public static void Register(IAddressableIconSpriteProvider provider)
        {
            if (provider == null || Providers.Contains(provider))
            {
                return;
            }

            Providers.Add(provider);
        }

        /// <summary>
        /// 등록된 UI 아이콘 Sprite Provider를 해제합니다.
        /// </summary>
        /// <param name="provider">해제할 Provider입니다.</param>
        public static void Unregister(IAddressableIconSpriteProvider provider)
        {
            if (provider == null)
            {
                return;
            }

            Providers.Remove(provider);
        }

        /// <summary>
        /// 요청을 처리할 수 있는 Provider를 찾습니다.
        /// </summary>
        /// <param name="request">아이콘 Sprite 요청 정보입니다.</param>
        /// <param name="provider">찾은 Provider입니다.</param>
        /// <returns>Provider를 찾았으면 <see langword="true"/>입니다.</returns>
        public static bool TryGetProvider(
            AddressableIconSpriteRequest request,
            out IAddressableIconSpriteProvider provider)
        {
            EnsureDefaultProvidersRegistered();

            for (int i = Providers.Count - 1; i >= 0; i--)
            {
                IAddressableIconSpriteProvider candidate = Providers[i];
                if (candidate != null && candidate.CanHandle(request))
                {
                    provider = candidate;
                    return true;
                }
            }

            provider = null;
            return false;
        }

        /// <summary>
        /// Core 기본 Provider를 지연 등록합니다.
        /// 상위 패키지 Provider가 먼저 등록되어도 마지막 등록 우선 탐색으로 오버라이드할 수 있습니다.
        /// </summary>
        private static void EnsureDefaultProvidersRegistered()
        {
            if (_defaultProvidersRegistered)
            {
                return;
            }

            _defaultProvidersRegistered = true;
            Providers.Insert(0, new AddressableLoaderItemIconSpriteProvider());
        }
    }
}
