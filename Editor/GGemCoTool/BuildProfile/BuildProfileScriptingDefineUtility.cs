using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using GGemCo2DCore;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// Build Profile에서 사용하는 Scripting Define Symbol 조회/변경/검증 기능을 제공합니다.
    /// Unity 6의 NamedBuildTarget 기반 API를 사용하여 현재 선택된 빌드 타겟의 심볼을 관리합니다.
    /// </summary>
    public static class BuildProfileScriptingDefineUtility
    {
        private static readonly char[] DefineSeparators = { ';', ',', ' ', '\n', '\r', '\t' };

        /// <summary>
        /// 현재 에디터에서 선택된 빌드 타겟 그룹을 NamedBuildTarget으로 변환합니다.
        /// </summary>
        /// <returns>현재 선택된 빌드 타겟에 대응되는 NamedBuildTarget입니다.</returns>
        public static NamedBuildTarget GetActiveNamedBuildTarget()
        {
            return NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
        }

        /// <summary>
        /// 현재 선택된 빌드 타겟 그룹 이름을 반환합니다.
        /// </summary>
        /// <returns>현재 빌드 타겟 그룹 표시 문자열입니다.</returns>
        public static string GetActiveBuildTargetGroupName()
        {
            return EditorUserBuildSettings.selectedBuildTargetGroup.ToString();
        }

        /// <summary>
        /// 현재 선택된 빌드 타겟에 지정한 심볼이 등록되어 있는지 확인합니다.
        /// </summary>
        /// <param name="symbol">확인할 Scripting Define Symbol입니다.</param>
        /// <returns>심볼이 등록되어 있으면 true입니다.</returns>
        public static bool HasSymbolInActiveTarget(string symbol)
        {
            return HasSymbol(GetActiveNamedBuildTarget(), symbol);
        }

        /// <summary>
        /// 지정한 빌드 타겟에 지정한 심볼이 등록되어 있는지 확인합니다.
        /// </summary>
        /// <param name="buildTarget">확인할 NamedBuildTarget입니다.</param>
        /// <param name="symbol">확인할 Scripting Define Symbol입니다.</param>
        /// <returns>심볼이 등록되어 있으면 true입니다.</returns>
        public static bool HasSymbol(NamedBuildTarget buildTarget, string symbol)
        {
            if (string.IsNullOrWhiteSpace(symbol))
                return false;

            return GetSymbols(buildTarget).Contains(symbol.Trim());
        }

        /// <summary>
        /// 현재 선택된 빌드 타겟에서 지정한 심볼의 활성 상태를 변경합니다.
        /// </summary>
        /// <param name="symbol">변경할 Scripting Define Symbol입니다.</param>
        /// <param name="enabled">등록하려면 true, 제거하려면 false입니다.</param>
        /// <returns>실제 심볼 목록이 변경되었으면 true입니다.</returns>
        public static bool SetSymbolEnabledForActiveTarget(string symbol, bool enabled)
        {
            return SetSymbolEnabled(GetActiveNamedBuildTarget(), symbol, enabled);
        }

        /// <summary>
        /// 지정한 빌드 타겟에서 심볼의 활성 상태를 변경합니다.
        /// </summary>
        /// <param name="buildTarget">변경할 NamedBuildTarget입니다.</param>
        /// <param name="symbol">변경할 Scripting Define Symbol입니다.</param>
        /// <param name="enabled">등록하려면 true, 제거하려면 false입니다.</param>
        /// <returns>실제 심볼 목록이 변경되었으면 true입니다.</returns>
        public static bool SetSymbolEnabled(NamedBuildTarget buildTarget, string symbol, bool enabled)
        {
            if (string.IsNullOrWhiteSpace(symbol))
                return false;

            string normalizedSymbol = symbol.Trim();
            List<string> symbols = GetSymbols(buildTarget);
            bool contains = symbols.Contains(normalizedSymbol);

            if (enabled && contains)
                return false;

            if (!enabled && !contains)
                return false;

            if (enabled)
            {
                symbols.Add(normalizedSymbol);
            }
            else
            {
                symbols.RemoveAll(value => string.Equals(value, normalizedSymbol, StringComparison.Ordinal));
            }

            PlayerSettings.SetScriptingDefineSymbols(buildTarget, NormalizeSymbols(symbols).ToArray());
            return true;
        }

        /// <summary>
        /// 현재 선택된 빌드 타겟의 치트 도구 심볼 활성 상태를 변경합니다.
        /// </summary>
        /// <param name="enabled">치트 도구 코드를 컴파일에 포함하려면 true입니다.</param>
        /// <returns>실제 심볼 목록이 변경되었으면 true입니다.</returns>
        public static bool SetCheatToolsEnabledForActiveTarget(bool enabled)
        {
            return SetSymbolEnabledForActiveTarget(GGemCoScriptingDefineSymbols.EnableCheatTools, enabled);
        }

        /// <summary>
        /// 현재 선택된 빌드 타겟에 치트 도구 심볼이 등록되어 있는지 확인합니다.
        /// </summary>
        /// <returns>치트 도구 심볼이 등록되어 있으면 true입니다.</returns>
        public static bool HasCheatToolsSymbolInActiveTarget()
        {
            return HasSymbolInActiveTarget(GGemCoScriptingDefineSymbols.EnableCheatTools);
        }

        /// <summary>
        /// 릴리즈 빌드에서 금지된 Scripting Define Symbol이 등록되어 있는지 검사합니다.
        /// </summary>
        /// <param name="buildTargetGroup">검사할 빌드 타겟 그룹입니다.</param>
        /// <returns>릴리즈 빌드를 차단해야 하는 심볼 검출 결과입니다.</returns>
        public static List<ScriptingDefineRiskEntry> FindReleaseBlockingSymbols(BuildTargetGroup buildTargetGroup)
        {
            List<ScriptingDefineRiskEntry> results = new List<ScriptingDefineRiskEntry>();

            NamedBuildTarget namedBuildTarget = NamedBuildTarget.FromBuildTargetGroup(buildTargetGroup);
            if (HasSymbol(namedBuildTarget, GGemCoScriptingDefineSymbols.EnableCheatTools))
            {
                results.Add(new ScriptingDefineRiskEntry(
                    GGemCoScriptingDefineSymbols.EnableCheatTools,
                    buildTargetGroup.ToString(),
                    "Player Settings Scripting Define Symbols",
                    "릴리즈 빌드에 치트 도구 코드가 컴파일될 수 있습니다."));
            }

            CollectResponseFileRisks(results);
            return results
                .OrderBy(entry => entry.Source)
                .ThenBy(entry => entry.Symbol)
                .ToList();
        }

        /// <summary>
        /// 릴리즈 검증 실패 메시지에 사용할 금지 심볼 요약을 생성합니다.
        /// </summary>
        /// <param name="entries">검출된 금지 심볼 목록입니다.</param>
        /// <returns>줄바꿈이 포함된 요약 메시지입니다.</returns>
        public static string BuildSummaryMessage(IReadOnlyList<ScriptingDefineRiskEntry> entries)
        {
            if (entries == null || entries.Count == 0)
            {
                return "릴리즈 빌드를 차단하는 Scripting Define Symbol이 없습니다.";
            }

            StringBuilder builder = new StringBuilder();
            builder.AppendLine($"릴리즈 빌드 금지 Scripting Define Symbol {entries.Count}건을 찾았습니다.");

            foreach (ScriptingDefineRiskEntry entry in entries)
            {
                builder.Append("- ")
                    .Append(entry.Symbol)
                    .Append(" | target=")
                    .Append(entry.Target)
                    .Append(" | source=")
                    .Append(entry.Source);

                if (!string.IsNullOrWhiteSpace(entry.Reason))
                {
                    builder.Append(" | ").Append(entry.Reason);
                }

                builder.AppendLine();
            }

            return builder.ToString();
        }

        /// <summary>
        /// 지정한 빌드 타겟의 심볼 목록을 정리된 리스트로 반환합니다.
        /// </summary>
        /// <param name="buildTarget">조회할 NamedBuildTarget입니다.</param>
        /// <returns>중복과 빈 값이 제거된 심볼 목록입니다.</returns>
        private static List<string> GetSymbols(NamedBuildTarget buildTarget)
        {
            string rawSymbols = PlayerSettings.GetScriptingDefineSymbols(buildTarget);
            return NormalizeSymbols(ParseSymbols(rawSymbols)).ToList();
        }

        /// <summary>
        /// Unity가 반환하는 세미콜론 구분 심볼 문자열을 개별 심볼로 분리합니다.
        /// </summary>
        /// <param name="rawSymbols">원본 심볼 문자열입니다.</param>
        /// <returns>분리된 심볼 목록입니다.</returns>
        private static IEnumerable<string> ParseSymbols(string rawSymbols)
        {
            if (string.IsNullOrWhiteSpace(rawSymbols))
                yield break;

            foreach (string token in rawSymbols.Split(DefineSeparators, StringSplitOptions.RemoveEmptyEntries))
            {
                string trimmed = token.Trim();
                if (!string.IsNullOrWhiteSpace(trimmed))
                    yield return trimmed;
            }
        }

        /// <summary>
        /// 심볼 목록에서 빈 값과 중복을 제거하되 기존 순서를 유지합니다.
        /// </summary>
        /// <param name="symbols">정리할 심볼 목록입니다.</param>
        /// <returns>정리된 심볼 목록입니다.</returns>
        private static IEnumerable<string> NormalizeSymbols(IEnumerable<string> symbols)
        {
            HashSet<string> uniqueSymbols = new HashSet<string>(StringComparer.Ordinal);
            foreach (string symbol in symbols)
            {
                string trimmed = symbol?.Trim();
                if (string.IsNullOrWhiteSpace(trimmed))
                    continue;

                if (uniqueSymbols.Add(trimmed))
                    yield return trimmed;
            }
        }

        /// <summary>
        /// csc.rsp 같은 응답 파일에 치트 도구 심볼이 직접 정의되어 있는지 검사합니다.
        /// </summary>
        /// <param name="results">검출 결과를 추가할 목록입니다.</param>
        private static void CollectResponseFileRisks(ICollection<ScriptingDefineRiskEntry> results)
        {
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            if (string.IsNullOrWhiteSpace(projectRoot))
                return;

            string[] responseFiles = Directory.GetFiles(Application.dataPath, "*.rsp", SearchOption.AllDirectories);
            foreach (string responseFile in responseFiles)
            {
                string content = File.ReadAllText(responseFile);
                if (!content.Contains(GGemCoScriptingDefineSymbols.EnableCheatTools))
                    continue;

                string relativePath = responseFile.Replace(projectRoot, string.Empty).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                results.Add(new ScriptingDefineRiskEntry(
                    GGemCoScriptingDefineSymbols.EnableCheatTools,
                    "Project",
                    relativePath.Replace('\\', '/'),
                    "응답 파일에 전역 심볼로 정의되어 모든 빌드에 적용될 수 있습니다."));
            }
        }

        /// <summary>
        /// 릴리즈 빌드를 차단해야 하는 Scripting Define Symbol 검출 결과입니다.
        /// </summary>
        public readonly struct ScriptingDefineRiskEntry
        {
            /// <summary>
            /// 검출된 Scripting Define Symbol 이름입니다.
            /// </summary>
            public readonly string Symbol;

            /// <summary>
            /// 심볼이 적용되는 빌드 타겟 또는 범위입니다.
            /// </summary>
            public readonly string Target;

            /// <summary>
            /// 심볼이 발견된 설정 위치입니다.
            /// </summary>
            public readonly string Source;

            /// <summary>
            /// 릴리즈 빌드를 차단해야 하는 이유입니다.
            /// </summary>
            public readonly string Reason;

            /// <summary>
            /// Scripting Define Symbol 검출 결과를 생성합니다.
            /// </summary>
            /// <param name="symbol">검출된 Scripting Define Symbol 이름입니다.</param>
            /// <param name="target">심볼이 적용되는 빌드 타겟 또는 범위입니다.</param>
            /// <param name="source">심볼이 발견된 설정 위치입니다.</param>
            /// <param name="reason">릴리즈 빌드를 차단해야 하는 이유입니다.</param>
            public ScriptingDefineRiskEntry(string symbol, string target, string source, string reason)
            {
                Symbol = symbol;
                Target = target;
                Source = source;
                Reason = reason;
            }
        }
    }
}
