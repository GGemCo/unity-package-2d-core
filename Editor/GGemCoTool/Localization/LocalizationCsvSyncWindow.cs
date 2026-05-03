#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// 모든 StringTableCollection 을 단일 CSV 로 내보내고,
    /// 번역 결과를 다시 Merge 하는 에디터 윈도우입니다.
    /// </summary>
    public sealed class LocalizationCsvSyncWindow : EditorWindow
    {
        private const string Title = "Localization CSV 동기화 툴";
        private const string ExportPathKey = "GGemCo2DCoreEditor.LocalizationCsvSync.ExportPath";
        private const string ImportPathKey = "GGemCo2DCoreEditor.LocalizationCsvSync.ImportPath";

        private bool _includeSmartColumns = true;
        private bool _overwriteWithEmptyValue;
        private bool _createMissingEntries = true;
        private bool _createMissingLocaleTables = true;
        private bool _allowKeyRename;
        private string _exportPath = string.Empty;
        private string _importPath = string.Empty;
        private string _logText = string.Empty;
        private Vector2 _scrollPosition;

        /// <summary>
        /// 툴 윈도우를 엽니다.
        /// </summary>
        [MenuItem(ConfigEditor.NameToolLocalizationCsvSync, false, (int)ConfigEditor.ToolOrdering.LocalizationCsvSync)]
        public static void ShowWindow()
        {
            GetWindow<LocalizationCsvSyncWindow>(Title);
        }

        /// <summary>
        /// 에디터 윈도우가 활성화될 때 저장된 경로를 복원합니다.
        /// </summary>
        private void OnEnable()
        {
            _exportPath = EditorPrefs.GetString(ExportPathKey, BuildDefaultCsvPath());
            _importPath = EditorPrefs.GetString(ImportPathKey, _exportPath);
        }

        /// <summary>
        /// 에디터 윈도우가 비활성화될 때 현재 경로를 저장합니다.
        /// </summary>
        private void OnDisable()
        {
            EditorPrefs.SetString(ExportPathKey, _exportPath ?? string.Empty);
            EditorPrefs.SetString(ImportPathKey, _importPath ?? string.Empty);
        }

        /// <summary>
        /// 윈도우 GUI 를 그립니다.
        /// </summary>
        private void OnGUI()
        {
            DrawExportSection();
            EditorGUILayout.Space(12f);
            DrawImportSection();
            EditorGUILayout.Space(12f);
            DrawLogSection();
        }

        /// <summary>
        /// CSV 내보내기 영역을 그립니다.
        /// </summary>
        private void DrawExportSection()
        {
            HelperEditorUI.OnGUITitle("1. CSV 내보내기");
            EditorGUILayout.HelpBox("프로젝트의 모든 String Table Collection 을 하나의 CSV 파일로 내보냅니다.", MessageType.Info);

            using (new EditorGUILayout.VerticalScope("box"))
            {
                DrawPathField(ref _exportPath, "저장 경로", SelectExportPath);
                _includeSmartColumns = EditorGUILayout.ToggleLeft("Smart String 플래그 컬럼 포함", _includeSmartColumns);

                EditorGUILayout.Space(4f);
                using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(_exportPath)))
                {
                    if (GUILayout.Button("CSV 내보내기", GUILayout.Height(32f)))
                    {
                        ExecuteExport();
                    }
                }
            }
        }

        /// <summary>
        /// CSV 가져오기/병합 영역을 그립니다.
        /// </summary>
        private void DrawImportSection()
        {
            HelperEditorUI.OnGUITitle("2. CSV 가져오기 및 Merge");
            EditorGUILayout.HelpBox("Collection + Id 우선, Collection + Key 보조 규칙으로 병합합니다.", MessageType.Info);

            using (new EditorGUILayout.VerticalScope("box"))
            {
                DrawPathField(ref _importPath, "불러올 CSV", SelectImportPath);
                _overwriteWithEmptyValue = EditorGUILayout.ToggleLeft("빈 셀도 실제 빈 문자열로 반영", _overwriteWithEmptyValue);
                _createMissingEntries = EditorGUILayout.ToggleLeft("없는 Key 는 신규 생성", _createMissingEntries);
                _createMissingLocaleTables = EditorGUILayout.ToggleLeft("없는 Locale 테이블은 자동 생성", _createMissingLocaleTables);
                _allowKeyRename = EditorGUILayout.ToggleLeft("Id 매칭 시 Key 이름 변경 허용", _allowKeyRename);

                EditorGUILayout.Space(4f);
                using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(_importPath)))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("미리보기", GUILayout.Height(32f)))
                        {
                            ExecuteImport(true);
                        }

                        if (GUILayout.Button("Merge 적용", GUILayout.Height(32f)))
                        {
                            ExecuteImport(false);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 결과 로그 영역을 그립니다.
        /// </summary>
        private void DrawLogSection()
        {
            HelperEditorUI.OnGUITitle("3. 작업 로그");
            using (new EditorGUILayout.VerticalScope("box"))
            {
                _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(260f));
                EditorGUILayout.TextArea(_logText, GUILayout.ExpandHeight(true));
                EditorGUILayout.EndScrollView();
            }
        }

        /// <summary>
        /// 공통 경로 선택 UI 를 그립니다.
        /// </summary>
        /// <param name="path">편집할 경로 문자열입니다.</param>
        /// <param name="label">표시 라벨입니다.</param>
        /// <param name="selectAction">경로 선택 콜백입니다.</param>
        private void DrawPathField(ref string path, string label, Action selectAction)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                path = EditorGUILayout.TextField(label, path);
                if (GUILayout.Button("선택", GUILayout.Width(80f)))
                {
                    selectAction?.Invoke();
                }
            }
        }

        /// <summary>
        /// 내보내기 경로를 선택합니다.
        /// </summary>
        private void SelectExportPath()
        {
            var selectedPath = EditorUtility.SaveFilePanel(
                "Localization CSV 저장",
                GetDirectoryOrFallback(_exportPath),
                Path.GetFileName(string.IsNullOrWhiteSpace(_exportPath) ? LocalizationCsvSyncService.DefaultFileName : _exportPath),
                "csv");

            if (!string.IsNullOrWhiteSpace(selectedPath))
            {
                _exportPath = selectedPath;
                if (string.IsNullOrWhiteSpace(_importPath))
                {
                    _importPath = selectedPath;
                }
            }
        }

        /// <summary>
        /// 가져오기 경로를 선택합니다.
        /// </summary>
        private void SelectImportPath()
        {
            var selectedPath = EditorUtility.OpenFilePanel(
                "Localization CSV 선택",
                GetDirectoryOrFallback(_importPath),
                "csv");

            if (!string.IsNullOrWhiteSpace(selectedPath))
            {
                _importPath = selectedPath;
            }
        }

        /// <summary>
        /// CSV 내보내기를 실행합니다.
        /// </summary>
        private void ExecuteExport()
        {
            try
            {
                var result = LocalizationCsvSyncService.ExportAll(_exportPath, _includeSmartColumns);
                _logText = result.GetLogText();
                _importPath = _exportPath;
            }
            catch (Exception ex)
            {
                _logText = $"[오류] CSV 내보내기 실패\n{ex}";
                Debug.LogException(ex);
            }
        }

        /// <summary>
        /// CSV 미리보기 또는 실제 병합을 실행합니다.
        /// </summary>
        /// <param name="dryRun">true 이면 미리보기, false 이면 실제 적용입니다.</param>
        private void ExecuteImport(bool dryRun)
        {
            try
            {
                var options = LocalizationCsvSyncOptions.CreateDefault();
                options.OverwriteWithEmptyValue = _overwriteWithEmptyValue;
                options.CreateMissingEntries = _createMissingEntries;
                options.CreateMissingLocaleTables = _createMissingLocaleTables;
                options.AllowKeyRename = _allowKeyRename;
                options.DryRun = dryRun;

                var result = LocalizationCsvSyncService.ImportAndMerge(_importPath, options);
                _logText = result.GetLogText();
            }
            catch (Exception ex)
            {
                _logText = $"[오류] CSV 병합 실패\n{ex}";
                Debug.LogException(ex);
            }
        }

        /// <summary>
        /// 기본 CSV 저장 경로를 계산합니다.
        /// </summary>
        /// <returns>절대 경로 문자열입니다.</returns>
        private static string BuildDefaultCsvPath()
        {
            var projectRoot = Path.GetDirectoryName(Application.dataPath);
            return Path.Combine(projectRoot ?? Application.dataPath, LocalizationCsvSyncService.DefaultFileName);
        }

        /// <summary>
        /// 파일 패널용 기본 폴더를 계산합니다.
        /// </summary>
        /// <param name="path">현재 파일 경로입니다.</param>
        /// <returns>존재하는 폴더 경로입니다.</returns>
        private static string GetDirectoryOrFallback(string path)
        {
            var directory = string.IsNullOrWhiteSpace(path) ? string.Empty : Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory))
            {
                return directory;
            }

            return Path.GetDirectoryName(Application.dataPath) ?? Application.dataPath;
        }
    }
}
#endif
