using System;
using System.Collections.Generic;
using System.IO;
using GGemCo2DCore;
using UnityEditor;
using UnityEngine;
using UnityEngine.Timeline;

namespace GGemCo2DCoreEditor
{
    public class CutsceneEditorWindow : DefaultEditorWindow
    {
        private const string Title = "연출툴";
        public const string TempImportFolder = "Assets/_test";

        private TableCutscene _tableCutscene;
        private Dictionary<int, StruckTableCutscene> _cutsceneDictionary;

        private readonly List<SearchableDropdownUtility.Option<StruckTableCutscene>> _dropDownOptions = new();
        private StruckTableCutscene _selectedCutscene;

        private Vector2 _scroll;
        private string _lastReloadMessage = string.Empty;
        private string _lastActionMessage = string.Empty;

        private TextAsset _selectedJson;
        private TimelineAsset _selectedTimelineAsset;

        [MenuItem(ConfigEditor.NameToolCutscene, false, (int)ConfigEditor.ToolOrdering.Cutscene)]
        private static void Open()
        {
            GetWindow<CutsceneEditorWindow>(Title);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            ReloadCutsceneTable();
        }

        private void OnGUI()
        {
            using (var scroll = new EditorGUILayout.ScrollViewScope(_scroll))
            {
                _scroll = scroll.scrollPosition;

                EditorGUILayout.Space(6);
                DrawSelectionSection();
                EditorGUILayout.Space(8);

                DrawTimelineSection();
                EditorGUILayout.Space(8);

                DrawJsonImportSection();
                EditorGUILayout.Space(8);

                DrawTableReloadSection(_lastReloadMessage, "cutscene 재로딩", ReloadCutsceneTable);
                EditorGUILayout.Space(10);

                if (!string.IsNullOrEmpty(_lastActionMessage))
                {
                    EditorGUILayout.HelpBox(_lastActionMessage, MessageType.Info);
                }
            }
        }

        private void DrawSelectionSection()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("연출 선택", EditorStyles.boldLabel);

                if (_dropDownOptions.Count == 0)
                {
                    EditorGUILayout.HelpBox("등록된 연출이 없습니다. cutscene 테이블을 확인해주세요.", MessageType.Warning);
                    return;
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.PrefixLabel("Cutscene");

                    var currentText = _selectedCutscene != null
                        ? $"{_selectedCutscene.Uid} - {_selectedCutscene.Memo}"
                        : "선택...";

                    var selectedIndex = _selectedCutscene != null ? _selectedCutscene.Uid : 0;

                    SearchableDropdownUtility.DrawButtonAndShow(
                        buttonText: currentText,
                        options: _dropDownOptions,
                        selectedIndex: selectedIndex,
                        onSelected: (idx, opt) =>
                        {
                            _selectedCutscene = opt.Data;
                            SyncTimelineSelectionWithCutscene();
                            Repaint();
                        },
                        defaultSearchMode: SearchableDropdownUtility.SearchMode.Both);
                }

                if (_selectedCutscene == null)
                {
                    return;
                }

                EditorGUILayout.LabelField("UID", _selectedCutscene.Uid.ToString());
                EditorGUILayout.LabelField("Memo", _selectedCutscene.Memo);
                EditorGUILayout.LabelField("FileName", _selectedCutscene.FileName);
                EditorGUILayout.LabelField("Json Path", GetSelectedCutsceneJsonPath());

                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUI.DisabledScope(!Application.isPlaying))
                    {
                        if (GUILayout.Button("연출 플레이", GUILayout.Height(24)))
                        {
                            PlaySelectedCutscene();
                        }
                    }

                    if (GUILayout.Button("Json 에셋 선택", GUILayout.Height(24)))
                    {
                        PingSelectedCutsceneJson();
                    }

                    if (GUILayout.Button("Json -> Temp Timeline", GUILayout.Height(24)))
                    {
                        ImportSelectedCutsceneJsonToTempTimeline();
                    }
                }
            }
        }

        private void DrawTimelineSection()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("연출 타임라인", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("선택한 TimelineAsset을 현재 cutscene 테이블 행의 FileName.json으로 바로 내보낼 수 있습니다.", MessageType.Info);

                _selectedTimelineAsset = (TimelineAsset)EditorGUILayout.ObjectField(
                    "Timeline Asset",
                    _selectedTimelineAsset,
                    typeof(TimelineAsset),
                    false);

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("선택한 Timeline 등록(Json 저장)", GUILayout.Height(24)))
                    {
                        ExportSelectedTimelineToCutsceneJson();
                    }

                    if (GUILayout.Button("Timeline 에셋 선택", GUILayout.Height(24)))
                    {
                        PingSelectedTimeline();
                    }
                }
            }
        }

        private void DrawJsonImportSection()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("JSON -> Timeline 생성", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("임의의 Json 파일을 선택해 Temp Timeline으로 변환할 수 있습니다.", MessageType.None);

                _selectedJson = (TextAsset)EditorGUILayout.ObjectField("JSON 파일", _selectedJson, typeof(TextAsset), false);

                if (GUILayout.Button("선택한 Json으로 Temp Timeline 생성", GUILayout.Height(24)))
                {
                    ImportJsonToTempTimeline(_selectedJson);
                }
            }
        }

        private void ReloadCutsceneTable()
        {
            try
            {
                _tableCutscene = TableLoaderManager.LoadCutsceneTable();
                _cutsceneDictionary = _tableCutscene != null ? _tableCutscene.GetDatas() : null;
                RebuildDropdown();
                SyncTimelineSelectionWithCutscene();
                _lastReloadMessage = $"테이블 재로딩 완료: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                _lastReloadMessage = $"테이블 재로딩 실패: {e.GetType().Name} - {e.Message}";
            }

            Repaint();
        }

        private void RebuildDropdown()
        {
            RebuildDropdownOptions(
                source: _cutsceneDictionary != null ? _cutsceneDictionary.Values : null,
                targetOptions: _dropDownOptions,
                isValidRow: row => row != null && row.Uid > 0,
                keySelector: row => row.Uid.ToString(),
                valueSelector: row => string.IsNullOrWhiteSpace(row.Memo) ? row.FileName : row.Memo,
                assignSelected: row =>
                {
                    if (_selectedCutscene == null)
                    {
                        _selectedCutscene = row;
                        return;
                    }

                    if (row == null)
                    {
                        _selectedCutscene = null;
                        return;
                    }

                    if (_cutsceneDictionary != null && _cutsceneDictionary.TryGetValue(_selectedCutscene.Uid, out var selectedRow))
                    {
                        _selectedCutscene = selectedRow;
                    }
                    else
                    {
                        _selectedCutscene = row;
                    }
                });
        }

        private void PlaySelectedCutscene()
        {
            if (_selectedCutscene == null)
            {
                EditorUtility.DisplayDialog(Title, "연출을 먼저 선택해주세요.", "OK");
                return;
            }

            if (!SceneGame.Instance)
            {
                EditorUtility.DisplayDialog(Title, "게임을 실행해주세요.", "OK");
                return;
            }
            
            SceneGame.Instance.CutsceneManager.SetOverlayTextOverride("boss_name", "Shadow Queen");
            
            _ = SceneGame.Instance.CutsceneManager.PlayCutscene(_selectedCutscene.Uid);
        }

        private void ImportSelectedCutsceneJsonToTempTimeline()
        {
            if (_selectedCutscene == null)
            {
                EditorUtility.DisplayDialog(Title, "연출을 먼저 선택해주세요.", "OK");
                return;
            }

            var jsonPath = GetSelectedCutsceneJsonPath();
            var jsonAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(jsonPath);
            if (jsonAsset == null)
            {
                EditorUtility.DisplayDialog(Title, $"Json 파일을 찾지 못했습니다.\n{jsonPath}", "OK");
                return;
            }

            var timelinePath = GetTempTimelinePath(_selectedCutscene.FileName);
            CreateTimelineFromJsonAsset(jsonAsset, timelinePath, $"선택 연출 Json을 Temp Timeline으로 변환 완료\n{timelinePath}");
        }

        private void ImportJsonToTempTimeline(TextAsset jsonAsset)
        {
            if (jsonAsset == null)
            {
                EditorUtility.DisplayDialog(Title, "JSON 파일을 선택해주세요.", "OK");
                return;
            }

            var assetName = Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(jsonAsset));
            var timelinePath = GetTempTimelinePath(assetName);
            CreateTimelineFromJsonAsset(jsonAsset, timelinePath, $"Json -> Temp Timeline 생성 완료\n{timelinePath}");
        }

        private void CreateTimelineFromJsonAsset(TextAsset jsonAsset, string timelinePath, string successMessage)
        {
            TimelineAsset createdTimeline;
            string error;
            if (!CutsceneTimelineJsonUtility.TryCreateTimelineFromJsonAsset(jsonAsset, timelinePath, out createdTimeline, out error))
            {
                EditorUtility.DisplayDialog(Title, error, "OK");
                return;
            }

            _selectedTimelineAsset = createdTimeline;
            Selection.activeObject = createdTimeline;
            EditorGUIUtility.PingObject(createdTimeline);
            _lastActionMessage = successMessage;
            EditorUtility.DisplayDialog(Title, successMessage, "OK");
        }

        private void ExportSelectedTimelineToCutsceneJson()
        {
            if (_selectedCutscene == null)
            {
                EditorUtility.DisplayDialog(Title, "연출을 먼저 선택해주세요.", "OK");
                return;
            }

            if (_selectedTimelineAsset == null)
            {
                EditorUtility.DisplayDialog(Title, "등록할 TimelineAsset을 선택해주세요.", "OK");
                return;
            }

            var jsonPath = GetSelectedCutsceneJsonPath();
            CutsceneData exportedData;
            string error;
            if (!CutsceneTimelineJsonUtility.TryExportTimelineToJson(_selectedTimelineAsset, jsonPath, out exportedData, out error))
            {
                EditorUtility.DisplayDialog(Title, error, "OK");
                return;
            }

            _lastActionMessage = $"Timeline 등록 완료: {_selectedCutscene.Uid} / {_selectedCutscene.FileName}.json";
            EditorUtility.DisplayDialog(Title, "선택한 Timeline을 cutscene Json으로 저장했습니다.", "OK");
        }

        private void SyncTimelineSelectionWithCutscene()
        {
            if (_selectedCutscene == null || string.IsNullOrWhiteSpace(_selectedCutscene.FileName))
            {
                return;
            }

            if (_selectedTimelineAsset != null)
            {
                return;
            }

            var tempTimelinePath = GetTempTimelinePath(_selectedCutscene.FileName);
            _selectedTimelineAsset = AssetDatabase.LoadAssetAtPath<TimelineAsset>(tempTimelinePath);
        }

        private void PingSelectedTimeline()
        {
            if (_selectedTimelineAsset == null)
            {
                EditorUtility.DisplayDialog(Title, "선택된 TimelineAsset이 없습니다.", "OK");
                return;
            }

            Selection.activeObject = _selectedTimelineAsset;
            EditorGUIUtility.PingObject(_selectedTimelineAsset);
        }

        private void PingSelectedCutsceneJson()
        {
            if (_selectedCutscene == null)
            {
                EditorUtility.DisplayDialog(Title, "연출을 먼저 선택해주세요.", "OK");
                return;
            }

            var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(GetSelectedCutsceneJsonPath());
            if (asset == null)
            {
                EditorUtility.DisplayDialog(Title, "선택된 연출 Json 에셋을 찾지 못했습니다.", "OK");
                return;
            }

            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }

        private string GetSelectedCutsceneJsonPath()
        {
            return _selectedCutscene == null
                ? string.Empty
                : $"{ConfigAddressablePath.Narrative.Cutscene}/{_selectedCutscene.FileName}.json";
        }

        private static string GetTempTimelinePath(string assetName)
        {
            return $"{TempImportFolder}/{assetName}.playable";
        }
    }
}
