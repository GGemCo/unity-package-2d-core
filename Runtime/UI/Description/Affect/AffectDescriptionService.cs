using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using GGemCo2DAffect;

namespace GGemCo2DCore
{
    /// <summary>
    /// Affect(버프/디버프) 설명 문구를 Unity Localization Smart String 기반으로 생성하는 서비스.
    /// </summary>
    /// <remarks>
    /// - 기본은 AffectModifierDefinition 조합을 통해 자동 생성한다.
    /// - 특정 Affect에 대해 문장을 완전히 제어하고 싶다면, String Table에 오버라이드 키를 추가할 수 있다.
    ///   예) Affect_Desc_{AffectUid}
    /// - 결과는 Locale+Uid 기준으로 캐시한다.
    /// </remarks>
    public sealed class AffectDescriptionService
    {
        private static AffectDescriptionService _instance;

        public static AffectDescriptionService Instance => _instance ??= new AffectDescriptionService();

        private readonly Dictionary<CacheKey, string> _cache = new(256);
        private readonly StringBuilder _sb = new(256);

        private AffectDescriptionService() { }

        /// <summary>
        /// Locale 변경 등으로 캐시가 무효화되어야 할 때 호출합니다.
        /// </summary>
        public static void ClearCache()
        {
            // 서비스가 아직 사용되지 않았다면 인스턴스 생성 없이 종료한다.
            if (_instance == null)
                return;

            _instance._cache.Clear();
        }

        /// <summary>
        /// AffectUid에 대한 표시용 설명 문구를 생성합니다.
        /// </summary>
        /// <param name="affectUid">Affect 고유 ID</param>
        /// <returns>여러 줄 설명 문자열</returns>
        public string GetDescription(int affectUid)
        {
            if (affectUid <= 0) return string.Empty;

            var loc = LocalizationManager.Instance;
            if (loc == null) return string.Empty;

            var locale = loc.GetCurrentLanguageCode() ?? string.Empty;
            var key = new CacheKey(locale, affectUid);
            if (_cache.TryGetValue(key, out var cached))
                return cached;

            var tables = TableLoaderManager.Instance;
            if (tables == null) return string.Empty;

            var affectInfo = tables.TableAffect.GetDataByUid(affectUid);
            if (affectInfo == null) return string.Empty;

            // 1) 개별 Affect 오버라이드(선택)
            string overrideKey = $"Affect_Desc_{affectUid}";
            if (loc.HasAffectDescriptionLocalizationKey(overrideKey))
            {
                var overrideText = loc.GetAffectDescriptionByKey(overrideKey);
                if (!string.IsNullOrWhiteSpace(overrideText))
                {
                    _cache[key] = overrideText;
                    return overrideText;
                }
            }

            // 2) 자동 생성
            _sb.Clear();

            var modifiers = tables.TableAffectModifier.GetModifiers(affectUid);
            for (int i = 0; i < modifiers.Count; i++)
            {
                var mod = modifiers[i];
                string line = BuildLineFromModifier(tables, loc, affectInfo, mod);
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                if (_sb.Length > 0)
                    _sb.Append('\n');
                _sb.Append(line);
            }

            // 3) 메타 정보(지속/틱/스택 등) 라인
            AppendMetaLines(loc, affectInfo);

            var result = _sb.ToString();
            _cache[key] = result;
            return result;
        }

        /// <summary>
        /// 스킬/아이템처럼 '발동 확률'이 별도로 존재하는 경우를 위한 헬퍼.
        /// </summary>
        public string GetDescriptionWithChancePrefix(int affectUid, float chancePercent)
        {
            var loc = LocalizationManager.Instance;
            if (loc == null) return string.Empty;

            var desc = GetDescription(affectUid);
            if (string.IsNullOrWhiteSpace(desc)) return string.Empty;

            var args = new AffectChancePrefixArgs
            {
                ChancePercentText = FormatNumber(chancePercent)
            };

            // UIWindowSkillInfo의 Text_Affect 같은 키로도 처리할 수 있지만,
            // Affect 전용 테이블에서 공용 접두 문구를 관리하면 재사용성이 높다.
            var prefix = loc.GetAffectDescriptionSmart(AffectDescriptionKeys.PrefixChance, args);
            if (string.IsNullOrWhiteSpace(prefix))
                prefix = $"{chancePercent}%";

            return $"{prefix}\n{desc}";
        }

        
        private static string ResolveStatusName(LocalizationManager loc, string id, string fallback)
        {
            if (loc == null)
                return fallback ?? string.Empty;

            if (string.IsNullOrWhiteSpace(id))
                return fallback ?? string.Empty;

            var localized = loc.GetStatusNameByKey(id);
            if (!string.IsNullOrWhiteSpace(localized))
                return localized;

            // 로컬라이즈 테이블에 엔트리가 없으면, 테이블에 캐시된 Name 또는 id 자체를 사용한다.
            return !string.IsNullOrWhiteSpace(fallback) ? fallback : id;
        }

        private string BuildLineFromModifier(TableLoaderManager tables, LocalizationManager loc, StruckTableAffect affectInfo, AffectModifierDefinition mod)
        {
            switch (mod.kind)
            {
                case ModifierKind.Stat:
                {
                    var stat = tables.TableStat.GetDataById(mod.statId);
                    string statName = ResolveStatusName(loc, mod.statId, stat?.Name);
                    var args = new AffectStatLineArgs
                    {
                        StatName = statName,
                        Operation = mod.statOperation.ToString(),
                        ValueType = mod.statValueType.ToString(),
                        ValueText = FormatNumber(mod.statValue)
                    };
                    return loc.GetAffectDescriptionSmart(AffectDescriptionKeys.LineStat, args);
                }

                case ModifierKind.Damage:
                {
                    var dmg = tables.TableDamageType.GetDataById(mod.damageTypeId);
                    string damageTypeName = ResolveStatusName(loc, mod.damageTypeId, dmg?.Name);

                    var scalingStat = tables.TableStat.GetDataById(mod.scalingStatId);
                    string scalingStatName = ResolveStatusName(loc, mod.scalingStatId, scalingStat?.Name);

                    var args = new AffectDamageLineArgs
                    {
                        DamageTypeName = damageTypeName,
                        BaseValueText = FormatNumber(mod.damageBaseValue),
                        HasScaling = !string.IsNullOrWhiteSpace(mod.scalingStatId) && Math.Abs(mod.scalingCoefficient) > 0f,
                        ScalingStatName = scalingStatName,
                        ScalingCoefText = FormatNumber(mod.scalingCoefficient),
                        CanCrit = mod.canCrit,
                        IsDot = mod.isDot
                    };
                    return loc.GetAffectDescriptionSmart(AffectDescriptionKeys.LineDamage, args);
                }

                case ModifierKind.State:
                {
                    var state = tables.TableState.GetDataById(mod.stateId);
                    string stateName = ResolveStatusName(loc, mod.stateId, state?.Name);

                    float duration = mod.stateDurationOverride > 0f ? mod.stateDurationOverride : affectInfo.BaseDuration;
                    var args = new AffectStateLineArgs
                    {
                        StateName = stateName,
                        ChancePercentText = FormatNumber(mod.stateChance),
                        DurationSecondsText = duration > 0f ? FormatNumber(duration) : string.Empty,
                        HasDuration = duration > 0f
                    };
                    return loc.GetAffectDescriptionSmart(AffectDescriptionKeys.LineState, args);
                }
            }

            return string.Empty;
        }

        private void AppendMetaLines(LocalizationManager loc, StruckTableAffect affectInfo)
        {
            // 지속시간
            if (affectInfo.BaseDuration > 0f)
            {
                var args = new AffectDurationArgs { SecondsText = FormatNumber(affectInfo.BaseDuration) };
                AppendLineIfAny(loc.GetAffectDescriptionSmart(AffectDescriptionKeys.InfoDuration, args));
            }

            // Tick
            if (affectInfo.TickInterval > 0f)
            {
                var args = new AffectTickArgs { SecondsText = FormatNumber(affectInfo.TickInterval) };
                AppendLineIfAny(loc.GetAffectDescriptionSmart(AffectDescriptionKeys.InfoTick, args));
            }

            // 스택
            if (affectInfo.StackPolicy != StackPolicy.None)
            {
                var args = new AffectStackArgs
                {
                    StackPolicy = ResolveStackPolicyName(affectInfo.StackPolicy),
                    MaxStacksText = affectInfo.MaxStacks > 0 ? affectInfo.MaxStacks.ToString(CultureInfo.InvariantCulture) : string.Empty,
                    HasMaxStacks = affectInfo.MaxStacks > 0
                };
                AppendLineIfAny(loc.GetAffectDescriptionSmart(AffectDescriptionKeys.InfoStack, args));
            }
        }

        private void AppendLineIfAny(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return;
            if (_sb.Length > 0) _sb.Append('\n');
            _sb.Append(line);
        }

        private static string FormatNumber(float value)
        {
            // 표시 텍스트는 Culture 영향을 받지 않도록 invariant로 고정한다.
            // 필요하면 프로젝트 규칙(소수점 자리수)로 조정 가능.
            return value.ToString("0.##", CultureInfo.InvariantCulture);
        }

        private readonly struct CacheKey : IEquatable<CacheKey>
        {
            private readonly string _locale;
            private readonly int _uid;

            public CacheKey(string locale, int uid)
            {
                _locale = locale ?? string.Empty;
                _uid = uid;
            }

            public bool Equals(CacheKey other) => _uid == other._uid && string.Equals(_locale, other._locale, StringComparison.Ordinal);
            public override bool Equals(object obj) => obj is CacheKey other && Equals(other);
            public override int GetHashCode() => HashCode.Combine(_locale, _uid);
        }
        private static string ResolveStackPolicyName(StackPolicy policy)
        {
            var loc = LocalizationManager.Instance;
            if (loc == null)
                return policy.ToString();

            var localized = loc.GetStackPolicyName(policy);
            if (!string.IsNullOrEmpty(localized))
                return localized;

            return policy.ToString(); // fallback
        }
    }

    /// <summary>
    /// AffectDescription String Table 키 정의.
    /// </summary>
    public static class AffectDescriptionKeys
    {
        public const string PrefixChance = "Affect_Prefix_Chance";

        public const string LineStat = "Affect_Line_Stat";
        public const string LineDamage = "Affect_Line_Damage";
        public const string LineState = "Affect_Line_State";

        public const string InfoDuration = "Affect_Info_Duration";
        public const string InfoTick = "Affect_Info_Tick";
        public const string InfoStack = "Affect_Info_Stack";
    }

    // ----------------------------
    // Smart String argument models
    // ----------------------------

    public sealed class AffectChancePrefixArgs
    {
        public string ChancePercentText { get; set; }
    }

    public sealed class AffectStatLineArgs
    {
        public string StatName { get; set; }
        public string Operation { get; set; } // Add/Multiply/Override...
        public string ValueType { get; set; }  // Flat/Percent
        public string ValueText { get; set; }
    }

    public sealed class AffectDamageLineArgs
    {
        public string DamageTypeName { get; set; }
        public string BaseValueText { get; set; }
        public bool HasScaling { get; set; }
        public string ScalingStatName { get; set; }
        public string ScalingCoefText { get; set; }
        public bool CanCrit { get; set; }
        public bool IsDot { get; set; }
    }

    public sealed class AffectStateLineArgs
    {
        public string StateName { get; set; }
        public string ChancePercentText { get; set; }
        public bool HasDuration { get; set; }
        public string DurationSecondsText { get; set; }
    }

    public sealed class AffectDurationArgs
    {
        public string SecondsText { get; set; }
    }

    public sealed class AffectTickArgs
    {
        public string SecondsText { get; set; }
    }

    public sealed class AffectStackArgs
    {
        public string StackPolicy { get; set; } // None/Refresh/Add/Independent
        public bool HasMaxStacks { get; set; }
        public string MaxStacksText { get; set; }
    }
}
