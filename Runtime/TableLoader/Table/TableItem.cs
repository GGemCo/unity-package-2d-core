using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace GGemCo2DCore
{
    /// <summary>
    /// 아이템 테이블 Structure (강타입 DTO)
    /// </summary>
    public class StruckTableItem : IUidName
    {
        public int Uid { get; set; }
        public string Name { get; set; }
        public ItemConstants.Type Type;
        public ItemConstants.Category Category;
        public ItemConstants.SubCategory SubCategory;
        public ItemConstants.PartsType PartsID;
        public string PartsImagePath;
        public string ImageItemPath;
        public ItemConstants.Class Class;
        public string ImagePath;
        public string FileName;
        public int Upgrade;
        public int MaxOverlayCount;
        public float CoolTime;
        public CurrencyConstants.Type SaleCurrencyType;
        public int SaleCurrencyValue;
        public ItemConstants.AntiFlag[] AntiFlag;
        public string AntiFlagText;
        public string Description;

        public string StatusID1;
        public ConfigCommon.SuffixType StatusSuffix1;
        public int StatusValue1;
        public string StatusID2;
        public ConfigCommon.SuffixType StatusSuffix2;
        public int StatusValue2;

        public string OptionType1;
        public ConfigCommon.SuffixType OptionSuffix1;
        public int OptionValue1;
        public string OptionType2;
        public ConfigCommon.SuffixType OptionSuffix2;
        public int OptionValue2;
        public string OptionType3;
        public ConfigCommon.SuffixType OptionSuffix3;
        public int OptionValue3;
        public string OptionType4;
        public ConfigCommon.SuffixType OptionSuffix4;
        public int OptionValue4;
        public string OptionType5;
        public ConfigCommon.SuffixType OptionSuffix5;
        public int OptionValue5;

        // 편의 함수들 (동일)
        public bool IsTool() => Type == ItemConstants.Type.Equip && Category == ItemConstants.Category.Tool;
        public bool IsSubCategoryAxe() => IsTool() && SubCategory == ItemConstants.SubCategory.Axe;
        public bool IsSubCategoryPickAxe() => IsTool() && SubCategory == ItemConstants.SubCategory.PickAxe;
        public bool IsSubCategorySickle() => IsTool() && SubCategory == ItemConstants.SubCategory.Sickle;
        public bool IsSubCategoryHoe() => IsTool() && SubCategory == ItemConstants.SubCategory.Hoe;
        public bool IsSubCategoryWatering() => IsTool() && SubCategory == ItemConstants.SubCategory.Watering;

        public bool IsSeed() => Category == ItemConstants.Category.Seed;
        public bool IsSubCategoryHandHarvestable() => SubCategory == ItemConstants.SubCategory.HandHarvestable;
        public bool IsSubCategoryScytheHarvestable() => SubCategory == ItemConstants.SubCategory.ScytheHarvestable;
    }

    /// <summary>
    /// 아이템 테이블 (강타입 캐싱 버전)
    /// </summary>
    public sealed class TableItem : DefaultTable<StruckTableItem>
    {
        public override string Key => ConfigAddressableTable.Item;
        
        // 색인: 카테고리/서브카테고리별 목록
        private readonly Dictionary<ItemConstants.Category, List<StruckTableItem>> _byCategory
            = new Dictionary<ItemConstants.Category, List<StruckTableItem>>();
        private readonly Dictionary<ItemConstants.SubCategory, List<StruckTableItem>> _bySubCategory
            = new Dictionary<ItemConstants.SubCategory, List<StruckTableItem>>();

        // AntiFlag 문자열 별칭 매핑(기존 동작 유지: "Shop" → ShopSale)
        private static readonly Dictionary<string, ItemConstants.AntiFlag> MapAntiFlag = new()
        {
            { "Shop", ItemConstants.AntiFlag.ShopSale },
            { nameof(ItemConstants.AntiFlag.Stash), ItemConstants.AntiFlag.Stash },
            { nameof(ItemConstants.AntiFlag.Salvage), ItemConstants.AntiFlag.Salvage },
            { nameof(ItemConstants.AntiFlag.Upgrade), ItemConstants.AntiFlag.Upgrade },
        };

        // ------------ 빌더/유틸 ------------

        protected override StruckTableItem BuildRow(Dictionary<string, string> d)
        {
            // 기본 값 파싱
            int uid            = MathHelper.ParseInt(d.GetValueOrDefault("Uid"));
            int upgrade        = MathHelper.ParseInt(d.GetValueOrDefault("Upgrade"));
            int overlay        = MathHelper.ParseInt(d.GetValueOrDefault("MaxOverlayCount"));
            float coolTime     = MathHelper.ParseFloat(d.GetValueOrDefault("CoolTime"));
            int saleValue      = MathHelper.ParseInt(d.GetValueOrDefault("SaleCurrencyValue"));

            // Enum 변환(대소문자 무시)
            var type           = EnumHelper.ConvertEnum<ItemConstants.Type>(d.GetValueOrDefault("Type"));
            var category       = EnumHelper.ConvertEnum<ItemConstants.Category>(d.GetValueOrDefault("Category"));
            var parts          = EnumHelper.ConvertEnum<ItemConstants.PartsType>(d.GetValueOrDefault("PartsID"));
            var klass          = EnumHelper.ConvertEnum<ItemConstants.Class>(d.GetValueOrDefault("Class"));
            var saleCurrency   = ConvertCurrencyType(d.GetValueOrDefault("SaleCurrencyType"));

            // SubCategory는 카테고리 유효성 검사 포함
            var subCategory    = ConvertSubCategoryValidated(d, category);

            // 로컬라이즈된 이름/설명
            string name = d.GetValueOrDefault("Name");
            string desc = d.GetValueOrDefault("Description");
            if (LocalizationManager.Instance != null)
            {
                name = LocalizationManager.Instance.GetItemNameByKey(uid.ToString());
                desc = LocalizationManager.Instance.GetItemDescriptionByKey(uid.ToString());
            }
            if (upgrade > 0)
                name = $"{name} +{upgrade}";

            // 플레이스홀더 치환 (설명 내부 {컬럼명})
            desc = ParsePlaceholders(desc, d);

            // 이미지 경로
            string imagePath        = d.GetValueOrDefault("ImagePath");
            string partsImagePath   = BuildPartsImagePath(d, parts);
            string finalIconPath    = BuildItemIconPath(d, type, category, subCategory, imagePath);
            string finalItemPath    = finalIconPath.Replace("/Icon/Item/", "/Item/");
            string fileName         = imagePath; // 원래 파일명 보존

            // AntiFlag[]
            var antiFlags = ConvertAntiFlags(d.GetValueOrDefault("AntiFlag"));
            string antiFlagText = BuildAntiFlagText(antiFlags);

            // 옵션/스탯
            var row = new StruckTableItem
            {
                Uid = uid,
                Name = name,
                Type = type,
                Category = category,
                SubCategory = subCategory,
                PartsID = parts,
                PartsImagePath = partsImagePath,
                ImageItemPath = finalItemPath,
                Class = klass,
                ImagePath = finalIconPath,
                FileName = fileName,
                Upgrade = upgrade,
                MaxOverlayCount = overlay,
                CoolTime = coolTime,
                SaleCurrencyType = saleCurrency,
                SaleCurrencyValue = saleValue,
                AntiFlag = antiFlags,
                AntiFlagText = antiFlagText,
                Description = desc,

                StatusID1     = d.GetValueOrDefault("StatusID1"),
                StatusSuffix1 = ConvertSuffixType(d.GetValueOrDefault("StatusSuffix1")),
                StatusValue1  = MathHelper.ParseInt(d.GetValueOrDefault("StatusValue1")),
                StatusID2     = d.GetValueOrDefault("StatusID2"),
                StatusSuffix2 = ConvertSuffixType(d.GetValueOrDefault("StatusSuffix2")),
                StatusValue2  = MathHelper.ParseInt(d.GetValueOrDefault("StatusValue2")),

                OptionType1   = d.GetValueOrDefault("OptionType1"),
                OptionSuffix1 = ConvertSuffixType(d.GetValueOrDefault("OptionSuffix1")),
                OptionValue1  = MathHelper.ParseInt(d.GetValueOrDefault("OptionValue1")),
                OptionType2   = d.GetValueOrDefault("OptionType2"),
                OptionSuffix2 = ConvertSuffixType(d.GetValueOrDefault("OptionSuffix2")),
                OptionValue2  = MathHelper.ParseInt(d.GetValueOrDefault("OptionValue2")),
                OptionType3   = d.GetValueOrDefault("OptionType3"),
                OptionSuffix3 = ConvertSuffixType(d.GetValueOrDefault("OptionSuffix3")),
                OptionValue3  = MathHelper.ParseInt(d.GetValueOrDefault("OptionValue3")),
                OptionType4   = d.GetValueOrDefault("OptionType4"),
                OptionSuffix4 = ConvertSuffixType(d.GetValueOrDefault("OptionSuffix4")),
                OptionValue4  = MathHelper.ParseInt(d.GetValueOrDefault("OptionValue4")),
                OptionType5   = d.GetValueOrDefault("OptionType5"),
                OptionSuffix5 = ConvertSuffixType(d.GetValueOrDefault("OptionSuffix5")),
                OptionValue5  = MathHelper.ParseInt(d.GetValueOrDefault("OptionValue5")),
            };

            return row;
        }

        /// <summary>
        /// 행 생성 직후 호출되는 훅: 색인 구축
        /// </summary>
        protected override void OnLoadedData(StruckTableItem row)
        {
            if (!_byCategory.TryGetValue(row.Category, out var listC))
                _byCategory[row.Category] = listC = new List<StruckTableItem>();
            listC.Add(row);

            if (!_bySubCategory.TryGetValue(row.SubCategory, out var listS))
                _bySubCategory[row.SubCategory] = listS = new List<StruckTableItem>();
            listS.Add(row);
        }

        // ------------ 공용 조회 API ------------
        public Dictionary<ItemConstants.Category, List<StruckTableItem>> GetDictionaryByCategory() => _byCategory;
        public Dictionary<ItemConstants.SubCategory, List<StruckTableItem>> GetDictionaryBySubCategory() => _bySubCategory;

        // ------------ 내부 유틸(원 코드 로직 보존/정리) ------------

        private static string BuildPartsImagePath(Dictionary<string, string> d, ItemConstants.PartsType parts)
        {
            // PartsID가 비어있다면 "Images/Parts/<ImagePath>" 사용
            string imagePath = d.GetValueOrDefault("ImagePath");
            string pid = d.GetValueOrDefault("PartsID");
            if (string.IsNullOrEmpty(pid))
                return $"Images/Parts/{imagePath}";
            return $"Images/Parts/{pid}/{imagePath}";
        }

        private static string BuildItemIconPath(Dictionary<string, string> d,
            ItemConstants.Type type,
            ItemConstants.Category category,
            ItemConstants.SubCategory subCategory,
            string imagePath)
        {
            string typeStr = d.GetValueOrDefault("Type");
            string catStr  = d.GetValueOrDefault("Category");
            string subStr  = d.GetValueOrDefault("SubCategory");

            // 카테고리/서브카테고리 유무에 따라 경로를 다르게 생성
            if (category == ItemConstants.Category.None && subCategory == ItemConstants.SubCategory.None)
                return $"Images/Icon/Item/{typeStr}/{imagePath}";
            if (subCategory == ItemConstants.SubCategory.None)
                return $"Images/Icon/Item/{typeStr}/{catStr}/{imagePath}";
            return $"Images/Icon/Item/{typeStr}/{catStr}/{subStr}/{imagePath}";
        }

        private static ItemConstants.SubCategory ConvertSubCategoryValidated(Dictionary<string, string> d, ItemConstants.Category category)
        {
            var sub = EnumHelper.ConvertEnum<ItemConstants.SubCategory>(d.GetValueOrDefault("SubCategory"));

            if (sub == ItemConstants.SubCategory.None)
                return ItemConstants.SubCategory.None;

            if (ItemConstants.IsValidSubCategory(category, sub))
                return sub;

            GcLogger.LogError($"Category/SubCategory mismatch. Uid:{d.GetValueOrDefault("Uid")}, Category:{category}, Sub:{sub}");
            return ItemConstants.SubCategory.None;
        }

        private static ItemConstants.AntiFlag[] ConvertAntiFlags(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return Array.Empty<ItemConstants.AntiFlag>();

            string[] parts = s.Split(',');
            var arr = new ItemConstants.AntiFlag[parts.Length];
            for (int i = 0; i < parts.Length; i++)
            {
                var token = parts[i].Trim();
                // 별칭 우선 → 실패 시 Enum.TryParse 시도
                if (MapAntiFlag.TryGetValue(token, out var v))
                    arr[i] = v;
                else if (Enum.TryParse<ItemConstants.AntiFlag>(token, true, out var e))
                    arr[i] = e;
                else
                    arr[i] = ItemConstants.AntiFlag.None;
            }
            return arr;
        }

        private static string BuildAntiFlagText(ItemConstants.AntiFlag[] flags)
        {
            if (flags == null || flags.Length == 0) return string.Empty;

            string GetName(ItemConstants.AntiFlag f)
            {
                if (LocalizationManager.Instance)
                {
                    return f switch
                    {
                        ItemConstants.AntiFlag.ShopSale => LocalizationManager.Instance.GetCommonGameByKey("Item_AntiFlag_Shop"),
                        ItemConstants.AntiFlag.Stash    => LocalizationManager.Instance.GetCommonGameByKey("Item_AntiFlag_Stash"),
                        ItemConstants.AntiFlag.Salvage  => LocalizationManager.Instance.GetCommonGameByKey("Item_AntiFlag_Salvage"),
                        ItemConstants.AntiFlag.Upgrade  => LocalizationManager.Instance.GetCommonGameByKey("Item_AntiFlag_Upgrade"),
                        _ => ""
                    };
                }
                // LocalizationManager 미존재 시 영문 키 기본값
                return f switch
                {
                    ItemConstants.AntiFlag.ShopSale => "AntiFlag_Shop",
                    ItemConstants.AntiFlag.Stash    => "AntiFlag_Stash",
                    ItemConstants.AntiFlag.Salvage  => "AntiFlag_Salvage",
                    ItemConstants.AntiFlag.Upgrade  => "AntiFlag_Upgrade",
                    _ => ""
                };
            }

            var list = new List<string>(flags.Length);
            foreach (var f in flags)
            {
                var name = GetName(f);
                if (!string.IsNullOrEmpty(name))
                    list.Add(name);
            }
            return string.Join(",", list);
        }

        /// <summary>
        /// Description 문자열 내 {컬럼명} 형태의 플레이스홀더를 실제 값으로 치환합니다.
        /// </summary>
        private static string ParsePlaceholders(string template, Dictionary<string, string> values)
        {
            if (string.IsNullOrEmpty(template)) return string.Empty;
            var regex = new Regex(@"\{(.*?)\}");
            return regex.Replace(template, m =>
            {
                string key = m.Groups[1].Value;
                return values.TryGetValue(key, out var v) ? (v ?? "") : m.Value;
            });
        }
    }
}
