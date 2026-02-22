using System;
using System.Collections.Generic;

namespace GGemCo2DCore
{
    public static class CharacterConstantsNpc
    {
        /// <summary>
        /// NPC의 기본 존재 유형
        /// </summary>
        public enum NpcType
        {
            Character,     // 사람형, AI, 몬스터 등
            Object,        // 나무, 상자 등 비생명형
        }

        /// <summary>
        /// NPC의 기능/목적 분류
        /// </summary>
        public enum NpcCategory
        {
            Normal,        // 단순 대화형
            Functional,    // 상점, 창고 등 기능 제공형
            Collectible,   // 수집/채집형 오브젝트
            Quest,          // 이벤트/퀘스트용
            Furniture          // 가구
        }

        /// <summary>
        /// NPC의 세부 기능 분류
        /// </summary>
        public enum NpcSubCategory
        {
            None,
            // Normal
            Villager, Traveler, StoryNpc,
            // Functional
            Shopkeeper,         // 상점
            Enchanter,          // 아이템 강화
            Disassembler,       // 아이템 분해
            StorageKeeper,      // 창고
            Crafter,            // 제작
            Alchemist,          // 포션 제작
            // Collectible
            Tree, Rock, Ore, Chest, Plant, FishingSpot,
            // Quest
            QuestGiver, CutsceneActor, EventTrigger,
            // Furniture
            Bed
        }
        
        private static readonly Dictionary<NpcType, HashSet<NpcCategory>> TypeToCategory = new()
        {
            { NpcType.Character, new HashSet<NpcCategory> { NpcCategory.Normal, NpcCategory.Functional, NpcCategory.Quest } },
            { NpcType.Object,    new HashSet<NpcCategory> { NpcCategory.Collectible, NpcCategory.Furniture } },
        };

        private static readonly Dictionary<NpcCategory, HashSet<NpcSubCategory>> CategoryToSub = new()
        {
            {
                NpcCategory.Normal,
                new HashSet<NpcSubCategory> { NpcSubCategory.Villager, NpcSubCategory.Traveler, NpcSubCategory.StoryNpc }
            },
            {
                NpcCategory.Functional,
                new HashSet<NpcSubCategory>
                {
                    NpcSubCategory.None,
                    NpcSubCategory.Shopkeeper,
                    NpcSubCategory.Enchanter,
                    NpcSubCategory.Disassembler,
                    NpcSubCategory.StorageKeeper,
                    NpcSubCategory.Crafter,
                    NpcSubCategory.Alchemist
                }
            },
            {
                NpcCategory.Collectible,
                new HashSet<NpcSubCategory> { NpcSubCategory.Tree, NpcSubCategory.Rock, NpcSubCategory.Ore, NpcSubCategory.Chest, NpcSubCategory.Plant, NpcSubCategory.FishingSpot }
            },
            {
                NpcCategory.Quest,
                new HashSet<NpcSubCategory> { NpcSubCategory.QuestGiver, NpcSubCategory.CutsceneActor, NpcSubCategory.EventTrigger }
            },
            {
                NpcCategory.Furniture,
                new HashSet<NpcSubCategory> { NpcSubCategory.Bed }
            }
        };

        /// <summary>
        /// Type → Category 조합이 올바른지 검사
        /// </summary>
        public static bool IsValidTypeCategory(NpcType type, NpcCategory category)
        {
            return TypeToCategory.TryGetValue(type, out var categories) && categories.Contains(category);
        }

        /// <summary>
        /// Category → SubCategory 조합이 올바른지 검사
        /// </summary>
        public static bool IsValidCategorySub(NpcCategory category, NpcSubCategory sub)
        {
            return CategoryToSub.TryGetValue(category, out var subs) && subs.Contains(sub);
        }

        /// <summary>
        /// Type, Category, SubCategory 전체 조합이 유효한지 검사
        /// </summary>
        public static bool IsValidCombination(NpcType type, NpcCategory category, NpcSubCategory sub)
        {
            if (!IsValidTypeCategory(type, category))
            {
                GcLogger.LogWarning($"Invalid combination: Type {type} cannot have Category {category}.");
                return false;
            }

            if (!IsValidCategorySub(category, sub))
            {
                GcLogger.LogWarning($"Invalid combination: Category {category} cannot have SubCategory {sub}.");
                return false;
            }

            return true;
        }
        
        /// <summary>
        /// 문자열을 NpcType enum으로 변환
        /// </summary>
        public static NpcType ParseType(string typeStr)
        {
            if (string.IsNullOrEmpty(typeStr))
            {
                typeStr = "None";
            }
            if (Enum.TryParse(typeStr, true, out NpcType result))
                return result;

            GcLogger.LogWarning($"[CharacterConstantsNpc] Unknown NpcType: {typeStr}");
            return default;
        }

        /// <summary>
        /// 문자열을 NpcCategory enum으로 변환
        /// </summary>
        public static NpcCategory ParseCategory(string categoryStr)
        {
            if (string.IsNullOrEmpty(categoryStr))
            {
                categoryStr = "None";
            }
            if (Enum.TryParse(categoryStr, true, out NpcCategory result))
                return result;

            GcLogger.LogWarning($"[CharacterConstantsNpc] Unknown NpcCategory: {categoryStr}");
            return default;
        }

        /// <summary>
        /// 문자열을 NpcSubCategory enum으로 변환
        /// </summary>
        public static NpcSubCategory ParseSubCategory(string subStr)
        {
            if (string.IsNullOrEmpty(subStr))
            {
                subStr = "None";
            }
            if (Enum.TryParse(subStr, true, out NpcSubCategory result))
                return result;

            GcLogger.LogWarning($"[CharacterConstantsNpc] Unknown NpcSubCategory: {subStr}");
            return default;
        }

        /// <summary>
        /// 문자열 3개(Type, Category, SubCategory)를 모두 Enum으로 변환 후 유효성 검사
        /// </summary>
        public static bool TryParseAndValidate(
            string typeStr, string categoryStr, string subStr,
            out NpcType type, out NpcCategory category, out NpcSubCategory sub)
        {
            type = ParseType(typeStr);
            category = ParseCategory(categoryStr);
            sub = ParseSubCategory(subStr);

            if (!IsValidCombination(type, category, sub))
            {
                GcLogger.LogWarning($"[CharacterConstantsNpc] Invalid NPC combination: {type}/{category}/{sub}");
                return false;
            }

            return true;
        }
        public static bool TryParseAndValidate(
            string typeStr, string categoryStr, string subStr)
        {
            var type = ParseType(typeStr);
            var category = ParseCategory(categoryStr);
            var sub = ParseSubCategory(subStr);

            if (!IsValidCombination(type, category, sub))
            {
                GcLogger.LogWarning($"[CharacterConstantsNpc] Invalid NPC combination: {type}/{category}/{sub}");
                return false;
            }

            return true;
        }
    }
}
