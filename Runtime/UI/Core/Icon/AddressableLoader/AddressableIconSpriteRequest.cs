namespace GGemCo2DCore
{
    /// <summary>
    /// Addressables 기반 UI 아이콘 Sprite 요청 정보입니다.
    /// </summary>
    public readonly struct AddressableIconSpriteRequest
    {
        /// <summary>
        /// 요청할 아틀라스 종류입니다.
        /// </summary>
        public readonly AddressableIconAtlasType AtlasType;

        /// <summary>
        /// 아틀라스 내부 Sprite 이름입니다.
        /// </summary>
        public readonly string SpriteName;

        /// <summary>
        /// UI 아이콘 Sprite 요청을 생성합니다.
        /// </summary>
        /// <param name="atlasType">요청할 아틀라스 종류입니다.</param>
        /// <param name="spriteName">아틀라스 내부 Sprite 이름입니다.</param>
        public AddressableIconSpriteRequest(AddressableIconAtlasType atlasType, string spriteName)
        {
            AtlasType = atlasType;
            SpriteName = spriteName;
        }

        /// <summary>
        /// 요청에 사용할 Sprite 이름이 유효한지 확인합니다.
        /// </summary>
        public bool IsValid => !string.IsNullOrWhiteSpace(SpriteName);
    }
}
