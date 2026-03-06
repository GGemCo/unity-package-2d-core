using System;
using System.Collections.Generic;

namespace GGemCo2DCore
{
    /// <summary>
    /// 스킬 지급(ActionType.GrantSkill)
    /// - Core는 Skill 패키지를 직접 참조하지 않고, 인터페이스로만 연동합니다.
    /// - 중복 정책(dup)을 ParamStringA/B로 확장합니다.
    ///   예) dup=Ignore
    ///       dup=LevelUp
    ///       dup=AlternativeReward;altKind=Exp;altValue=100
    ///
    /// (레거시) ParamStringA == "Y" 는 "중복이면 실패"로 간주합니다.
    /// </summary>
    public sealed class ItemUseActionGrantSkill : IItemUseAction
    {
        private readonly int _skillUid;
        private readonly int _level;
        private readonly string _optA;
        private readonly string _optB;

        public ItemUseActionGrantSkill(int skillUid, int level, string optA, string optB)
        {
            _skillUid = skillUid;
            _level = level <= 0 ? 1 : level;
            _optA = optA;
            _optB = optB;
        }

        public ResultCommon CanExecute(ItemUseContext ctx)
        {
            if (_skillUid <= 0) return ResultCommon.Fail("ItemUse_InvalidSkill");
            if (ctx?.SkillReceiver == null) return ResultCommon.Fail("ItemUse_NoSkillReceiver");

            // 레거시: "Y" == 중복이면 실패
            if (string.Equals(_optA, "Y", StringComparison.OrdinalIgnoreCase))
            {
                if (ctx.SkillReceiver.HasSkill(_skillUid))
                    return ResultCommon.Fail("ItemUse_AlreadyHasSkill");
                return ResultCommon.Success();
            }

            var args = ParseArgs(_optA, _optB);
            var dup = GetDuplicatePolicy(args);

            if (!ctx.SkillReceiver.HasSkill(_skillUid))
                return ResultCommon.Success();

            switch (dup)
            {
                case SkillDuplicatePolicy.Fail:
                    return ResultCommon.Fail("ItemUse_AlreadyHasSkill");
                case SkillDuplicatePolicy.Ignore:
                    return ResultCommon.Success();
                case SkillDuplicatePolicy.LevelUp:
                    return (ctx.SkillReceiver is IItemUseSkillReceiverEx)
                        ? ResultCommon.SuccessWithIcons(null)
                        : ResultCommon.Fail("ItemUse_SkillReceiverNoLevelUp");
                case SkillDuplicatePolicy.AlternativeReward:
                    return ValidateAlternativeReward(ctx, args);
                default:
                    return ResultCommon.Fail("ItemUse_InvalidConfig");
            }
        }

        public ResultCommon Execute(ItemUseContext ctx)
        {
            // 레거시: "Y" == 중복이면 실패
            if (string.Equals(_optA, "Y", StringComparison.OrdinalIgnoreCase))
            {
                if (ctx.SkillReceiver.HasSkill(_skillUid))
                    return ResultCommon.Fail("ItemUse_AlreadyHasSkill");

                return Grant(ctx);
            }

            var args = ParseArgs(_optA, _optB);
            var dup = GetDuplicatePolicy(args);

            if (!ctx.SkillReceiver.HasSkill(_skillUid))
                return Grant(ctx);

            switch (dup)
            {
                case SkillDuplicatePolicy.Fail:
                    return ResultCommon.Fail("ItemUse_AlreadyHasSkill");
                case SkillDuplicatePolicy.Ignore:
                    return ResultCommon.Success();
                case SkillDuplicatePolicy.LevelUp:
                    return LevelUp(ctx);
                case SkillDuplicatePolicy.AlternativeReward:
                    return ApplyAlternativeReward(ctx, args);
                default:
                    return ResultCommon.Fail("ItemUse_InvalidConfig");
            }
        }

        private ResultCommon Grant(ItemUseContext ctx)
        {
            if (!ctx.SkillReceiver.TryGrantSkill(_skillUid, _level, out var messageKey))
            {
                return ResultCommon.Fail(string.IsNullOrEmpty(messageKey) ? "ItemUse_GrantSkill_Fail" : messageKey);
            }
            return ResultCommon.Success();
        }

        private ResultCommon LevelUp(ItemUseContext ctx)
        {
            if (ctx.SkillReceiver is not IItemUseSkillReceiverEx ex)
                return ResultCommon.Fail("ItemUse_SkillReceiverNoLevelUp");

            if (!ex.TryLevelUpSkill(_skillUid, _level, out var messageKey))
            {
                return ResultCommon.Fail(string.IsNullOrEmpty(messageKey) ? "ItemUse_LevelUpSkill_Fail" : messageKey);
            }

            return ResultCommon.Success();
        }

        private static ResultCommon ValidateAlternativeReward(ItemUseContext ctx, Dictionary<string, string> args)
        {
            if (ctx?.PlayerData == null) return ResultCommon.Fail("ItemUse_NoPlayerData");

            var altKind = GetArg(args, "altKind");
            if (string.IsNullOrWhiteSpace(altKind)) return ResultCommon.Fail("ItemUse_InvalidConfig");

            if (!TryGetIntArg(args, "altValue", out var altValue) || altValue <= 0)
                return ResultCommon.Fail("ItemUse_InvalidConfig");

            if (!altKind.Equals("Exp", StringComparison.OrdinalIgnoreCase)
                && !altKind.Equals("StatPoints", StringComparison.OrdinalIgnoreCase))
            {
                return ResultCommon.Fail("ItemUse_InvalidConfig");
            }

            return ResultCommon.Success();
        }

        private static ResultCommon ApplyAlternativeReward(ItemUseContext ctx, Dictionary<string, string> args)
        {
            var can = ValidateAlternativeReward(ctx, args);
            if (can is not { Result: ResultCommon.ResultType.Success }) return can;

            var altKind = GetArg(args, "altKind");
            TryGetIntArg(args, "altValue", out var altValue);

            if (altKind.Equals("Exp", StringComparison.OrdinalIgnoreCase))
            {
                ctx.PlayerData.AddExp(altValue);
            }
            else
            {
                ctx.PlayerData.UnspentStatPoints += altValue;
            }

            return ResultCommon.Success();
        }

        private static SkillDuplicatePolicy GetDuplicatePolicy(Dictionary<string, string> args)
        {
            var dup = GetArg(args, "dup");
            if (string.IsNullOrWhiteSpace(dup)) return SkillDuplicatePolicy.Fail;

            if (dup.Equals("Ignore", StringComparison.OrdinalIgnoreCase)) return SkillDuplicatePolicy.Ignore;
            if (dup.Equals("LevelUp", StringComparison.OrdinalIgnoreCase)) return SkillDuplicatePolicy.LevelUp;
            if (dup.Equals("AlternativeReward", StringComparison.OrdinalIgnoreCase)) return SkillDuplicatePolicy.AlternativeReward;
            if (dup.Equals("Fail", StringComparison.OrdinalIgnoreCase)) return SkillDuplicatePolicy.Fail;

            return SkillDuplicatePolicy.Fail;
        }

        private static Dictionary<string, string> ParseArgs(string a, string b)
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            ParseInto(dict, a);
            ParseInto(dict, b);
            return dict;
        }

        private static void ParseInto(Dictionary<string, string> dict, string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return;

            var parts = raw.Split(new[] { ';', ',', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < parts.Length; i++)
            {
                var p = parts[i].Trim();
                if (p.Length == 0) continue;

                int eq = p.IndexOf('=');
                if (eq <= 0 || eq >= p.Length - 1) continue;

                var k = p.Substring(0, eq).Trim();
                var v = p.Substring(eq + 1).Trim();
                if (k.Length == 0) continue;

                dict[k] = v;
            }
        }

        private static string GetArg(Dictionary<string, string> dict, string key)
        {
            if (dict == null) return null;
            return dict.TryGetValue(key, out var v) ? v : null;
        }

        private static bool TryGetIntArg(Dictionary<string, string> dict, string key, out int value)
        {
            value = 0;
            var v = GetArg(dict, key);
            if (string.IsNullOrWhiteSpace(v)) return false;
            return int.TryParse(v, out value);
        }
    }
}
