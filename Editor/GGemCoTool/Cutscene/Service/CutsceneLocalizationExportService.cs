using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GGemCo2DCore;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// 컷신 말풍선 원문을 Unity Localization String Table Collection 으로 내보내는 서비스입니다.
    /// </summary>
    internal sealed class CutsceneLocalizationExportService
    {
        private const string LocalizationOutputDirectory = "Assets/Localization/Cutscene";
        private const string CollectionNamePrefix = "GGemCo_Cutscene";

        /// <summary>
        /// 지정한 컷신 정보를 기준으로 DialogueBalloon 이벤트 메시지를 Localization 컬렉션으로 내보냅니다.
        /// </summary>
        /// <param name="cutsceneInfo">cutscene.txt의 컷신 메타 정보입니다.</param>
        /// <param name="events">Timeline에서 수집된 컷신 이벤트 목록입니다.</param>
        /// <returns>갱신된 컬렉션 이름입니다. 내보낼 말풍선 메시지가 없으면 빈 문자열입니다.</returns>
        public string Export(StruckTableCutscene cutsceneInfo, IReadOnlyList<CutsceneEvent> events)
        {
            string cutsceneToken = cutsceneInfo != null && cutsceneInfo.Uid > 0
                ? cutsceneInfo.Uid.ToString()
                : cutsceneInfo?.FileName;

            return Export(cutsceneToken, events);
        }

        /// <summary>
        /// 지정한 컷신 토큰을 기준으로 DialogueBalloon 이벤트 메시지를 Localization 컬렉션으로 내보냅니다.
        /// </summary>
        /// <param name="cutsceneToken">컬렉션 이름과 키에 사용할 컷신 식별 토큰입니다.</param>
        /// <param name="events">Timeline에서 수집된 컷신 이벤트 목록입니다.</param>
        /// <returns>갱신된 컬렉션 이름입니다. 내보낼 말풍선 메시지가 없으면 빈 문자열입니다.</returns>
        public string Export(string cutsceneToken, IReadOnlyList<CutsceneEvent> events)
        {
            List<CutsceneEvent> dialogueBalloonEvents = CollectDialogueBalloonEvents(events);
            if (dialogueBalloonEvents.Count == 0)
            {
                return string.Empty;
            }

            string safeCutsceneToken = SanitizeToken(cutsceneToken);
            if (string.IsNullOrWhiteSpace(safeCutsceneToken) || safeCutsceneToken == "empty")
            {
                throw new InvalidOperationException("컷신 Localization 컬렉션을 만들 컷신 식별자가 없습니다.");
            }

            EnsureOutputDirectory();

            List<Locale> locales = LocalizationEditorSettings.GetLocales()
                .Where(locale => locale != null)
                .ToList();
            if (locales.Count == 0)
            {
                throw new InvalidOperationException("Localization Locale 이 설정되어 있지 않습니다.");
            }

            Locale sourceLocale = ResolveSourceLocale(locales);
            string collectionName = BuildCollectionName(safeCutsceneToken);
            StringTableCollection collection = HelperLocalization.EnsureStringTableCollection(collectionName, LocalizationOutputDirectory);

            foreach (CutsceneEvent cutsceneEvent in dialogueBalloonEvents)
            {
                cutsceneEvent.EnsureEventGuid();
                DialogueBalloonData data = cutsceneEvent.dialogueBalloon;
                string messageKey = BuildDialogueBalloonMessageKey(safeCutsceneToken, cutsceneEvent.eventGuid);

                UpsertEntry(collection, locales, sourceLocale, messageKey, data.message);
                data.messageTable = collectionName;
                data.messageKey = messageKey;
            }

            EditorUtility.SetDirty(collection.SharedData);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return collectionName;
        }

        /// <summary>
        /// 지정한 컷신 Uid 기준으로 String Table Collection 이름을 생성합니다.
        /// </summary>
        /// <param name="cutsceneUid">컷신 고유번호입니다.</param>
        /// <returns>컬렉션 이름입니다.</returns>
        public static string BuildCollectionName(int cutsceneUid)
        {
            return BuildCollectionName(cutsceneUid.ToString());
        }

        /// <summary>
        /// 지정한 컷신 토큰 기준으로 String Table Collection 이름을 생성합니다.
        /// </summary>
        /// <param name="cutsceneToken">컷신 식별 토큰입니다.</param>
        /// <returns>컬렉션 이름입니다.</returns>
        public static string BuildCollectionName(string cutsceneToken)
        {
            return $"{CollectionNamePrefix}_{SanitizeToken(cutsceneToken)}";
        }

        /// <summary>
        /// 말풍선 메시지용 Localization Key를 생성합니다.
        /// </summary>
        /// <param name="cutsceneUid">컷신 고유번호입니다.</param>
        /// <param name="eventGuid">컷신 이벤트 GUID입니다.</param>
        /// <returns>말풍선 메시지 키입니다.</returns>
        public static string BuildDialogueBalloonMessageKey(int cutsceneUid, string eventGuid)
        {
            return BuildDialogueBalloonMessageKey(cutsceneUid.ToString(), eventGuid);
        }

        /// <summary>
        /// 말풍선 메시지용 Localization Key를 생성합니다.
        /// </summary>
        /// <param name="cutsceneToken">컷신 식별 토큰입니다.</param>
        /// <param name="eventGuid">컷신 이벤트 GUID입니다.</param>
        /// <returns>말풍선 메시지 키입니다.</returns>
        public static string BuildDialogueBalloonMessageKey(string cutsceneToken, string eventGuid)
        {
            return $"cutscene_{SanitizeToken(cutsceneToken)}_balloon_{SanitizeToken(eventGuid)}_message";
        }

        /// <summary>
        /// Localization으로 내보낼 DialogueBalloon 이벤트를 수집합니다.
        /// 메시지 원문이 비어 있는 이벤트는 기존 데이터 호환을 위해 건너뜁니다.
        /// </summary>
        /// <param name="events">검사할 컷신 이벤트 목록입니다.</param>
        /// <returns>Localization Export 대상 말풍선 이벤트 목록입니다.</returns>
        private static List<CutsceneEvent> CollectDialogueBalloonEvents(IReadOnlyList<CutsceneEvent> events)
        {
            var result = new List<CutsceneEvent>();
            if (events == null)
            {
                return result;
            }

            foreach (CutsceneEvent cutsceneEvent in events)
            {
                if (cutsceneEvent == null || cutsceneEvent.type != CutsceneEventType.DialogueBalloon)
                {
                    continue;
                }

                DialogueBalloonData data = cutsceneEvent.dialogueBalloon;
                if (data == null || string.IsNullOrWhiteSpace(data.message))
                {
                    continue;
                }

                result.Add(cutsceneEvent);
            }

            return result;
        }

        /// <summary>
        /// 로컬라이제이션 에셋 출력 폴더를 보장합니다.
        /// </summary>
        private static void EnsureOutputDirectory()
        {
            if (Directory.Exists(LocalizationOutputDirectory))
            {
                return;
            }

            Directory.CreateDirectory(LocalizationOutputDirectory);
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// 프로젝트 기준 원문 Locale을 결정합니다.
        /// Project Locale이 없으면 첫 번째 Locale을 사용합니다.
        /// </summary>
        /// <param name="locales">현재 프로젝트 Locale 목록입니다.</param>
        /// <returns>원문 Locale입니다.</returns>
        private static Locale ResolveSourceLocale(IReadOnlyList<Locale> locales)
        {
            Locale projectLocale = LocalizationSettings.ProjectLocale;
            return projectLocale != null ? projectLocale : locales[0];
        }

        /// <summary>
        /// SharedTableEntry와 Locale별 StringTableEntry를 갱신하거나 생성합니다.
        /// 원문 Locale은 항상 최신 원문으로 덮어쓰고, 다른 Locale은 기존 번역을 유지합니다.
        /// 단, 다른 Locale에 엔트리가 아직 없으면 원문으로 초기값을 채웁니다.
        /// </summary>
        /// <param name="collection">대상 컬렉션입니다.</param>
        /// <param name="locales">프로젝트 Locale 목록입니다.</param>
        /// <param name="sourceLocale">원문 Locale입니다.</param>
        /// <param name="key">엔트리 키입니다.</param>
        /// <param name="sourceText">원문 텍스트입니다.</param>
        private static void UpsertEntry(
            StringTableCollection collection,
            IReadOnlyList<Locale> locales,
            Locale sourceLocale,
            string key,
            string sourceText)
        {
            if (collection == null || string.IsNullOrWhiteSpace(key))
            {
                return;
            }

            SharedTableData sharedData = collection.SharedData;
            if (sharedData == null)
            {
                throw new InvalidOperationException($"SharedTableData 를 찾을 수 없습니다. collection: {collection.TableCollectionName}");
            }

            SharedTableData.SharedTableEntry sharedEntry = sharedData.GetEntry(key);
            if (sharedEntry == null)
            {
                sharedEntry = sharedData.AddKey(key);
                EditorUtility.SetDirty(sharedData);
            }

            foreach (Locale locale in locales)
            {
                StringTable table = HelperLocalization.EnsureLocaleTable(collection, locale);
                StringTableEntry entry = table.GetEntry(sharedEntry.Id);
                bool isSourceLocale = IsSameLocale(locale, sourceLocale);

                if (entry == null)
                {
                    table.AddEntry(sharedEntry.Id, sourceText ?? string.Empty);
                    entry = table.GetEntry(sharedEntry.Id);
                    if (entry != null)
                    {
                        entry.IsSmart = false;
                    }

                    EditorUtility.SetDirty(table);
                    continue;
                }

                if (!isSourceLocale)
                {
                    continue;
                }

                if (!string.Equals(entry.Value, sourceText ?? string.Empty, StringComparison.Ordinal) || entry.IsSmart)
                {
                    table.AddEntry(sharedEntry.Id, sourceText ?? string.Empty);
                    entry = table.GetEntry(sharedEntry.Id);
                    if (entry != null)
                    {
                        entry.IsSmart = false;
                    }

                    EditorUtility.SetDirty(table);
                }
            }
        }

        /// <summary>
        /// 두 Locale이 동일한지 Code 기준으로 비교합니다.
        /// </summary>
        /// <param name="a">비교 대상 Locale입니다.</param>
        /// <param name="b">비교 대상 Locale입니다.</param>
        /// <returns>동일하면 true입니다.</returns>
        private static bool IsSameLocale(Locale a, Locale b)
        {
            if (a == null || b == null)
            {
                return false;
            }

            return string.Equals(a.Identifier.Code, b.Identifier.Code, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// GUID와 파일명 토큰을 Localization Key에 안전한 문자열로 정규화합니다.
        /// </summary>
        /// <param name="value">원본 문자열입니다.</param>
        /// <returns>정규화된 토큰입니다.</returns>
        private static string SanitizeToken(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "empty";
            }

            char[] buffer = value.Trim().ToCharArray();
            for (int i = 0; i < buffer.Length; i++)
            {
                char c = buffer[i];
                buffer[i] = char.IsLetterOrDigit(c) ? char.ToLowerInvariant(c) : '_';
            }

            string normalized = new string(buffer);
            while (normalized.IndexOf("__", StringComparison.Ordinal) >= 0)
            {
                normalized = normalized.Replace("__", "_");
            }

            normalized = normalized.Trim('_');
            return string.IsNullOrWhiteSpace(normalized) ? "empty" : normalized;
        }
    }
}
