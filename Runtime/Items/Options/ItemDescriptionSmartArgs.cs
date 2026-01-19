namespace GGemCo2DCore
{
    /// <summary>
    /// ItemDescription 로컬라이제이션 Smart String 평가에 전달되는 인자 묶음.
    /// </summary>
    /// <remarks>
    /// Smart String 예시:
    /// - "{Name}\n{Options}"...
    /// </remarks>
    public sealed class ItemDescriptionSmartArgs
    {
        public int Uid { get; }
        public string Name { get; }
        public int Upgrade { get; }
        public float CoolTime { get; }
        public string SalePrice { get; }

        /// <summary>
        /// UI 표시에 맞춘 옵션 멀티라인 문자열.
        /// 로컬라이제이션에서 {Options}로 참조합니다.
        /// </summary>
        public string Options { get; }

        public ItemDescriptionSmartArgs(StruckTableItem item, LocalizationManager localization, string options)
        {
            if (item == null)
            {
                Uid = 0;
                Name = string.Empty;
                Upgrade = 0;
                CoolTime = 0;
                SalePrice = string.Empty;
                Options = options ?? string.Empty;
                return;
            }

            Uid = item.Uid;
            Upgrade = item.Upgrade;
            CoolTime = item.CoolTime;
            Name = localization != null
                ? localization.GetItemNameByKey(item.Uid.ToString())
                : item.Name;

            if (item.SaleCurrencyValue > 0)
                SalePrice = $"{CurrencyConstants.GetNameByCurrencyType(item.SaleCurrencyType)} {item.SaleCurrencyValue}";
            else
                SalePrice = string.Empty;

            Options = options ?? string.Empty;
        }
    }
}
