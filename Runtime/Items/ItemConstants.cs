using System.Collections.Generic;

namespace GGemCo2DCore
{
    public static class ItemConstants
    {
        public enum Type
        {
            None,
            Equip, // 장비
            Consumable, //소모품
            Currency,
            Misc
        }

        public enum Category
        {
            None,
            Weapon, // 무기
            Armor, // 방어구
            Potion, // 물약
            Gold,
            Silver,
            Material,
            Tool,
            Seed,
            Vegetable,
            Grain,
            Wood,
            Ore,
            Remnant,
            SkillBook
        }

        public enum SubCategory
        {
            None,
            Sword, // 칼
            Chest, // 상의
            Boots, // 신발
            RecoverHp, // hp 물약
            RecoverMp, // mp 물약
            IncreaseAttackSpeed, // 공격속도 증가
            IncreaseMoveSpeed, // 이동속도 증가
            Axe, // 도끼
            Hoe, // 호미
            PickAxe, // 곡괭이
            Sickle, // 낫
            Watering, // 물뿌리개
            HandHarvestable, // 손으로 수확
            ScytheHarvestable, // 낫으로 수확
            IncreaseHp,
            Active,
            Passive,
            ActiveBuff,
            Status
        }

        public enum Class
        {
            Normal, // 일반
            Magic,
            Rare,
            Unique,
        }

        public enum DropVisualType
        {
            Sprite,
            Vfx,
        }

        public enum PartsType
        {
            None,
            Helmet,
            Chest,
            Shoulder,
            Forearm,
            Gloves,
            Belt,
            Pants,
            Boots,
            Weapon,
            Necklace,
            Ring,
            Shield,
        }
        public enum AntiFlag
        {
            None,
            ShopSale,
            Stash,
            Salvage,
            Upgrade,
        }
        /// <summary>
        /// 부위별 리소스 폴더 이름
        /// </summary>
        public static readonly Dictionary<PartsType, string> FolderNameByPartsType = new Dictionary<PartsType, string>
        {
            { PartsType.Helmet, "Helmet" },
            { PartsType.Chest, "Chest" },
            { PartsType.Shoulder, "Shoulder" },
            { PartsType.Forearm, "Forearm" },
            { PartsType.Gloves, "Gloves" },
            { PartsType.Belt, "Belt" },
            { PartsType.Pants, "Pants" },
            { PartsType.Boots, "Boots" },
            { PartsType.Weapon, "Weapon" },
            { PartsType.Necklace, "Necklace" },
            { PartsType.Ring, "Ring" },
            { PartsType.Shield, "Shield" },
        };
        /// <summary>
        /// 부위별 스파인 슬롯 이름
        /// </summary>
        public static readonly Dictionary<PartsType, List<string>> SlotNameByPartsType = new Dictionary<PartsType, List<string>>
        {
            { PartsType.Chest, new List<string> { "body" } },
            { PartsType.Boots, new List<string> { "leg_l", "leg_r" } },
            { PartsType.Weapon, new List<string> { "knife" } },
        };
        /// <summary>
        /// 슬롯별 어태치먼트 이름
        /// </summary>
        public static readonly Dictionary<string, string> AttachmentNameBySlotName = new Dictionary<string, string>
        {
            { "body", "body" },
            { "leg_l", "leg_l" },
            { "leg_r", "leg_r" },
            { "knife", "knife" },
            { "knife2", "knife" },
        };

        public static readonly Dictionary<ConfigCommon.SuffixType, string> StatusSuffixFormats = new Dictionary<ConfigCommon.SuffixType, string>
        {
            { ConfigCommon.SuffixType.Plus, "+{0}" },
            { ConfigCommon.SuffixType.Minus, "-{0}" },
            { ConfigCommon.SuffixType.Increase, "+{0}%" },
            { ConfigCommon.SuffixType.Decrease, "-{0}%" }
        };
        // -------------------------------------------------------------
        // Category별 선택 가능한 SubCategory 매핑
        // -------------------------------------------------------------
        public static readonly Dictionary<Category, List<SubCategory>> SubCategoriesByCategory = new()
        {
            {
                Category.Weapon, new List<SubCategory>
                {
                    SubCategory.Sword,
                }
            },
            {
                Category.Armor, new List<SubCategory>
                {
                    SubCategory.Chest,
                    SubCategory.Boots
                }
            },
            {
                Category.Potion, new List<SubCategory>
                {
                    SubCategory.RecoverHp,
                    SubCategory.RecoverMp,
                    SubCategory.IncreaseAttackSpeed,
                    SubCategory.IncreaseMoveSpeed,
                    SubCategory.IncreaseHp,
                    SubCategory.Status,
                }
            },
            {
                Category.Tool, new List<SubCategory>
                {
                    SubCategory.Axe,
                    SubCategory.Hoe,
                    SubCategory.PickAxe,
                    SubCategory.Sickle,
                    SubCategory.Watering,
                }
            },
            {
                Category.Seed, new List<SubCategory>
                {
                    SubCategory.HandHarvestable,
                    SubCategory.ScytheHarvestable,
                }
            },
            {
                Category.SkillBook, new List<SubCategory>
                {
                    SubCategory.Active,
                    SubCategory.ActiveBuff,
                    SubCategory.Passive,
                }
            }
        };
        /// <summary>
        /// 주어진 Category에 SubCategory가 유효한 조합인지 검사합니다.
        /// </summary>
        public static bool IsValidSubCategory(Category category, SubCategory subCategory)
        {
            // None은 항상 무효 처리
            if (category == Category.None || subCategory == SubCategory.None)
                return false;

            // Category가 등록되어 있는지 확인
            if (!SubCategoriesByCategory.TryGetValue(category, out var list))
                return false;

            // SubCategory가 목록에 포함되어 있는지 확인
            return list.Contains(subCategory);
        }

        /// <summary>
        /// Category에 포함된 모든 SubCategory를 반환합니다. (없을 경우 빈 리스트)
        /// </summary>
        public static IReadOnlyList<SubCategory> GetSubCategories(Category category)
        {
            return SubCategoriesByCategory.TryGetValue(category, out var list)
                ? list
                : System.Array.Empty<SubCategory>();
        }
    }
}
