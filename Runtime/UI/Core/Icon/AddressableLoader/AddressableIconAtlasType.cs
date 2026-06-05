namespace GGemCo2DCore
{
    /// <summary>
    /// UI 아이콘 Sprite를 가져올 Addressables 아틀라스 종류입니다.
    /// Core는 하위 공통 리소스만 직접 알고, 상위 패키지 아이콘은 Provider 등록으로 확장합니다.
    /// </summary>
    public enum AddressableIconAtlasType
    {
        /// <summary>
        /// 아이템 아이콘 아틀라스입니다.
        /// </summary>
        ItemIcon = 0,

        /// <summary>
        /// 필드 드랍 아이템 아틀라스입니다.
        /// </summary>
        ItemDrop = 1,

        /// <summary>
        /// 장착 파츠 아이템 아틀라스입니다.
        /// </summary>
        ItemEquip = 2,

        /// <summary>
        /// Affect 버프/디버프 아이콘 아틀라스입니다.
        /// 상위 Affect 패키지가 Provider를 등록하여 처리합니다.
        /// </summary>
        AffectIcon = 3,
    }
}
