using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 플레이어 공격력/방어력/스태미나와 마지막 최종 데미지를 HUD에 출력하는 Provider입니다.
    /// </summary>
    [DebugHudProvider(350)]
    public sealed class PlayerStatHud : IDebugHudProvider
    {
        private readonly StringBuilder _builder = new(512);
        private bool _hasSnapshot;

        /// <summary>
        /// 현재 설정에서 플레이어 스탯 HUD가 활성화되어야 하는지 확인합니다.
        /// </summary>
        public bool IsEnabled(GGemCoSettings settings)
        {
            if (!GGemCoBuildFlags.AllowDebugFeatures || settings == null || !settings.EnableDebugHud)
                return false;

            GGemCoPlayerStatSettings statSettings = GetPlayerStatSettings();
            return statSettings != null && statSettings.EnablePlayerStatDebugHud;
        }

        /// <summary>
        /// HUD 갱신 주기를 반환합니다.
        /// </summary>
        public float GetUpdateInterval(GGemCoSettings settings)
        {
            GGemCoPlayerStatSettings statSettings = GetPlayerStatSettings();
            return statSettings != null ? Mathf.Max(0.05f, statSettings.playerStatDebugHudUpdateInterval) : 0.2f;
        }

        /// <summary>
        /// 내부 문자열 캐시를 초기화합니다.
        /// </summary>
        public void Reset()
        {
            _builder.Clear();
            _hasSnapshot = false;
        }

        /// <summary>
        /// 현재 플레이어와 계산 매니저에서 디버그 스냅샷을 수집합니다.
        /// </summary>
        public void Tick(float elapsedSeconds)
        {
            _builder.Clear();
            _hasSnapshot = false;

            GGemCoPlayerStatSettings statSettings = GetPlayerStatSettings();
            Player player = GetPlayer();
            if (statSettings == null || player == null)
                return;

            CharacterStatDebugCollector.Snapshot snapshot = CharacterStatDebugCollector.BuildSnapshot(player);
            _builder.AppendLine("[Player Stat]");
            AppendStatLine(snapshot.Atk, statSettings);
            AppendStatLine(snapshot.Def, statSettings);
            AppendStatLine(snapshot.Stamina, statSettings);

            if (statSettings.EnableFormulaVariableDebug)
            {
                AppendFormulaVariableLines("[Formula Variables]", snapshot.FormulaVariables, statSettings.EnableFormulaVariableContributionDebug);
            }

            if (statSettings.EnablePlayerFinalDamageDebug)
            {
                AppendDamageLine(statSettings);
            }

            _hasSnapshot = _builder.Length > 0;
        }

        /// <summary>
        /// 현재 Provider 내용을 최종 HUD 문자열에 추가합니다.
        /// </summary>
        public bool TryBuildContent(StringBuilder builder)
        {
            if (!_hasSnapshot || _builder.Length <= 0)
                return false;

            builder.Append(_builder);
            return true;
        }

        /// <summary>
        /// 단일 스탯 항목을 HUD 문자열로 추가합니다.
        /// </summary>
        private void AppendStatLine(CharacterStatDebugCollector.StatLine line, GGemCoPlayerStatSettings settings)
        {
            _builder.Append(line.DisplayName)
                .Append(" Base:").Append(line.BaseStart)
                .Append(" -> ").Append(line.BaseTotal)
                .Append(" / Stat:").Append(line.StatStart)
                .Append(" -> ").Append(line.StatTotal)
                .Append(" / Final:").AppendLine(line.FinalValue.ToString());

            if (!settings.EnablePlayerStatContributionDebug)
                return;

            _builder.Append("  Item:").Append(FormatSigned(line.ItemContribution))
                .Append(" Skill:").Append(FormatSigned(line.SkillContribution))
                .Append(" Affect:").AppendLine(FormatSigned(line.AffectContribution));
        }

        /// <summary>
        /// 마지막 데미지 계산 결과를 HUD 문자열로 추가합니다.
        /// </summary>
        private void AppendDamageLine(GGemCoPlayerStatSettings settings)
        {
            CalculateManager manager = CalculateManager.GetActive();
            if (manager == null || !manager.TryGetLastDamageDebugSnapshot(out DamageCalculationDebugSnapshot damage))
            {
                _builder.AppendLine("Damage Final: -");
                return;
            }

            _builder.Append("Damage Final: ").Append(damage.FinalDamage)
                .Append(" Raw:").Append(damage.RawDamage)
                .Append(" Type:").Append(damage.DamageType)
                .Append(" Formula:").Append(string.IsNullOrEmpty(damage.FormulaKey) ? "-" : damage.FormulaKey);

            if (damage.AppliedDefaultDamage)
                _builder.Append(" Default");
            if (damage.IsImmune)
                _builder.Append(" Immune");

            _builder.AppendLine();

            if (settings != null && settings.EnableLastDamageFormulaVariableDebug)
            {
                AppendFormulaVariableLines("Used Variables", damage.UsedFormulaVariables, settings.EnableFormulaVariableContributionDebug);
            }
        }

        /// <summary>
        /// 공식 변수 목록을 HUD 문자열로 추가합니다.
        /// </summary>
        /// <param name="title">섹션 제목입니다.</param>
        /// <param name="lines">출력할 공식 변수 목록입니다.</param>
        /// <param name="showContributions">출처별 기여도를 함께 표시할지 여부입니다.</param>
        private void AppendFormulaVariableLines(
            string title,
            IReadOnlyList<DamageFormulaVariableDebugLine> lines,
            bool showContributions)
        {
            if (lines == null || lines.Count == 0)
                return;

            _builder.AppendLine(title);
            for (int i = 0; i < lines.Count; i++)
            {
                DamageFormulaVariableDebugLine line = lines[i];
                if (string.IsNullOrWhiteSpace(line.VariableKey))
                    continue;

                _builder.Append("  ")
                    .Append(line.VariableKey)
                    .Append(" Final:")
                    .AppendLine(FormatDouble(line.FinalValue));

                if (!showContributions)
                    continue;

                _builder.Append("    Item:").Append(FormatSigned(line.ItemValue))
                    .Append(" Skill:").Append(FormatSigned(line.SkillValue))
                    .Append(" Affect:").AppendLine(FormatSigned(line.AffectValue));
            }
        }

        /// <summary>
        /// 부호가 포함된 정수 문자열을 반환합니다.
        /// </summary>
        private static string FormatSigned(long value)
        {
            return value > 0 ? "+" + value : value.ToString();
        }

        /// <summary>
        /// 부호가 포함된 실수 문자열을 반환합니다.
        /// </summary>
        private static string FormatSigned(double value)
        {
            return value > 0d ? "+" + FormatDouble(value) : FormatDouble(value);
        }

        /// <summary>
        /// HUD 표시용 실수 문자열을 반환합니다.
        /// </summary>
        private static string FormatDouble(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                return "0";

            return System.Math.Abs(value % 1d) < 0.0001d
                ? ((long)System.Math.Round(value)).ToString()
                : value.ToString("0.###");
        }

        /// <summary>
        /// 현재 게임 씬의 플레이어 컴포넌트를 조회합니다.
        /// </summary>
        private static Player GetPlayer()
        {
            SceneGame sceneGame = SceneGame.Instance;
            if (sceneGame == null || sceneGame.player == null)
                return null;

            return sceneGame.player.GetComponent<Player>();
        }

        /// <summary>
        /// 로드된 플레이어 스탯 설정 에셋을 조회합니다.
        /// </summary>
        private static GGemCoPlayerStatSettings GetPlayerStatSettings()
        {
            if (AddressableLoaderSettings.Instance != null && AddressableLoaderSettings.Instance.playerStatSettings != null)
                return AddressableLoaderSettings.Instance.playerStatSettings;

            return AddressableLoaderSettingsRegist.Instance != null
                ? AddressableLoaderSettingsRegist.Instance.playerStatSettings
                : null;
        }
    }
}
