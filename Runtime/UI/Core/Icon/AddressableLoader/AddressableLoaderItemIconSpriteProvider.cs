using System.Threading.Tasks;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// Core 아이템 계열 Addressables 아틀라스에서 UI 아이콘 Sprite를 제공하는 Provider입니다.
    /// </summary>
    public sealed class AddressableLoaderItemIconSpriteProvider : IAddressableIconSpriteProvider
    {
        /// <summary>
        /// 아이템 아이콘/드랍/장착 아틀라스 요청인지 확인합니다.
        /// </summary>
        /// <param name="request">아이콘 Sprite 요청 정보입니다.</param>
        /// <returns>처리 가능한 아이템 계열 요청이면 <see langword="true"/>입니다.</returns>
        public bool CanHandle(AddressableIconSpriteRequest request)
        {
            return request.AtlasType == AddressableIconAtlasType.ItemIcon ||
                   request.AtlasType == AddressableIconAtlasType.ItemDrop ||
                   request.AtlasType == AddressableIconAtlasType.ItemEquip;
        }

        /// <summary>
        /// AddressableLoaderItem 캐시에서 Sprite를 즉시 조회합니다.
        /// </summary>
        /// <param name="request">아이콘 Sprite 요청 정보입니다.</param>
        /// <returns>캐시에서 찾은 Sprite입니다.</returns>
        public Sprite GetCachedSprite(AddressableIconSpriteRequest request)
        {
            AddressableLoaderItem loader = AddressableLoaderItem.Instance;
            if (loader == null)
            {
                return null;
            }

            switch (request.AtlasType)
            {
                case AddressableIconAtlasType.ItemIcon:
                    return loader.GetCachedImageIconItemByName(request.SpriteName);

                case AddressableIconAtlasType.ItemDrop:
                    return loader.GetCachedImageDropByName(request.SpriteName);

                case AddressableIconAtlasType.ItemEquip:
                    return loader.GetCachedImageEquipByName(request.SpriteName);

                default:
                    return null;
            }
        }

        /// <summary>
        /// AddressableLoaderItem을 통해 필요한 아틀라스를 로드한 뒤 Sprite를 조회합니다.
        /// </summary>
        /// <param name="request">아이콘 Sprite 요청 정보입니다.</param>
        /// <returns>로드 후 찾은 Sprite입니다.</returns>
        public Task<Sprite> LoadSpriteAsync(AddressableIconSpriteRequest request)
        {
            AddressableLoaderItem loader = AddressableLoaderItem.Instance;
            if (loader == null)
            {
                return Task.FromResult<Sprite>(null);
            }

            switch (request.AtlasType)
            {
                case AddressableIconAtlasType.ItemIcon:
                    return loader.LoadImageIconItemByNameAsync(request.SpriteName);

                case AddressableIconAtlasType.ItemDrop:
                    return loader.LoadImageDropByNameAsync(request.SpriteName);

                case AddressableIconAtlasType.ItemEquip:
                    return loader.LoadImageEquipByNameAsync(request.SpriteName);

                default:
                    return Task.FromResult<Sprite>(null);
            }
        }
    }
}
