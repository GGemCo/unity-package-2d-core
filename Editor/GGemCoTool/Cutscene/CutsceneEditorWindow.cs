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
        private readonly CutsceneEditorState _state = new();

        private CutsceneSelectionPanel _selectionPanel;
        private CutsceneTimelinePanel _timelinePanel;
        private CutsceneJsonImportPanel _jsonImportPanel;

        [MenuItem(ConfigEditor.NameToolCutscene, false, (int)ConfigEditor.ToolOrdering.Cutscene)]
        private static void Open()
        {
            GetWindow<CutsceneEditorWindow>(Title);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            _selectionPanel = new CutsceneSelectionPanel(Title);
            _timelinePanel = new CutsceneTimelinePanel();
            _jsonImportPanel = new CutsceneJsonImportPanel();
            ReloadCutsceneTable();
        }

        private void OnGUI()
        {
            using (var scroll = new EditorGUILayout.ScrollViewScope(_state.Scroll))
            {
                _state.Scroll = scroll.scrollPosition;

                EditorGUILayout.Space(6);
                _selectionPanel.Draw(
                    state: _state,
                    dropDownOptions: _dropDownOptions,
                    getSelectedCutsceneJsonPath: GetSelectedCutsceneJsonPath,
                    onCutsceneSelected: OnCutsceneSelected,
                    playSelectedCutscene: PlaySelectedCutscene,
                    pingSelectedCutsceneJson: PingSelectedCutsceneJson,
                    importSelectedCutsceneJsonToTempTimeline: ImportSelectedCutsceneJsonToTempTimeline,
                    repaint: Repaint);
                EditorGUILayout.Space(8);

                _timelinePanel.Draw(
                    state: _state,
                    exportSelectedTimelineToCutsceneJson: ExportSelectedTimelineToCutsceneJson,
                    pingSelectedTimeline: PingSelectedTimeline);
                EditorGUILayout.Space(8);

                _jsonImportPanel.Draw(_state, ImportJsonToTempTimeline);
                EditorGUILayout.Space(8);

                DrawTableReloadSection(_state.LastReloadMessage, "cutscene 재로딩", ReloadCutsceneTable);
                EditorGUILayout.Space(10);

                if (!string.IsNullOrEmpty(_state.LastActionMessage))
                {
                    EditorGUILayout.HelpBox(_state.LastActionMessage, MessageType.Info);
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
                _state.LastReloadMessage = $"테이블 재로딩 완료: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                _state.LastReloadMessage = $"테이블 재로딩 실패: {e.GetType().Name} - {e.Message}";
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
                    if (_state.SelectedCutscene == null)
                    {
                        _state.SelectedCutscene = row;
                        return;
                    }

                    if (row == null)
                    {
                        _state.SelectedCutscene = null;
                        return;
                    }

                    if (_cutsceneDictionary != null && _cutsceneDictionary.TryGetValue(_state.SelectedCutscene.Uid, out var selectedRow))
                    {
                        _state.SelectedCutscene = selectedRow;
                    }
                    else
                    {
                        _state.SelectedCutscene = row;
                    }
                });
        }

        private void OnCutsceneSelected(StruckTableCutscene cutscene)
        {
            _state.SelectedCutscene = cutscene;
            SyncTimelineSelectionWithCutscene(forceRefresh: true);
        }

        private void PlaySelectedCutscene()
        {
            if (_state.SelectedCutscene == null)
            {
                EditorUtility.DisplayDialog(Title, "연출을 먼저 선택해주세요.", "OK");
                return;
            }

            if (!SceneGame.Instance)
            {
                EditorUtility.DisplayDialog(Title, "게임을 실행해주세요.", "OK");
                return;
            }

            // SceneGame.Instance.CutsceneManager.SetOverlayTextOverride("boss_name", "Shadow Queen");
            _ = SceneGame.Instance.CutsceneManager.PlayCutscene(_state.SelectedCutscene.Uid);
        }

        private void ImportSelectedCutsceneJsonToTempTimeline()
        {
            if (_state.SelectedCutscene == null)
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

            var timelinePath = GetTempTimelinePath(_state.SelectedCutscene.FileName);
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

            _state.SelectedTimelineAsset = createdTimeline;
            Selection.activeObject = createdTimeline;
            EditorGUIUtility.PingObject(createdTimeline);
            _state.LastActionMessage = successMessage;
            EditorUtility.DisplayDialog(Title, successMessage, "OK");
        }

        private void ExportSelectedTimelineToCutsceneJson()
        {
            if (_state.SelectedCutscene == null)
            {
                EditorUtility.DisplayDialog(Title, "연출을 먼저 선택해주세요.", "OK");
                return;
            }

            if (_state.SelectedTimelineAsset == null)
            {
                EditorUtility.DisplayDialog(Title, "등록할 TimelineAsset을 선택해주세요.", "OK");
                return;
            }

            var jsonPath = GetSelectedCutsceneJsonPath();
            CutsceneData exportedData;
            string error;
            if (!CutsceneTimelineJsonUtility.TryExportTimelineToJson(_state.SelectedTimelineAsset, jsonPath, out exportedData, out error))
            {
                EditorUtility.DisplayDialog(Title, error, "OK");
                return;
            }

            _state.LastActionMessage = $"Timeline 등록 완료: {_state.SelectedCutscene.Uid} / {_state.SelectedCutscene.FileName}.json";
            EditorUtility.DisplayDialog(Title, "선택한 Timeline을 cutscene Json으로 저장했습니다.", "OK");
        }

        private void SyncTimelineSelectionWithCutscene(bool forceRefresh = false)
        {
            if (_state.SelectedCutscene == null || string.IsNullOrWhiteSpace(_state.SelectedCutscene.FileName))
            {
                return;
            }

            if (!forceRefresh && _state.SelectedTimelineAsset != null)
            {
                return;
            }

            var tempTimelinePath = GetTempTimelinePath(_state.SelectedCutscene.FileName);
            _state.SelectedTimelineAsset = AssetDatabase.LoadAssetAtPath<TimelineAsset>(tempTimelinePath);
        }

        private void PingSelectedTimeline()
        {
            if (_state.SelectedTimelineAsset == null)
            {
                EditorUtility.DisplayDialog(Title, "선택된 TimelineAsset이 없습니다.", "OK");
                return;
            }

            Selection.activeObject = _state.SelectedTimelineAsset;
            EditorGUIUtility.PingObject(_state.SelectedTimelineAsset);
        }

        private void PingSelectedCutsceneJson()
        {
            if (_state.SelectedCutscene == null)
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
            return _state.SelectedCutscene == null
                ? string.Empty
                : $"{ConfigAddressablePath.Narrative.Cutscene}/{_state.SelectedCutscene.FileName}.json";
        }

        private static string GetTempTimelinePath(string assetName)
        {
            return $"{TempImportFolder}/{assetName}.playable";
        }
    }
}
