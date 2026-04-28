using System;
using System.Collections.Generic;

namespace GGemCo2DCore
{
    /// <summary>
    /// 패시브 스킬 지급(ActionType.GrantSkillPassive)을 처리합니다.
    /// Core는 Skill 패키지를 직접 참조하지 않고, 패시브 스킬 지급 인터페이스로만 연동합니다.
    /// </summary>
    public sealed class ItemUseActionGrantSkillPassive : IItemUseAction
    {
        private readonly int _skillPassiveUid;
        private readonly int _level;
        private readonly string _optA;
        private readonly string _optB;

        /// <summary>
        /// 패시브 스킬 지급 액션을 생성합니다.
        /// </summary>
        /// <param name="skillPassiveUid">지급할 패시브 스킬 UID입니다.</param>
        /// <param name="level">지급할 레벨입니다. 0 이하이면 1로 보정합니다.</param>
        /// <param name="optA">중복 정책 등 선택 인자 문자열입니다.</param>
        /// <param name="optB">추가 선택 인자 문자열입니다.</param>
        public ItemUseActionGrantSkillPassive(int skillPassiveUid, int level, string optA, string optB)
        {
            _skillPassiveUid = skillPassiveUid;
            _level = level <= 0 ? 1 : level;
            _optA = optA;
            _optB = optB;
        }

        /// <summary>
        /// 패시브 스킬 지급이 가능한지 사전 검사합니다.
        /// </summary>
        /// <param name="ctx">아이템 사용 컨텍스트입니다.</param>
        /// <returns>실행 가능 여부와 실패 메시지를 담은 결과입니다.</returns>
        public ResultCommon CanExecute(ItemUseContext ctx)
        {
            if (_skillPassiveUid <= 0) return ResultCommon.Fail("ItemUse_InvalidSkill");
            if (ctx?.SkillPassiveReceiver == null) return ResultCommon.Fail("ItemUse_NoSkillReceiver");

            if (string.Equals(_optA, "Y", StringComparison.OrdinalIgnoreCase))
            {
                if (ctx.SkillPassiveReceiver.HasPassiveSkill(_skillPassiveUid))
                    return ResultCommon.Fail("ItemUse_AlreadyHasSkill");
                return ResultCommon.Success();
            }

            var args = ParseArgs(_optA, _optB);
            var dup = GetDuplicatePolicy(args);

            if (!ctx.SkillPassiveReceiver.HasPassiveSkill(_skillPassiveUid))
                return ResultCommon.Success();

            switch (dup)
            {
                case SkillDuplicatePolicy.Fail:
                    return ResultCommon.Fail("ItemUse_AlreadyHasSkill");
                case SkillDuplicatePolicy.Ignore:
                    return ResultCommon.Success();
                case SkillDuplicatePolicy.LevelUp:
                    return (ctx.SkillPassiveReceiver is IItemUseSkillPassiveReceiverEx)
                        ? ResultCommon.SuccessWithIcons(null)
                        : ResultCommon.Fail("ItemUse_SkillReceiverNoLevelUp");
                case SkillDuplicatePolicy.AlternativeReward:
                    return ValidateAlternativeReward(ctx, args);
                default:
                    return ResultCommon.Fail("ItemUse_InvalidConfig");
            }
        }

        /// <summary>
        /// 패시브 스킬 지급을 실행합니다.
        /// </summary>
        /// <param name="ctx">아이템 사용 컨텍스트입니다.</param>
        /// <returns>지급, 레벨업, 대체 보상 중 실제 처리 결과입니다.</returns>
        public ResultCommon Execute(ItemUseContext ctx)
        {
            if (string.Equals(_optA, "Y", StringComparison.OrdinalIgnoreCase))
            {
                if (ctx.SkillPassiveReceiver.HasPassiveSkill(_skillPassiveUid))
                    return ResultCommon.Fail("ItemUse_AlreadyHasSkill");

                return Grant(ctx);
            }

            var args = ParseArgs(_optA, _optB);
            var dup = GetDuplicatePolicy(args);

            if (!ctx.SkillPassiveReceiver.HasPassiveSkill(_skillPassiveUid))
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

        /// <summary>
        /// 신규 패시브 스킬 지급을 실행합니다.
        /// </summary>
        /// <param name="ctx">아이템 사용 컨텍스트입니다.</param>
        /// <returns>지급 결과입니다.</returns>
        private ResultCommon Grant(ItemUseContext ctx)
        {
            if (!ctx.SkillPassiveReceiver.TryGrantPassiveSkill(_skillPassiveUid, _level, out var messageKey))
            {
                return ResultCommon.Fail(string.IsNullOrEmpty(messageKey) ? "ItemUse_GrantSkill_Fail" : messageKey);
            }

            return ResultCommon.Success();
        }

        /// <summary>
        /// 이미 보유한 패시브 스킬의 레벨업을 실행합니다.
        /// </summary>
        /// <param name="ctx">아이템 사용 컨텍스트입니다.</param>
        /// <returns>레벨업 결과입니다.</returns>
        private ResultCommon LevelUp(ItemUseContext ctx)
        {
            if (ctx.SkillPassiveReceiver is not IItemUseSkillPassiveReceiverEx ex)
                return ResultCommon.Fail("ItemUse_SkillReceiverNoLevelUp");

            if (!ex.TryLevelUpPassiveSkill(_skillPassiveUid, _level, out var messageKey))
            {
                return ResultCommon.Fail(string.IsNullOrEmpty(messageKey) ? "ItemUse_LevelUpSkill_Fail" : messageKey);
            }

            return ResultCommon.Success();
        }

        /// <summary>
        /// 대체 보상 설정이 실행 가능한지 검사합니다.
        /// </summary>
        /// <param name="ctx">아이템 사용 컨텍스트입니다.</param>
        /// <param name="args">파싱된 옵션 인자입니다.</param>
        /// <returns>대체 보상 설정 검증 결과입니다.</returns>
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

        /// <summary>
        /// 대체 보상을 실제 플레이어 데이터에 적용합니다.
        /// </summary>
        /// <param name="ctx">아이템 사용 컨텍스트입니다.</param>
        /// <param name="args">파싱된 옵션 인자입니다.</param>
        /// <returns>대체 보상 지급 결과입니다.</returns>
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

        /// <summary>
        /// 옵션 인자에서 중복 정책을 읽습니다.
        /// </summary>
        /// <param name="args">파싱된 옵션 인자입니다.</param>
        /// <returns>설정된 중복 정책입니다. 값이 없거나 알 수 없으면 Fail입니다.</returns>
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

        /// <summary>
        /// 세미콜론, 쉼표, 줄바꿈으로 구분된 key=value 옵션 문자열을 파싱합니다.
        /// </summary>
        /// <param name="a">첫 번째 옵션 문자열입니다.</param>
        /// <param name="b">두 번째 옵션 문자열입니다.</param>
        /// <returns>대소문자를 구분하지 않는 옵션 딕셔너리입니다.</returns>
        private static Dictionary<string, string> ParseArgs(string a, string b)
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            ParseInto(dict, a);
            ParseInto(dict, b);
            return dict;
        }

        /// <summary>
        /// 옵션 문자열 하나를 딕셔너리에 병합합니다.
        /// </summary>
        /// <param name="dict">옵션을 누적할 딕셔너리입니다.</param>
        /// <param name="raw">원본 옵션 문자열입니다.</param>
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

        /// <summary>
        /// 옵션 딕셔너리에서 문자열 값을 조회합니다.
        /// </summary>
        /// <param name="dict">옵션 딕셔너리입니다.</param>
        /// <param name="key">조회할 키입니다.</param>
        /// <returns>값이 있으면 해당 문자열, 없으면 null입니다.</returns>
        private static string GetArg(Dictionary<string, string> dict, string key)
        {
            if (dict == null) return null;
            return dict.TryGetValue(key, out var v) ? v : null;
        }

        /// <summary>
        /// 옵션 딕셔너리에서 정수 값을 조회합니다.
        /// </summary>
        /// <param name="dict">옵션 딕셔너리입니다.</param>
        /// <param name="key">조회할 키입니다.</param>
        /// <param name="value">파싱된 정수 값입니다.</param>
        /// <returns>정수 파싱에 성공하면 true입니다.</returns>
        private static bool TryGetIntArg(Dictionary<string, string> dict, string key, out int value)
        {
            value = 0;
            var v = GetArg(dict, key);
            if (string.IsNullOrWhiteSpace(v)) return false;
            return int.TryParse(v, out value);
        }
    }
}
