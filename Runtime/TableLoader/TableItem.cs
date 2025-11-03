using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace GGemCo2DCore
{
    /// <summary>
    /// 아이템 테이블 Structure
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
        
        public bool IsTool()
        {
            return Type == ItemConstants.Type.Equip && Category == ItemConstants.Category.Tool;
        }

        public bool IsSubCategoryAxe()
        {
            return IsTool() && SubCategory == ItemConstants.SubCategory.Axe;
        }
        public bool IsSubCategoryPickAxe()
        {
            return IsTool() && SubCategory == ItemConstants.SubCategory.PickAxe;
        }
        public bool IsSubCategorySickle()
        {
            return IsTool() && SubCategory == ItemConstants.SubCategory.Sickle;
        }
        public bool IsSubCategoryHoe()
        {
            return IsTool() && SubCategory == ItemConstants.SubCategory.Hoe;
        }
        public bool IsSubCategoryWatering()
        {
            return IsTool() && SubCategory == ItemConstants.SubCategory.Watering;
        }

        public bool IsSeed()
        {
            return Category == ItemConstants.Category.Seed;
        }

        public bool IsSubCategoryHandHarvestable()
        {
            return SubCategory == ItemConstants.SubCategory.HandHarvestable;
        }

        public bool IsSubCategoryScytheHarvestable()
        {
            return SubCategory == ItemConstants.SubCategory.ScytheHarvestable;
        }
    }
    /// <summary>
    /// 아이템 테이블
    /// </summary>
    public class TableItem : DefaultTable
    {
        private static readonly Dictionary<string, ItemConstants.Type> MapType;
        private static readonly Dictionary<string, ItemConstants.Category> MapCategory;
        private static readonly Dictionary<string, ItemConstants.SubCategory> MapSubCategory;
        private static readonly Dictionary<string, ItemConstants.Class> MapClass;
        private static readonly Dictionary<string, ItemConstants.PartsType> MapPartsID;
        private static readonly Dictionary<string, ItemConstants.AntiFlag> MapAntiFlag;

        static TableItem()
        {
            MapType = new Dictionary<string, ItemConstants.Type>
            {
                { nameof(ItemConstants.Type.Equip), ItemConstants.Type.Equip },
                { nameof(ItemConstants.Type.Consumable), ItemConstants.Type.Consumable },
                { nameof(ItemConstants.Type.Currency), ItemConstants.Type.Currency },
                { nameof(ItemConstants.Type.Misc), ItemConstants.Type.Misc },
            };
            MapCategory = new Dictionary<string, ItemConstants.Category>
            {
                { nameof(ItemConstants.Category.Weapon), ItemConstants.Category.Weapon },
                { nameof(ItemConstants.Category.Armor), ItemConstants.Category.Armor },
                { nameof(ItemConstants.Category.Potion), ItemConstants.Category.Potion },
                { nameof(ItemConstants.Category.Gold), ItemConstants.Category.Gold },
                { nameof(ItemConstants.Category.Silver), ItemConstants.Category.Silver },
                { nameof(ItemConstants.Category.Material), ItemConstants.Category.Material },
                { nameof(ItemConstants.Category.Tool), ItemConstants.Category.Tool },
                { nameof(ItemConstants.Category.Seed), ItemConstants.Category.Seed },
                { nameof(ItemConstants.Category.Vegetable), ItemConstants.Category.Vegetable },
                { nameof(ItemConstants.Category.Grain), ItemConstants.Category.Grain },
                { nameof(ItemConstants.Category.Wood), ItemConstants.Category.Wood },
                { nameof(ItemConstants.Category.Ore), ItemConstants.Category.Ore },
            };
            MapSubCategory = new Dictionary<string, ItemConstants.SubCategory>
            {
                { nameof(ItemConstants.SubCategory.Sword), ItemConstants.SubCategory.Sword },
                { nameof(ItemConstants.SubCategory.Chest), ItemConstants.SubCategory.Chest },
                { nameof(ItemConstants.SubCategory.Boots), ItemConstants.SubCategory.Boots },
                { nameof(ItemConstants.SubCategory.RecoverHp), ItemConstants.SubCategory.RecoverHp },
                { nameof(ItemConstants.SubCategory.RecoverMp), ItemConstants.SubCategory.RecoverMp },
                { nameof(ItemConstants.SubCategory.IncreaseAttackSpeed), ItemConstants.SubCategory.IncreaseAttackSpeed },
                { nameof(ItemConstants.SubCategory.IncreaseMoveSpeed), ItemConstants.SubCategory.IncreaseMoveSpeed },
                { nameof(ItemConstants.SubCategory.Axe), ItemConstants.SubCategory.Axe },
                { nameof(ItemConstants.SubCategory.Hoe), ItemConstants.SubCategory.Hoe },
                { nameof(ItemConstants.SubCategory.PickAxe), ItemConstants.SubCategory.PickAxe },
                { nameof(ItemConstants.SubCategory.Sickle), ItemConstants.SubCategory.Sickle },
                { nameof(ItemConstants.SubCategory.Watering), ItemConstants.SubCategory.Watering },
                { nameof(ItemConstants.SubCategory.HandHarvestable), ItemConstants.SubCategory.HandHarvestable },
                { nameof(ItemConstants.SubCategory.ScytheHarvestable), ItemConstants.SubCategory.ScytheHarvestable },
            };
            MapClass = new Dictionary<string, ItemConstants.Class>
            {
                { nameof(ItemConstants.Class.Normal), ItemConstants.Class.Normal },
            };
            MapPartsID = new Dictionary<string, ItemConstants.PartsType>
            {
                { nameof(ItemConstants.PartsType.Helmet), ItemConstants.PartsType.Helmet },
                { nameof(ItemConstants.PartsType.Chest), ItemConstants.PartsType.Chest },
                { nameof(ItemConstants.PartsType.Shoulder), ItemConstants.PartsType.Shoulder },
                { nameof(ItemConstants.PartsType.Forearm), ItemConstants.PartsType.Forearm },
                { nameof(ItemConstants.PartsType.Gloves), ItemConstants.PartsType.Gloves },
                { nameof(ItemConstants.PartsType.Belt), ItemConstants.PartsType.Belt },
                { nameof(ItemConstants.PartsType.Pants), ItemConstants.PartsType.Pants },
                { nameof(ItemConstants.PartsType.Boots), ItemConstants.PartsType.Boots },
                { nameof(ItemConstants.PartsType.Weapon), ItemConstants.PartsType.Weapon },
                { nameof(ItemConstants.PartsType.Necklace), ItemConstants.PartsType.Necklace },
                { nameof(ItemConstants.PartsType.Ring), ItemConstants.PartsType.Ring },
                { nameof(ItemConstants.PartsType.Shield), ItemConstants.PartsType.Shield },
            };
            MapAntiFlag = new Dictionary<string, ItemConstants.AntiFlag>
            {
                { "Shop", ItemConstants.AntiFlag.ShopSale },
                { nameof(ItemConstants.AntiFlag.Stash), ItemConstants.AntiFlag.Stash },
                { nameof(ItemConstants.AntiFlag.Salvage), ItemConstants.AntiFlag.Salvage },
                { nameof(ItemConstants.AntiFlag.Upgrade), ItemConstants.AntiFlag.Upgrade },
            };
        }
        private static ItemConstants.Type ConvertType(string type) => MapType.GetValueOrDefault(type, ItemConstants.Type.None);
        private static ItemConstants.Category ConvertCategory(string type) => MapCategory.GetValueOrDefault(type, ItemConstants.Category.None);

        private static ItemConstants.SubCategory ConvertSubCategory(Dictionary<string, string> data)
        {
            var category = MapCategory.GetValueOrDefault(data["Category"], ItemConstants.Category.None);
            var subCategory = MapSubCategory.GetValueOrDefault(data["SubCategory"], ItemConstants.SubCategory.None);
            if (subCategory == ItemConstants.SubCategory.None) return ItemConstants.SubCategory.None;
            
            var result = ItemConstants.IsValidSubCategory(category, subCategory);
            if (result) return subCategory;
            
            GcLogger.LogError($"Category와 SubCategory 매칭이 잘 못 되었습니다. Uid:{data["Uid"]}, Category: {category}, SubCategory: {subCategory}");
            return ItemConstants.SubCategory.None;
        } 
        private static ItemConstants.Class ConvertClass(string type) => MapClass.GetValueOrDefault(type, ItemConstants.Class.None);
        private static ItemConstants.PartsType ConvertPartsID(string type) => MapPartsID.GetValueOrDefault(type, ItemConstants.PartsType.None);
        private static ItemConstants.AntiFlag[] ConvertAntiFlag(string type)
        {
            string[] flags = type.Split(',');
            ItemConstants.AntiFlag[] antiFlags = new ItemConstants.AntiFlag[flags.Length];
            for (int i = 0; i < antiFlags.Length; i++)
            {
                antiFlags[i] = MapAntiFlag.GetValueOrDefault(flags[i], ItemConstants.AntiFlag.None);
            }
            return antiFlags;
        }

        /// <summary>
        /// Description 문자열 내 {컬럼명} 형태의 플레이스홀더를 실제 값으로 치환합니다.
        /// </summary>
        /// <param name="template">설명 문자열</param>
        /// <param name="values">컬럼 이름과 값이 들어 있는 딕셔너리</param>
        /// <returns>치환된 문자열</returns>
        private static string ParsePlaceholders(string template, Dictionary<string, string> values)
        {
            // 정규식: 중괄호 {} 안의 내용을 캡처
            Regex regex = new Regex(@"\{(.*?)\}");

            return regex.Replace(template, match =>
            {
                string key = match.Groups[1].Value;

                if (values.TryGetValue(key, out var value))
                {
                    return value ?? "";
                }

                // 해당 키가 없으면 원래 문자열 유지
                return match.Value;
            });
        }

        private static string ConvertImagePath(Dictionary<string, string> values, ItemConstants.Category category, ItemConstants.SubCategory subCategory)
        {
            string type = values["Type"];
            string categoryValue = values["Category"];
            string subCategoryValue = values["SubCategory"];
            string imagePath = values["ImagePath"];
            string newImagePath = $"Images/Icon/Item/{type}/{categoryValue}/{subCategoryValue}/{imagePath}";
            if (category == ItemConstants.Category.None && subCategory == ItemConstants.SubCategory.None)
            {
                newImagePath = $"Images/Icon/Item/{type}/{imagePath}";
            }
            else if (subCategory == ItemConstants.SubCategory.None)
            {
                newImagePath = $"Images/Icon/Item/{type}/{categoryValue}/{imagePath}";
            }
            return newImagePath;
        }
        private readonly Dictionary<ItemConstants.Category, List<StruckTableItem>> _dictionaryByCategory = new Dictionary<ItemConstants.Category, List<StruckTableItem>>();
        private readonly Dictionary<ItemConstants.SubCategory, List<StruckTableItem>> _dictionaryBySubCategory = new Dictionary<ItemConstants.SubCategory, List<StruckTableItem>>();

        private static string GetAntiFlagName(ItemConstants.AntiFlag antiFlag)
        {
            if (LocalizationManager.Instance)
            {
                return antiFlag switch
                {
                    ItemConstants.AntiFlag.ShopSale => LocalizationManager.Instance.GetCommonGameByKey("Item_AntiFlag_Shop"),
                    ItemConstants.AntiFlag.Stash => LocalizationManager.Instance.GetCommonGameByKey("Item_AntiFlag_Stash"),
                    ItemConstants.AntiFlag.Salvage => LocalizationManager.Instance.GetCommonGameByKey("Item_AntiFlag_Salvage"),
                    ItemConstants.AntiFlag.Upgrade => LocalizationManager.Instance.GetCommonGameByKey("Item_AntiFlag_Upgrade"),
                    _ => ""
                };
            }
            else
            {
                return antiFlag switch
                {
                    ItemConstants.AntiFlag.ShopSale => "AntiFlag_Shop",
                    ItemConstants.AntiFlag.Stash => "AntiFlag_Stash",
                    ItemConstants.AntiFlag.Salvage => "AntiFlag_Salvage",
                    ItemConstants.AntiFlag.Upgrade => "AntiFlag_Upgrade",
                    _ => ""
                }; 
            }
        }
        protected override void OnLoadedData(Dictionary<string, string> data)
        {
            int uid = int.Parse(data["Uid"]);
            int upgrade = int.Parse(data["Upgrade"]);
            string name = data["Name"];
            if (LocalizationManager.Instance != null)
            {
                name = LocalizationManager.Instance.GetItemNameByKey(uid.ToString());   
            }
            if (upgrade > 0)
            {
                data["Name"] = $"{name} +{data["Upgrade"]}"; 
            }

            ItemConstants.Category category = ConvertCategory(data["Category"]);
            ItemConstants.SubCategory subCategory = ConvertSubCategory(data);
            string desc = data["Description"];
            if (LocalizationManager.Instance != null)
            {
                desc = LocalizationManager.Instance.GetItemDescriptionByKey(uid.ToString());   
            }
            data["Description"] = ParsePlaceholders(desc, data);
            data["PartsImagePath"] = $"Images/Parts/{data["PartsID"]}/{data["ImagePath"]}";
            if (data["PartsID"] == "")
            {
                data["PartsImagePath"] = $"Images/Parts/{data["ImagePath"]}";
            }
            
            // 아이콘 이미지 경로
            data["FileName"] = data["ImagePath"];
            data["ImagePath"] = ConvertImagePath(data, category, subCategory);
            // 드랍 아이템 이미지 경로
            data["ImageItemPath"] = data["ImagePath"].Replace("/Icon/Item/", "/Item/");

            // Anti Flag를 item info 에서 보여줄 문구로 변환
            ItemConstants.AntiFlag[] antiFlags = ConvertAntiFlag(data["AntiFlag"]);
            string antiFlag = "";
            foreach (var t in antiFlags)
            {
                if (antiFlag != "")
                {
                    antiFlag += ",";
                }
                antiFlag += GetAntiFlagName(t);
            }
            data["AntiFlagText"] = antiFlag;
            
            StruckTableItem struckTableItemDropGroup = GetDataByUid(uid);
            {
                if (!_dictionaryByCategory.ContainsKey(category))
                {
                    _dictionaryByCategory[category] = new List<StruckTableItem>();
                }

                _dictionaryByCategory[category].Add(struckTableItemDropGroup);
            }
            {
                if (!_dictionaryBySubCategory.ContainsKey(subCategory))
                {
                    _dictionaryBySubCategory[subCategory] = new List<StruckTableItem>();
                }

                _dictionaryBySubCategory[subCategory].Add(struckTableItemDropGroup);
            }
        }

        public Dictionary<ItemConstants.Category, List<StruckTableItem>> GetDictionaryByCategory()
        {
            return _dictionaryByCategory;
        }
        public Dictionary<ItemConstants.SubCategory, List<StruckTableItem>> GetDictionaryBySubCategory()
        {
            return _dictionaryBySubCategory;
        }
        
        public StruckTableItem GetDataByUid(int uid)
        {
            if (uid <= 0)
            {
                GcLogger.LogError("uid is 0.");
                return null;
            }
            var data = GetData(uid);
            if (data == null) return null;
            return new StruckTableItem
            {
                Uid = int.Parse(data["Uid"]),
                Name = data["Name"],
                Type = ConvertType(data["Type"]),
                Category = ConvertCategory(data["Category"]),
                SubCategory = ConvertSubCategory(data),
                PartsID = ConvertPartsID(data["PartsID"]),
                PartsImagePath = data["PartsImagePath"],
                ImageItemPath = data["ImageItemPath"],
                Class = ConvertClass(data["Class"]),
                ImagePath = data["ImagePath"],
                Upgrade = int.Parse(data["Upgrade"]),
                AntiFlag = ConvertAntiFlag(data["AntiFlag"]),
                AntiFlagText = data["AntiFlagText"],
                MaxOverlayCount = int.Parse(data["MaxOverlayCount"]),
                CoolTime = float.Parse(data["CoolTime"]),
                Description = data["Description"],
                SaleCurrencyType = ConvertCurrencyType(data["SaleCurrencyType"]),
                SaleCurrencyValue = int.Parse(data["SaleCurrencyValue"]),
                
                StatusID1 = data["StatusID1"],
                StatusSuffix1 = ConvertSuffixType(data["StatusSuffix1"]),
                StatusValue1 = int.Parse(data["StatusValue1"]),
                StatusID2 = data["StatusID2"],
                StatusSuffix2 = ConvertSuffixType(data["StatusSuffix2"]),
                StatusValue2 = int.Parse(data["StatusValue2"]),
                
                OptionType1 = data["OptionType1"],
                OptionSuffix1 = ConvertSuffixType(data["OptionSuffix1"]),
                OptionValue1 = int.Parse(data["OptionValue1"]),
                OptionType2 = data["OptionType2"],
                OptionSuffix2 = ConvertSuffixType(data["OptionSuffix2"]),
                OptionValue2 = int.Parse(data["OptionValue2"]),
                OptionType3 = data["OptionType3"],
                OptionSuffix3 = ConvertSuffixType(data["OptionSuffix3"]),
                OptionValue3 = int.Parse(data["OptionValue3"]),
                OptionType4 = data["OptionType4"],
                OptionSuffix4 = ConvertSuffixType(data["OptionSuffix4"]),
                OptionValue4 = int.Parse(data["OptionValue4"]),
                OptionType5 = data["OptionType5"],
                OptionSuffix5 = ConvertSuffixType(data["OptionSuffix5"]),
                OptionValue5 = int.Parse(data["OptionValue5"]),
                
                FileName = data["FileName"],
            };
        }
        public override bool TryGetDataByUid(int uid, out object info)
        {
            info = GetDataByUid(uid);
            return info != null && ((StruckTableItem)info).Uid > 0;
        }
    }
}