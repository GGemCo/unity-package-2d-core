using System.Threading.Tasks;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// Addressables 기반 UI 아이콘 Sprite를 제공하는 공용 Provider 인터페이스입니다.
    /// 상위 패키지는 이 인터페이스를 구현해 Core Registry에 등록할 수 있습니다.
    /// </summary>
    public interface IAddressableIconSpriteProvider
    {
        /// <summary>
        /// 지정한 아이콘 요청을 이 Provider가 처리할 수 있는지 확인합니다.
        /// </summary>
        /// <param name="request">아이콘 Sprite 요청 정보입니다.</param>
        /// <returns>처리 가능하면 <see langword="true"/>입니다.</returns>
        bool CanHandle(AddressableIconSpriteRequest request);

        /// <summary>
        /// 이미 준비된 캐시에서 Sprite를 즉시 조회합니다.
        /// 캐시가 준비되지 않았거나 Sprite를 찾지 못하면 <see langword="null"/>을 반환합니다.
        /// </summary>
        /// <param name="request">아이콘 Sprite 요청 정보입니다.</param>
        /// <returns>캐시에서 찾은 Sprite입니다.</returns>
        Sprite GetCachedSprite(AddressableIconSpriteRequest request);

        /// <summary>
        /// 필요한 아틀라스를 로드한 뒤 Sprite를 반환합니다.
        /// </summary>
        /// <param name="request">아이콘 Sprite 요청 정보입니다.</param>
        /// <returns>로드 후 찾은 Sprite입니다.</returns>
        Task<Sprite> LoadSpriteAsync(AddressableIconSpriteRequest request);
    }
}
