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
    /// 대사 그래프 원문을 Unity Localization String Table Collection 으로 내보내는 서비스입니다.
    /// </summary>
    public sealed class DialogueLocalizationExportService
    {
        private const string LocalizationOutputDirectory = "Assets/Localization/Dialogue";
        private const string CollectionNamePrefix = "GGemCo_Dialogue";

        /// <summary>
        /// 지정한 대사 그래프를 로컬라이제이션 컬렉션으로 내보냅니다.
        /// 동일한 컬렉션이 이미 존재하면 신규 생성하지 않고 갱신합니다.
        /// </summary>
        /// <param name="dialogueInfo">대사 메타 정보입니다.</param>
        /// <param name="nodes">에디터에 작성된 노드 목록입니다.</param>
        /// <returns>내보내기에 사용된 컬렉션 이름입니다.</returns>
        public string Export(StruckTableDialogue dialogueInfo, IReadOnlyList<DialogueNode> nodes)
        {
            if (dialogueInfo == null)
            {
                throw new ArgumentNullException(nameof(dialogueInfo));
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
            string collectionName = BuildCollectionName(dialogueInfo.Uid);
            StringTableCollection collection = HelperLocalization.EnsureStringTableCollection(collectionName, LocalizationOutputDirectory);

            if (nodes != null)
            {
                foreach (DialogueNode node in nodes)
                {
                    if (node == null || string.IsNullOrWhiteSpace(node.guid))
                    {
                        continue;
                    }

                    string bodyKey = BuildNodeTextKey(dialogueInfo.Uid, node.guid);
                    UpsertEntry(collection, locales, sourceLocale, bodyKey, node.dialogueText);

                    if (node.options == null)
                    {
                        continue;
                    }

                    for (int optionIndex = 0; optionIndex < node.options.Count; optionIndex++)
                    {
                        DialogueOption option = node.options[optionIndex];
                        if (option == null)
                        {
                            continue;
                        }

                        string optionKey = BuildOptionTextKey(dialogueInfo.Uid, node.guid, optionIndex);
                        UpsertEntry(collection, locales, sourceLocale, optionKey, option.optionText);
                    }
                }
            }

            EditorUtility.SetDirty(collection.SharedData);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return collectionName;
        }

        /// <summary>
        /// 지정한 대사 Uid 기준으로 String Table Collection 이름을 생성합니다.
        /// </summary>
        /// <param name="dialogueUid">대사 고유번호입니다.</param>
        /// <returns>컬렉션 이름입니다.</returns>
        public static string BuildCollectionName(int dialogueUid)
        {
            return $"{CollectionNamePrefix}_{dialogueUid}";
        }

        /// <summary>
        /// 노드 본문용 로컬라이즈 키를 생성합니다.
        /// </summary>
        /// <param name="dialogueUid">대사 고유번호입니다.</param>
        /// <param name="nodeGuid">노드 GUID 입니다.</param>
        /// <returns>본문 키입니다.</returns>
        public static string BuildNodeTextKey(int dialogueUid, string nodeGuid)
        {
            return $"dlg_{dialogueUid}_{SanitizeToken(nodeGuid)}_body";
        }

        /// <summary>
        /// 선택지 본문용 로컬라이즈 키를 생성합니다.
        /// </summary>
        /// <param name="dialogueUid">대사 고유번호입니다.</param>
        /// <param name="nodeGuid">부모 노드 GUID 입니다.</param>
        /// <param name="optionIndex">선택지 인덱스입니다.</param>
        /// <returns>선택지 키입니다.</returns>
        public static string BuildOptionTextKey(int dialogueUid, string nodeGuid, int optionIndex)
        {
            return $"dlg_{dialogueUid}_{SanitizeToken(nodeGuid)}_opt_{optionIndex}";
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
        /// 프로젝트 기준 원문 Locale 을 결정합니다.
        /// Project Locale 이 없으면 첫 번째 Locale 을 사용합니다.
        /// </summary>
        /// <param name="locales">현재 프로젝트 Locale 목록입니다.</param>
        /// <returns>원문 Locale 입니다.</returns>
        private static Locale ResolveSourceLocale(IReadOnlyList<Locale> locales)
        {
            Locale projectLocale = LocalizationSettings.ProjectLocale;
            return projectLocale != null ? projectLocale : locales[0];
        }

        /// <summary>
        /// SharedTableEntry 와 Locale 별 StringTableEntry 를 갱신하거나 생성합니다.
        /// 원문 Locale 은 항상 최신 원문으로 덮어쓰고, 다른 Locale 은 기존 번역을 유지합니다.
        /// 단, 다른 Locale 에 엔트리가 아직 없으면 원문으로 초기값을 채웁니다.
        /// </summary>
        /// <param name="collection">대상 컬렉션입니다.</param>
        /// <param name="locales">프로젝트 Locale 목록입니다.</param>
        /// <param name="sourceLocale">원문 Locale 입니다.</param>
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
        /// 두 Locale 이 동일한지 Code 기준으로 비교합니다.
        /// </summary>
        /// <param name="a">비교 대상 Locale 입니다.</param>
        /// <param name="b">비교 대상 Locale 입니다.</param>
        /// <returns>동일하면 true 입니다.</returns>
        private static bool IsSameLocale(Locale a, Locale b)
        {
            if (a == null || b == null)
            {
                return false;
            }

            return string.Equals(a.Identifier.Code, b.Identifier.Code, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// GUID 기반 토큰을 Localization Key 에 안전한 문자열로 정규화합니다.
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

            return normalized.Trim('_');
        }
    }
}
