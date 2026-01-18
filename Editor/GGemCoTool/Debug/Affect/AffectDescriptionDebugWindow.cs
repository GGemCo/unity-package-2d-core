
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GGemCo2DCore;
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization.Settings;

namespace GGemCo2DCoreEditor
{
    public sealed class AffectDescriptionDebugWindow : DefaultEditorWindow
    {
        private const string Title = "Affect 설명 체크기";
        private TableAffect _tableAffect;
        private int _selectedIndex;
        private Dictionary<int, StruckTableAffect> _dictionary;
        private readonly List<string> _names = new List<string>();
        private readonly List<int> _uids = new List<int>();

        [MenuItem(ConfigEditor.NameToolDebugAffectDescription, false, (int)ConfigEditor.ToolOrdering.DebugAffectDescription)]
        private static void Open()
        {
            GetWindow<AffectDescriptionDebugWindow>(Title);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            _selectedIndex = 0;
            _tableAffect = TableLoaderManager.LoadAffectTable();
            _dictionary = _tableAffect.GetDatas();
            LoadInfoData();
        }
        private void OnGUI()
        {
            if (_selectedIndex >= _names.Count)
            {
                _selectedIndex = 0;
            }
            EditorGUILayout.LabelField(Title, EditorStyles.boldLabel);
            
            EditorGUILayout.Space();
            
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("게임을 실행 해야 합니다.");
                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField("`선택 Affect 설명 확인하기`를 실행하면 콘솔 로그 창에 출력됩니다.");
                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField("`모든 Affect 설명 txt로 내보내기`를 실행하면, 선택한 폴더에 txt 파일로 생성됩니다.");
                EditorGUILayout.Space(4);
            }
            _selectedIndex = EditorGUILayout.Popup("Affect 선택", _selectedIndex, _names.ToArray());
            
            if (GUILayout.Button("선택 Affect 설명 확인하기")) CheckAffectDescription();
            if (GUILayout.Button("모든 Affect 설명 txt로 내보내기")) ExportAllAffectDescription();
        }

        private void CheckAffectDescription()
        {
            if (!SceneGame.Instance)
            {
                EditorUtility.DisplayDialog(Title, "게임을 실행해주세요.", "OK");
                return;
            }
            int affectUid = _uids[_selectedIndex];
            if (affectUid <= 0)
            {
                EditorUtility.DisplayDialog(Title, "확인할 Affect를 선택해주세요.", "OK");
                return;
            }
            var desc = AffectDescriptionService.Instance.GetDescription(affectUid);
            Debug.Log(desc);
        }

        private void ExportAllAffectDescription()
        {
            if (!SceneGame.Instance)
            {
                EditorUtility.DisplayDialog(Title, "게임을 실행해주세요.", "OK");
                return;
            }

            var path = EditorUtility.SaveFilePanel(
                "Affect Description TSV Export",
                Application.dataPath,
                "affect_description",
                "csv");

            if (string.IsNullOrEmpty(path))
                return;

            try
            {
                // 현재 Locale 백업
                var originalLocale = LocalizationSettings.SelectedLocale;

                // en / ko Locale 확보
                var enLocale = LocalizationSettings.AvailableLocales.GetLocale("en");
                var koLocale = LocalizationSettings.AvailableLocales.GetLocale("ko");

                if (enLocale == null || koLocale == null)
                {
                    EditorUtility.DisplayDialog(
                        Title,
                        "en 또는 ko Locale이 Localization Settings에 존재하지 않습니다.",
                        "OK");
                    return;
                }

                var sb = new System.Text.StringBuilder(4096);

                // TSV Header
                sb.AppendLine("Uid\tName\tEn\tKo");

                foreach (var kvp in _dictionary)
                {
                    var info = kvp.Value;
                    if (info.Uid <= 0)
                        continue;

                    // EN
                    LocalizationSettings.SelectedLocale = enLocale;
                    AffectDescriptionService.ClearCache();
                    var enDesc = AffectDescriptionService.Instance.GetDescription(info.Uid);

                    // KO
                    LocalizationSettings.SelectedLocale = koLocale;
                    AffectDescriptionService.ClearCache();
                    var koDesc = AffectDescriptionService.Instance.GetDescription(info.Uid);

                    // TSV Escape (개행/탭 제거)
                    enDesc = SanitizeTsv(enDesc);
                    koDesc = SanitizeTsv(koDesc);

                    sb.Append(info.Uid).Append('\t')
                        .Append(info.Name).Append('\t')
                        .Append(enDesc).Append('\t')
                        .Append(koDesc).AppendLine();
                }

                // Locale 복구
                LocalizationSettings.SelectedLocale = originalLocale;
                AffectDescriptionService.ClearCache();

                System.IO.File.WriteAllText(path, sb.ToString(), System.Text.Encoding.UTF8);

                EditorUtility.DisplayDialog(
                    Title,
                    $"TSV 파일 생성 완료\n\n{path}",
                    "OK");
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorUtility.DisplayDialog(
                    Title,
                    "TSV 생성 중 오류가 발생했습니다.\n콘솔 로그를 확인해주세요.",
                    "OK");
            }
        }

        /// <summary>
        /// TSV 출력용 문자열 정리
        /// </summary>
        private static string SanitizeTsv(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            // TSV 규칙:
            // - 탭은 컬럼을 깨뜨리므로 제거
            // - 큰따옴표는 "" 로 escape
            // - 줄바꿈이 있으면 전체를 "로 감싼다
            bool hasNewLine = value.Contains("\n") || value.Contains("\r");

            value = value
                .Replace("\r\n", "\n")   // 개행 통일
                .Replace("\r", "\n")
                .Replace("\t", " ")      // 탭 제거
                .Replace("\"", "\"\"");  // quote escape

            if (hasNewLine)
                return $"\"{value}\"";

            return value;
        }

        private void LoadInfoData()
        {
            _names.Clear();
            _uids.Clear();

            foreach (var kvp in _dictionary)
            {
                var info = kvp.Value;
                if (info.Uid <= 0) continue;

                _names.Add($"{info.Uid} - {info.Name}");
                _uids.Add(info.Uid);
            }
            _selectedIndex = 0; // 추가
        }

    }
}
