using System.Threading.Tasks;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// UIWindow와 UIElement에서 공용으로 사용하는 Addressables 아이콘 Sprite 조회 서비스입니다.
    /// </summary>
    public static class AddressableIconSpriteService
    {
        /// <summary>
        /// 캐시에서 아이콘 Sprite를 즉시 조회합니다.
        /// </summary>
        /// <param name="request">아이콘 Sprite 요청 정보입니다.</param>
        /// <param name="sprite">조회된 Sprite입니다.</param>
        /// <returns>Sprite를 찾았으면 <see langword="true"/>입니다.</returns>
        public static bool TryGetCachedSprite(AddressableIconSpriteRequest request, out Sprite sprite)
        {
            sprite = null;
            if (!request.IsValid ||
                !AddressableIconSpriteProviderRegistry.TryGetProvider(request, out IAddressableIconSpriteProvider provider))
            {
                return false;
            }

            sprite = provider.GetCachedSprite(request);
            return sprite != null;
        }

        /// <summary>
        /// 필요한 아틀라스를 로드한 뒤 아이콘 Sprite를 조회합니다.
        /// </summary>
        /// <param name="request">아이콘 Sprite 요청 정보입니다.</param>
        /// <returns>로드 후 조회된 Sprite입니다.</returns>
        public static async Task<Sprite> LoadSpriteAsync(AddressableIconSpriteRequest request)
        {
            if (!request.IsValid ||
                !AddressableIconSpriteProviderRegistry.TryGetProvider(request, out IAddressableIconSpriteProvider provider))
            {
                return null;
            }

            return await provider.LoadSpriteAsync(request);
        }
    }
}
