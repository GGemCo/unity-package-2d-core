using System;
using System.Collections.Generic;
using System.IO;
using GGemCo2DCore;
using UnityEditor;
using UnityEngine;
using UnityEngine.Timeline;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// 컷신 데이터를 선택, 미리보기, Import/Export할 수 있는 Unity Editor 전용 창입니다.
    /// </summary>
    /// <remarks>
    /// - 컷신 테이블 로드 및 선택 관리
    /// - Timeline ↔ JSON 변환 기능 제공
    /// - 각 UI 영역은 Panel 클래스로 분리되어 있으며, 본 클래스는 흐름 제어를 담당합니다.
    /// </remarks>
    public class CutsceneEditorWindow : DefaultEditorWindow
    {
        private const string Title = "연출툴";

        /// <summary>
        /// JSON Import 시 생성되는 임시 Timeline 저장 경로입니다.
        /// </summary>
        public const string TempImportFolder = "Assets/_test";

        private TableCutscene _tableCutscene;
        private Dictionary<int, StruckTableCutscene> _cutsceneDictionary;

        private readonly List<SearchableDropdownUtility.Option<StruckTableCutscene>> _dropDownOptions = new();
        private readonly CutsceneEditorState _state = new();

        private CutsceneSelectionPanel _selectionPanel;
        private CutsceneTimelinePanel _timelinePanel;
        private CutsceneJsonImportPanel _jsonImportPanel;

        /// <summary>
        /// 컷신 에디터 창을 엽니다.
        /// </summary>
        [MenuItem(ConfigEditor.NameToolCutscene, false, (int)ConfigEditor.ToolOrdering.Cutscene)]
        private static void Open()
        {
            GetWindow<CutsceneEditorWindow>(Title);
        }

        /// <summary>
        /// EditorWindow 초기화 시 호출됩니다.
        /// UI 패널 생성 및 컷신 테이블을 로드합니다.
        /// </summary>
        protected override void OnEnable()
        {
            base.OnEnable();
            _selectionPanel = new CutsceneSelectionPanel(Title);
            _timelinePanel = new CutsceneTimelinePanel();
            _jsonImportPanel = new CutsceneJsonImportPanel();
            ReloadCutsceneTable();
        }

        /// <summary>
        /// Unity Editor GUI를 렌더링합니다.
        /// 각 패널을 순차적으로 그리며 상태는 <see cref="CutsceneEditorState"/>를 통해 관리됩니다.
        /// </summary>
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

        /// <summary>
        /// 컷신 테이블을 다시 로드하고 드롭다운 및 선택 상태를 갱신합니다.
        /// </summary>
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

        /// <summary>
        /// 컷신 데이터를 기반으로 드롭다운 옵션을 재구성합니다.
        /// </summary>
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

        /// <summary>
        /// 컷신 선택 시 호출되며 Timeline 선택 상태를 동기화합니다.
        /// </summary>
        private void OnCutsceneSelected(StruckTableCutscene cutscene)
        {
            _state.SelectedCutscene = cutscene;
            SyncTimelineSelectionWithCutscene(forceRefresh: true);
        }

        /// <summary>
        /// 선택된 컷신을 게임 내에서 실행합니다.
        /// </summary>
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

            _ = SceneGame.Instance.CutsceneManager.PlayCutscene(_state.SelectedCutscene.Uid);
        }

        /// <summary>
        /// 선택된 컷신 JSON을 임시 Timeline으로 변환합니다.
        /// </summary>
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

        /// <summary>
        /// 외부에서 선택한 JSON을 임시 Timeline으로 변환합니다.
        /// </summary>
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

        /// <summary>
        /// JSON 에셋을 기반으로 Timeline을 생성하고 Editor에 선택 상태로 반영합니다.
        /// </summary>
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

        /// <summary>
        /// 선택된 Timeline을 JSON 파일로 내보냅니다.
        /// </summary>
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

        /// <summary>
        /// 선택된 컷신과 연결된 Timeline을 자동으로 찾아 선택 상태에 반영합니다.
        /// </summary>
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

        /// <summary>
        /// 선택된 Timeline 에셋을 Project 창에서 강조 표시합니다.
        /// </summary>
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

        /// <summary>
        /// 선택된 컷신 JSON 파일을 Project 창에서 강조 표시합니다.
        /// </summary>
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

        /// <summary>
        /// 현재 선택된 컷신에 해당하는 JSON 파일 경로를 반환합니다.
        /// </summary>
        /// <returns>JSON 파일 경로 또는 선택된 컷신이 없으면 빈 문자열입니다.</returns>
        private string GetSelectedCutsceneJsonPath()
        {
            return _state.SelectedCutscene == null
                ? string.Empty
                : $"{ConfigAddressablePath.Narrative.Cutscene}/{_state.SelectedCutscene.FileName}.json";
        }

        /// <summary>
        /// 임시 Timeline 저장 경로를 생성합니다.
        /// </summary>
        /// <param name="assetName">에셋 이름입니다.</param>
        /// <returns>Timeline 에셋 경로입니다.</returns>
        private static string GetTempTimelinePath(string assetName)
        {
            return $"{TempImportFolder}/{assetName}.playable";
        }
    }
}