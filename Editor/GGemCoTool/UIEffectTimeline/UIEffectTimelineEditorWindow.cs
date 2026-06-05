using System;
using System.Collections.Generic;
using System.Linq;
using GGemCo2DCore;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.Timeline;
using Object = UnityEngine.Object;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// UI 효과 테이블을 기준으로 TimelineAsset을 검증하고 RuntimeSequence로 베이크하는 EditorWindow입니다.
    /// </summary>
    public sealed class UIEffectTimelineEditorWindow : EditorWindow
    {
        private TableUIEffect _tableUIEffect;
        private Dictionary<int, StruckTableUIEffect> _uiEffectDictionary = new Dictionary<int, StruckTableUIEffect>();
        private readonly List<SearchableDropdownUtility.Option<StruckTableUIEffect>> _dropDownOptions = new();
        private StruckTableUIEffect _selectedData;
        private StruckTableUIEffect _editingData;
        private TimelineAsset _timelineAsset;
        private UIEffectRuntimeSequence _runtimeSequence;
        private Vector2 _scrollPosition;
        private readonly List<string> _messages = new List<string>();
        private string _timelineAutoSelectMessage;
        private MessageType _timelineAutoSelectMessageType = MessageType.None;

        /// <summary>
        /// UI 효과 타임라인 편집툴을 엽니다.
        /// </summary>
        [MenuItem(ConfigEditor.NameToolUIEffectTimeline, false, (int)ConfigEditor.ToolOrdering.UIEffectTimeline)]
        public static void Open()
        {
            var window = GetWindow<UIEffectTimelineEditorWindow>();
            window.titleContent = new GUIContent("UI Effect Timeline");
            window.minSize = new Vector2(560f, 420f);
            window.Show();
        }

        /// <summary>
        /// 창 활성화 시 UI 효과 테이블을 로드하고 선택 목록을 구성합니다.
        /// </summary>
        private void OnEnable()
        {
            ReloadTable();
        }

        /// <summary>
        /// 에디터 창의 전체 GUI를 그립니다.
        /// </summary>
        private void OnGUI()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            DrawSelectionPanel();
            EditorGUILayout.Space(8f);
            DrawRowEditorPanel();
            EditorGUILayout.Space(8f);
            DrawBakePanel();
            EditorGUILayout.Space(8f);
            DrawPreviewPanel();
            EditorGUILayout.Space(8f);
            DrawMessagePanel();
            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// ui_effect 테이블을 다시 로드하고 드롭다운 옵션을 갱신합니다.
        /// </summary>
        private void ReloadTable()
        {
            int previousUid = GetSelectedUid();
            _messages.Clear();
            _tableUIEffect = TableLoaderManager.LoadUIEffectTable(true);
            _uiEffectDictionary = BuildDictionary(_tableUIEffect);
            RebuildDropdown(previousUid);
            Repaint();
        }

        /// <summary>
        /// 테이블 데이터를 UID 기준 사전으로 변환합니다.
        /// </summary>
        /// <param name="table">변환할 UI 효과 테이블입니다.</param>
        /// <returns>UID 기준 UI 효과 사전입니다.</returns>
        private static Dictionary<int, StruckTableUIEffect> BuildDictionary(TableUIEffect table)
        {
            var result = new Dictionary<int, StruckTableUIEffect>();
            if (table == null)
                return result;

            foreach (KeyValuePair<int, StruckTableUIEffect> pair in table.GetDatas())
            {
                StruckTableUIEffect row = pair.Value;
                if (row == null || row.Uid <= 0)
                    continue;

                result[row.Uid] = row;
            }

            return result;
        }

        /// <summary>
        /// 검색 가능한 드롭다운 옵션을 다시 구성하고 선택 상태를 복원합니다.
        /// </summary>
        /// <param name="preferredUid">복원할 우선 UID입니다.</param>
        private void RebuildDropdown(int preferredUid)
        {
            _dropDownOptions.Clear();
            foreach (StruckTableUIEffect row in _uiEffectDictionary.Values.OrderBy(item => item.Uid))
            {
                string value = string.IsNullOrWhiteSpace(row.Name)
                    ? row.Memo
                    : row.Name;
                _dropDownOptions.Add(new SearchableDropdownUtility.Option<StruckTableUIEffect>(row.Uid.ToString(), value, row));
            }

            if (preferredUid > 0 && _uiEffectDictionary.TryGetValue(preferredUid, out StruckTableUIEffect preferred))
            {
                SelectRow(preferred, true);
                return;
            }

            if (_selectedData != null && _uiEffectDictionary.TryGetValue(_selectedData.Uid, out StruckTableUIEffect selected))
            {
                SelectRow(selected, true);
                return;
            }

            if (_dropDownOptions.Count > 0)
            {
                SelectRow(_dropDownOptions[0].Data, true);
                return;
            }

            SelectRow(null, false);
        }

        /// <summary>
        /// 지정한 UI 효과 Row를 현재 선택 항목으로 설정하고, 필요하면 Timeline과 RuntimeSequence를 자동 선택합니다.
        /// </summary>
        /// <param name="row">선택할 UI 효과 Row입니다.</param>
        /// <param name="autoSelectAssets">UID 기반 규칙으로 관련 에셋을 자동 선택할지 여부입니다.</param>
        private void SelectRow(StruckTableUIEffect row, bool autoSelectAssets)
        {
            _selectedData = row;
            _editingData = CloneRow(row);
            _messages.Clear();
            ClearTimelineAutoSelectMessage();

            if (!autoSelectAssets || row == null)
            {
                _timelineAsset = null;
                _runtimeSequence = null;
                return;
            }

            AutoSelectAssetsForSelectedRow(false);
        }

        /// <summary>
        /// 선택 패널을 그립니다.
        /// </summary>
        private void DrawSelectionPanel()
        {
            EditorGUILayout.LabelField("UI Effect 선택", EditorStyles.boldLabel);
            if (_tableUIEffect == null)
            {
                EditorGUILayout.HelpBox("ui_effect 테이블을 불러오지 못했습니다. 테이블 파일과 Addressables 설정을 확인하세요.", MessageType.Warning);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel("UI Effect");
                string currentText = _selectedData != null
                    ? $"{_selectedData.Uid} | {GetDisplayName(_selectedData)}"
                    : "선택...";
                string selectedKey = _selectedData != null && _selectedData.Uid > 0 ? _selectedData.Uid.ToString() : string.Empty;

                SearchableDropdownUtility.DrawButtonAndShow(
                    buttonText: currentText,
                    options: _dropDownOptions,
                    selectedIndex: -1,
                    onSelected: (_, option) =>
                    {
                        SelectRow(option.Data, true);
                        Repaint();
                    },
                    defaultSearchMode: SearchableDropdownUtility.SearchMode.Both,
                    selectedKey: selectedKey);

                if (GUILayout.Button("리로드", GUILayout.Width(60)))
                {
                    ReloadTable();
                }
            }

            DrawExpectedPathInfo();
        }

        /// <summary>
        /// 선택한 UI 효과의 UID 기반 Timeline/RuntimeSequence 경로를 표시합니다.
        /// </summary>
        private void DrawExpectedPathInfo()
        {
            int uid = GetSelectedUid();
            if (uid <= 0)
            {
                EditorGUILayout.HelpBox("UI Effect를 선택하면 UID 기반 경로가 표시됩니다.", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField("권장 Timeline 경로");
            EditorGUILayout.SelectableLabel(UIEffectTimelineAuthoringPath.GetTimelineAssetPath(uid), EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
            EditorGUILayout.LabelField("RuntimeSequence 경로");
            EditorGUILayout.SelectableLabel(UIEffectTimelineAuthoringPath.GetRuntimeSequenceAssetPath(uid), EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
            EditorGUILayout.LabelField("RuntimeSequence Key");
            EditorGUILayout.SelectableLabel(UIEffectTimelineAuthoringPath.GetRuntimeSequenceKey(uid), EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
        }

        /// <summary>
        /// 선택된 UI 효과 Row를 편집하는 패널을 그립니다.
        /// </summary>
        private void DrawRowEditorPanel()
        {
            EditorGUILayout.LabelField("테이블 Row 편집", EditorStyles.boldLabel);
            if (_editingData == null)
            {
                EditorGUILayout.HelpBox("편집할 UI Effect를 선택하세요.", MessageType.Info);
                return;
            }

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.IntField("Uid", _editingData.Uid);
            }

            _editingData.Name = EditorGUILayout.TextField("Name", _editingData.Name ?? string.Empty);
            _editingData.Memo = EditorGUILayout.TextField("Memo", _editingData.Memo ?? string.Empty);
            _editingData.Category = EditorGUILayout.TextField("Category", _editingData.Category ?? string.Empty);
            _editingData.TargetKey = EditorGUILayout.TextField("TargetKey", _editingData.TargetKey ?? string.Empty);
            _editingData.PreLoad = EditorGUILayout.Toggle("PreLoad", _editingData.PreLoad);
            _editingData.Loop = EditorGUILayout.Toggle("Loop", _editingData.Loop);
            _editingData.DefaultDuration = EditorGUILayout.FloatField("DefaultDuration", _editingData.DefaultDuration);
            _editingData.Enabled = EditorGUILayout.Toggle("Enabled", _editingData.Enabled);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Row 저장"))
                {
                    SaveEditingRow();
                }

                if (GUILayout.Button("되돌리기"))
                {
                    _editingData = CloneRow(_selectedData);
                }
            }
        }

        /// <summary>
        /// 검증과 베이크 버튼 UI를 그립니다.
        /// </summary>
        private void DrawBakePanel()
        {
            EditorGUILayout.LabelField("Timeline / Bake", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            _timelineAsset = (TimelineAsset)EditorGUILayout.ObjectField("Timeline Asset", _timelineAsset, typeof(TimelineAsset), false);
            if (EditorGUI.EndChangeCheck())
            {
                ClearTimelineAutoSelectMessage();
            }

            _runtimeSequence = (UIEffectRuntimeSequence)EditorGUILayout.ObjectField("Runtime Sequence", _runtimeSequence, typeof(UIEffectRuntimeSequence), false);
            DrawTimelineAutoSelectMessage();

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(GetSelectedUid() <= 0))
                {
                    if (GUILayout.Button("UID 기반 자동 선택"))
                    {
                        AutoSelectAssetsForSelectedRow(true);
                    }
                }

                using (new EditorGUI.DisabledScope(_timelineAsset == null))
                {
                    if (GUILayout.Button("Validate"))
                    {
                        Validate();
                    }
                }

                using (new EditorGUI.DisabledScope(GetSelectedUid() <= 0 || _timelineAsset == null))
                {
                    if (GUILayout.Button("Bake + Addressables 등록"))
                    {
                        Bake();
                    }
                }
            }
        }

        /// <summary>
        /// Play Mode에서 RuntimeSequence를 실제로 재생하는 미리보기 UI를 그립니다.
        /// </summary>
        private void DrawPreviewPanel()
        {
            EditorGUILayout.LabelField("Play Mode Preview", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("씬에 UIEffectTimelineTargetRegistry를 배치하고 targetKey를 등록하면 Play Mode에서 실제 UI에 재생할 수 있습니다.", MessageType.Info);

            using (new EditorGUI.DisabledScope(!EditorApplication.isPlaying || _runtimeSequence == null))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Preview Play"))
                    {
                        PreviewPlay();
                    }

                    if (GUILayout.Button("Preview Stop"))
                    {
                        PreviewStop();
                    }
                }
            }
        }

        /// <summary>
        /// 검증/베이크 결과 메시지를 그립니다.
        /// </summary>
        private void DrawMessagePanel()
        {
            if (_messages.Count == 0)
                return;

            EditorGUILayout.LabelField("Messages", EditorStyles.boldLabel);
            foreach (string message in _messages)
            {
                EditorGUILayout.HelpBox(message, MessageType.None);
            }
        }

        /// <summary>
        /// 현재 선택된 UI 효과 UID와 경로 규칙에 맞는 에셋을 자동으로 선택합니다.
        /// </summary>
        /// <param name="pingTimelineObject">찾은 Timeline 에셋을 Project 창에서 강조 표시할지 여부입니다.</param>
        private void AutoSelectAssetsForSelectedRow(bool pingTimelineObject)
        {
            int selectedUid = GetSelectedUid();
            if (selectedUid <= 0)
            {
                _timelineAsset = null;
                _runtimeSequence = null;
                SetTimelineAutoSelectMessage("UI Effect를 먼저 선택하세요.", MessageType.Info);
                return;
            }

            if (UIEffectTimelineAuthoringPath.TryFindTimeline(selectedUid, out TimelineAsset timeline, out string timelinePath, out int candidateCount))
            {
                _timelineAsset = timeline;
                if (pingTimelineObject)
                {
                    Selection.activeObject = timeline;
                    EditorGUIUtility.PingObject(timeline);
                }

                SetTimelineAutoSelectMessage($"Timeline 자동 선택: {timelinePath}", MessageType.Info);
            }
            else
            {
                _timelineAsset = null;
                string expectedPath = UIEffectTimelineAuthoringPath.GetTimelineAssetPath(selectedUid);
                string message = candidateCount > 1
                    ? $"동일한 Timeline 후보가 {candidateCount}개 발견되어 자동 선택하지 않았습니다. 권장 경로에 하나만 유지하세요. 권장 경로: {expectedPath}"
                    : $"규칙에 맞는 Timeline 파일을 찾지 못했습니다. 권장 경로: {expectedPath}";
                SetTimelineAutoSelectMessage(message, MessageType.Warning);
            }

            string runtimeSequencePath = UIEffectTimelineAuthoringPath.GetRuntimeSequenceAssetPath(selectedUid);
            _runtimeSequence = AssetDatabase.LoadAssetAtPath<UIEffectRuntimeSequence>(runtimeSequencePath);
        }

        /// <summary>
        /// 현재 TimelineAsset의 UIEffectClip 설정을 검증합니다.
        /// </summary>
        private void Validate()
        {
            _messages.Clear();
            bool isValid = UIEffectTimelineValidationUtility.Validate(_timelineAsset, out List<string> messages);
            _messages.AddRange(messages);
            if (isValid)
            {
                _messages.Add("검증이 완료되었습니다. 오류가 없습니다.");
            }
        }

        /// <summary>
        /// 현재 TimelineAsset을 UID 기반 RuntimeSequence 경로로 베이크하고 Addressables에 등록합니다.
        /// </summary>
        private void Bake()
        {
            _messages.Clear();
            int selectedUid = GetSelectedUid();
            if (selectedUid <= 0)
            {
                _messages.Add("UI Effect를 먼저 선택하세요.");
                return;
            }

            if (!UIEffectTimelineValidationUtility.Validate(_timelineAsset, out List<string> messages))
            {
                _messages.AddRange(messages);
                return;
            }

            string outputPath = UIEffectTimelineAuthoringPath.GetRuntimeSequenceAssetPath(selectedUid);
            _runtimeSequence = UIEffectTimelineBaker.Bake(_timelineAsset, outputPath);
            if (_runtimeSequence == null)
            {
                _messages.Add("베이크에 실패했습니다.");
                return;
            }

            _runtimeSequence.sequenceKey = UIEffectTimelineAuthoringPath.GetRuntimeSequenceKey(selectedUid);
            EditorUtility.SetDirty(_runtimeSequence);
            EnsureAddressableEntry(
                AssetDatabase.GetAssetPath(_runtimeSequence),
                _runtimeSequence.sequenceKey,
                ConfigAddressableGroupName.UIEffectRuntimeSequence,
                ConfigAddressableLabel.UIEffectRuntimeSequence);

            EditorGUIUtility.PingObject(_runtimeSequence);
            _messages.Add($"베이크 완료: {_runtimeSequence.events.Length}개 이벤트, {_runtimeSequence.payloads.Length}개 Payload");
            _messages.Add($"Addressables 등록 완료: {_runtimeSequence.sequenceKey}");
        }

        /// <summary>
        /// 선택된 Row 편집 내용을 ui_effect.txt에 저장합니다.
        /// </summary>
        private void SaveEditingRow()
        {
            _messages.Clear();
            if (_editingData == null || _editingData.Uid <= 0)
            {
                _messages.Add("저장할 UI Effect Row가 없습니다.");
                return;
            }

            if (!TableTextRowPatchUtility.TryPatchRowByUid(
                    ConfigAddressableTable.TableUIEffect.Path,
                    _editingData.Uid,
                    _editingData,
                    SerializeUIEffectRow,
                    out string error))
            {
                _messages.Add($"Row 저장 실패: {error}");
                return;
            }

            int uid = _editingData.Uid;
            ReloadTable();
            if (_uiEffectDictionary.TryGetValue(uid, out StruckTableUIEffect saved))
            {
                SelectRow(saved, false);
            }

            _messages.Add("Row 저장 완료");
        }

        /// <summary>
        /// UI 효과 Row를 테이블 파일에 기록할 탭 구분 문자열로 변환합니다.
        /// </summary>
        /// <param name="row">저장할 UI 효과 Row입니다.</param>
        /// <param name="headers">출력 순서를 결정하는 테이블 헤더 목록입니다.</param>
        /// <returns>ui_effect 테이블 헤더 순서에 맞춘 한 줄 문자열입니다.</returns>
        private static string SerializeUIEffectRow(StruckTableUIEffect row, IReadOnlyList<string> headers)
        {
            var values = new string[headers.Count];
            for (int i = 0; i < headers.Count; i++)
            {
                values[i] = headers[i] switch
                {
                    "Uid" => row.Uid.ToString(),
                    "Name" => row.Name ?? string.Empty,
                    "Memo" => row.Memo ?? string.Empty,
                    "Category" => string.IsNullOrWhiteSpace(row.Category) ? "Common" : row.Category,
                    "TargetKey" => row.TargetKey ?? string.Empty,
                    "PreLoad" => row.PreLoad ? "Y" : "N",
                    "Loop" => row.Loop ? "Y" : "N",
                    "DefaultDuration" => row.DefaultDuration.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture),
                    "Enabled" => row.Enabled ? "Y" : "N",
                    _ => string.Empty,
                };
            }

            return string.Join("\t", values);
        }

        /// <summary>
        /// 지정한 에셋이 Addressables 그룹에 등록되도록 보장합니다.
        /// </summary>
        /// <param name="assetPath">등록할 에셋의 Unity 프로젝트 경로입니다.</param>
        /// <param name="addressKey">Addressables 주소 키입니다.</param>
        /// <param name="groupName">등록할 Addressables 그룹 이름입니다.</param>
        /// <param name="label">부여할 Addressables 라벨입니다.</param>
        private static void EnsureAddressableEntry(string assetPath, string addressKey, string groupName, string label)
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogWarning("[UIEffectTimeline] AddressableAssetSettings가 없습니다.");
                return;
            }

            if (string.IsNullOrWhiteSpace(assetPath))
            {
                Debug.LogWarning("[UIEffectTimeline] assetPath가 비어 있습니다.");
                return;
            }

            string guid = AssetDatabase.AssetPathToGUID(assetPath);
            if (string.IsNullOrEmpty(guid))
            {
                Debug.LogError($"[UIEffectTimeline] GUID를 찾을 수 없습니다. assetPath={assetPath}");
                return;
            }

            AddressableAssetGroup group = settings.FindGroup(groupName);
            if (group == null)
            {
                group = settings.CreateGroup(groupName, false, false, true, settings.DefaultGroup.Schemas);
            }

            AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, group);
            entry.address = addressKey;

            if (!string.IsNullOrWhiteSpace(label))
            {
                settings.AddLabel(label, true);
                entry.SetLabel(label, true);
            }

            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entry, true);
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// Play Mode에서 선택된 RuntimeSequence를 재생합니다.
        /// </summary>
        private void PreviewPlay()
        {
            UIEffectTimelinePlayer player = Object.FindObjectOfType<UIEffectTimelinePlayer>();
            if (player == null)
            {
                var playerObject = new GameObject("UIEffectTimelinePlayer_Preview");
                player = playerObject.AddComponent<UIEffectTimelinePlayer>();
            }

            player.Play(_runtimeSequence);
        }

        /// <summary>
        /// Play Mode에서 실행 중인 Preview 재생을 중지합니다.
        /// </summary>
        private void PreviewStop()
        {
            UIEffectTimelinePlayer player = Object.FindObjectOfType<UIEffectTimelinePlayer>();
            if (player != null)
            {
                player.Stop();
            }
        }

        /// <summary>
        /// 선택된 UI 효과 UID를 반환합니다.
        /// </summary>
        /// <returns>선택된 UI 효과 UID입니다. 없으면 0입니다.</returns>
        private int GetSelectedUid()
        {
            return _selectedData?.Uid ?? 0;
        }

        /// <summary>
        /// UI 효과 표시 이름을 생성합니다.
        /// </summary>
        /// <param name="row">표시할 UI 효과 Row입니다.</param>
        /// <returns>드롭다운과 라벨에 사용할 표시 이름입니다.</returns>
        private static string GetDisplayName(StruckTableUIEffect row)
        {
            if (row == null)
                return string.Empty;

            if (!string.IsNullOrWhiteSpace(row.Name))
                return row.Name;

            if (!string.IsNullOrWhiteSpace(row.Memo))
                return row.Memo;

            return $"UIEffect {row.Uid}";
        }

        /// <summary>
        /// 편집 중 원본 Row를 오염시키지 않도록 얕은 복사본을 생성합니다.
        /// </summary>
        /// <param name="row">복사할 원본 Row입니다.</param>
        /// <returns>복사된 Row입니다. 원본이 없으면 null입니다.</returns>
        private static StruckTableUIEffect CloneRow(StruckTableUIEffect row)
        {
            if (row == null)
                return null;

            return new StruckTableUIEffect
            {
                Uid = row.Uid,
                Name = row.Name,
                Memo = row.Memo,
                Category = row.Category,
                TargetKey = row.TargetKey,
                PreLoad = row.PreLoad,
                Loop = row.Loop,
                DefaultDuration = row.DefaultDuration,
                Enabled = row.Enabled,
            };
        }

        /// <summary>
        /// Timeline 자동 선택 결과 메시지를 UI에 표시합니다.
        /// </summary>
        private void DrawTimelineAutoSelectMessage()
        {
            if (string.IsNullOrWhiteSpace(_timelineAutoSelectMessage) || _timelineAutoSelectMessageType == MessageType.None)
                return;

            EditorGUILayout.HelpBox(_timelineAutoSelectMessage, _timelineAutoSelectMessageType);
        }

        /// <summary>
        /// Timeline 자동 선택 결과 메시지를 설정합니다.
        /// </summary>
        /// <param name="message">표시할 메시지입니다.</param>
        /// <param name="messageType">메시지 표시 타입입니다.</param>
        private void SetTimelineAutoSelectMessage(string message, MessageType messageType)
        {
            _timelineAutoSelectMessage = message;
            _timelineAutoSelectMessageType = messageType;
        }

        /// <summary>
        /// Timeline 자동 선택 결과 메시지를 제거합니다.
        /// </summary>
        private void ClearTimelineAutoSelectMessage()
        {
            _timelineAutoSelectMessage = null;
            _timelineAutoSelectMessageType = MessageType.None;
        }
    }
}
