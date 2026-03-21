#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// 프로젝트 내 Animation Event 함수명을 일괄 변경하는 툴.
    /// 일반 .anim 클립과 ModelImporter(FBX 등) 기반 클립을 모두 지원한다.
    /// </summary>
    public class AnimationEventNameChangerWindow : EditorWindow
    {
        private const string Title = "애니메이션 Event 이름 변경 툴";
        private const string DefaultOldFunctionName = "GGemCoAniEventEffect";
        private const string DefaultNewFunctionName = "GGemCoAniEventVfx";

        private readonly List<ScanEntry> _entries = new List<ScanEntry>();
        private readonly List<string> _searchFolders = new List<string> { "Assets" };

        private Vector2 _scrollFolders;
        private Vector2 _scrollEntries;
        private string _oldFunctionName = DefaultOldFunctionName;
        private string _newFunctionName = DefaultNewFunctionName;
        private string _newFolderInput = "Assets";
        private bool _includeAnimClipAssets = true;
        private bool _includeModelImporterClips = true;
        private bool _showOnlyMatched = true;
        private bool _showOnlySelected;
        private bool _selectAllMatchesOnScan = true;

        private int _scannedAssetCount;
        private int _matchedEventCount;
        private int _changedEventCountLastApply;
        private int _changedAssetCountLastApply;
        private string _lastSummary = string.Empty;

        [MenuItem(ConfigEditor.NameToolAnimationEventNameChanger, false, (int)ConfigEditor.ToolOrdering.AnimationEventNameChanger)]
        public static void ShowWindow()
        {
            GetWindow<AnimationEventNameChangerWindow>(Title);
        }

        private void OnGUI()
        {
            DrawHeader();
            EditorGUILayout.Space(4f);
            DrawSearchOptions();
            EditorGUILayout.Space(8f);
            DrawActions();
            EditorGUILayout.Space(8f);
            DrawSummary();
            EditorGUILayout.Space(8f);
            DrawResults();
        }

        private void DrawHeader()
        {
            EditorGUILayout.LabelField(Title, EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Project 탭 기준으로 Animation Clip의 Animation Event 함수명을 검색하고 일괄 변경합니다.\n" +
                ".anim 에셋과 FBX 같은 ModelImporter 기반 클립을 모두 지원합니다.",
                MessageType.Info);
        }

        private void DrawSearchOptions()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("변경 설정", EditorStyles.boldLabel);
            _oldFunctionName = EditorGUILayout.TextField("기존 함수명", _oldFunctionName);
            _newFunctionName = EditorGUILayout.TextField("새 함수명", _newFunctionName);
            _includeAnimClipAssets = EditorGUILayout.ToggleLeft("일반 .anim 클립 포함", _includeAnimClipAssets);
            _includeModelImporterClips = EditorGUILayout.ToggleLeft("모델(FBX 등) 내부 클립 포함", _includeModelImporterClips);
            _showOnlyMatched = EditorGUILayout.ToggleLeft("결과에서 매칭 항목만 표시", _showOnlyMatched);
            _showOnlySelected = EditorGUILayout.ToggleLeft("결과에서 선택 항목만 표시", _showOnlySelected);
            _selectAllMatchesOnScan = EditorGUILayout.ToggleLeft("스캔 후 매칭 항목 자동 선택", _selectAllMatchesOnScan);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(4f);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("검색 폴더", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            _newFolderInput = EditorGUILayout.TextField("폴더 추가", _newFolderInput);
            if (GUILayout.Button("추가", GUILayout.Width(70f)))
            {
                AddSearchFolder(_newFolderInput);
            }
            if (GUILayout.Button("Selection 사용", GUILayout.Width(100f)))
            {
                AddFoldersFromSelection();
            }
            EditorGUILayout.EndHorizontal();

            _scrollFolders = EditorGUILayout.BeginScrollView(_scrollFolders, GUILayout.Height(90f));
            int removeIndex = -1;
            for (int i = 0; i < _searchFolders.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.SelectableLabel(_searchFolders[i], EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
                if (GUILayout.Button("제거", GUILayout.Width(60f)))
                {
                    removeIndex = i;
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();

            if (removeIndex >= 0 && _searchFolders.Count > 1)
            {
                _searchFolders.RemoveAt(removeIndex);
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Assets로 초기화", EditorConstants.GUILayoutButtonHeight22))
            {
                _searchFolders.Clear();
                _searchFolders.Add("Assets");
            }
            if (GUILayout.Button("중복 제거", EditorConstants.GUILayoutButtonHeight22))
            {
                RemoveDuplicateFolders();
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private void DrawActions()
        {
            EditorGUILayout.BeginHorizontal();
            GUI.enabled = CanScan();
            if (GUILayout.Button("스캔", GUILayout.Height(28f)))
            {
                Scan();
            }

            GUI.enabled = _entries.Count > 0;
            if (GUILayout.Button("전체 선택", GUILayout.Height(28f)))
            {
                SetSelectionForVisibleEntries(true);
            }
            if (GUILayout.Button("전체 해제", GUILayout.Height(28f)))
            {
                SetSelectionForVisibleEntries(false);
            }

            GUI.enabled = CanApply();
            if (GUILayout.Button("선택 항목 적용", GUILayout.Height(28f)))
            {
                ApplySelected();
            }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
        }

        private void DrawSummary()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("요약", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("스캔한 에셋 수", _scannedAssetCount.ToString());
            EditorGUILayout.LabelField("매칭된 이벤트 수", _matchedEventCount.ToString());
            EditorGUILayout.LabelField("마지막 적용 변경 이벤트 수", _changedEventCountLastApply.ToString());
            EditorGUILayout.LabelField("마지막 적용 변경 에셋 수", _changedAssetCountLastApply.ToString());

            if (!string.IsNullOrEmpty(_lastSummary))
            {
                EditorGUILayout.HelpBox(_lastSummary, MessageType.None);
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawResults()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("결과", EditorStyles.boldLabel);

            DrawResultHeader();

            _scrollEntries = EditorGUILayout.BeginScrollView(_scrollEntries);
            foreach (ScanEntry entry in _entries)
            {
                if (!ShouldDrawEntry(entry))
                {
                    continue;
                }

                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                GUI.enabled = entry.CanApply;
                entry.IsSelected = EditorGUILayout.Toggle(entry.IsSelected, GUILayout.Width(24f));
                GUI.enabled = true;

                EditorGUILayout.LabelField(entry.AssetTypeLabel, GUILayout.Width(95f));
                EditorGUILayout.LabelField(entry.ClipName, GUILayout.Width(180f));
                EditorGUILayout.LabelField(entry.EventTimeText, GUILayout.Width(72f));
                EditorGUILayout.LabelField(entry.FunctionName, GUILayout.Width(180f));
                EditorGUILayout.LabelField(entry.TargetFunctionName, GUILayout.Width(160f));
                EditorGUILayout.LabelField(entry.ParameterPreview, GUILayout.Width(220f));
                EditorGUILayout.LabelField(entry.StatusLabel, GUILayout.Width(100f));

                GUI.enabled = !string.IsNullOrEmpty(entry.AssetPath);
                if (GUILayout.Button("Ping", GUILayout.Width(50f)))
                {
                    PingAsset(entry.AssetPath);
                }
                GUI.enabled = true;

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(28f);
                EditorGUILayout.SelectableLabel(entry.AssetPath, EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private static void DrawResultHeader()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("선택", GUILayout.Width(28f));
            GUILayout.Label("유형", GUILayout.Width(95f));
            GUILayout.Label("클립", GUILayout.Width(180f));
            GUILayout.Label("시간", GUILayout.Width(72f));
            GUILayout.Label("기존", GUILayout.Width(180f));
            GUILayout.Label("변경", GUILayout.Width(160f));
            GUILayout.Label("파라미터", GUILayout.Width(220f));
            GUILayout.Label("상태", GUILayout.Width(100f));
            GUILayout.Label("", GUILayout.Width(50f));
            EditorGUILayout.EndHorizontal();
        }

        private bool ShouldDrawEntry(ScanEntry entry)
        {
            if (_showOnlyMatched && !entry.IsMatch)
            {
                return false;
            }

            if (_showOnlySelected && !entry.IsSelected)
            {
                return false;
            }

            return true;
        }

        private bool CanScan()
        {
            return !string.IsNullOrWhiteSpace(_oldFunctionName)
                   && !string.IsNullOrWhiteSpace(_newFunctionName)
                   && _searchFolders.Count > 0
                   && (_includeAnimClipAssets || _includeModelImporterClips);
        }

        private bool CanApply()
        {
            return _entries.Any(e => e.IsSelected && e.CanApply && e.IsMatch);
        }

        private void AddSearchFolder(string folderPath)
        {
            if (string.IsNullOrWhiteSpace(folderPath))
            {
                return;
            }

            folderPath = folderPath.Trim().Replace('\\', '/');
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                EditorUtility.DisplayDialog(Title, "유효한 폴더가 아닙니다. Assets 하위 폴더를 입력해주세요.\n" + folderPath, "OK");
                return;
            }

            if (_searchFolders.Contains(folderPath))
            {
                return;
            }

            _searchFolders.Add(folderPath);
            RemoveDuplicateFolders();
        }

        private void AddFoldersFromSelection()
        {
            UnityEngine.Object[] selection = Selection.GetFiltered(typeof(UnityEngine.Object), SelectionMode.Assets);
            if (selection == null || selection.Length == 0)
            {
                return;
            }

            foreach (UnityEngine.Object asset in selection)
            {
                string path = AssetDatabase.GetAssetPath(asset);
                if (string.IsNullOrEmpty(path))
                {
                    continue;
                }

                if (AssetDatabase.IsValidFolder(path))
                {
                    AddSearchFolder(path);
                    continue;
                }

                string directory = Path.GetDirectoryName(path);
                if (string.IsNullOrEmpty(directory))
                {
                    continue;
                }

                AddSearchFolder(directory.Replace('\\', '/'));
            }
        }

        private void RemoveDuplicateFolders()
        {
            HashSet<string> unique = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = _searchFolders.Count - 1; i >= 0; i--)
            {
                string folder = _searchFolders[i];
                if (string.IsNullOrWhiteSpace(folder) || !unique.Add(folder))
                {
                    _searchFolders.RemoveAt(i);
                }
            }

            if (_searchFolders.Count == 0)
            {
                _searchFolders.Add("Assets");
            }
        }

        private void Scan()
        {
            _entries.Clear();
            _scannedAssetCount = 0;
            _matchedEventCount = 0;
            _changedEventCountLastApply = 0;
            _changedAssetCountLastApply = 0;
            _lastSummary = string.Empty;

            string[] searchFolders = _searchFolders.Where(AssetDatabase.IsValidFolder).Distinct().ToArray();
            HashSet<string> processedAssetPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            string[] clipGuids = AssetDatabase.FindAssets("t:AnimationClip", searchFolders);

            try
            {
                for (int i = 0; i < clipGuids.Length; i++)
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(clipGuids[i]);
                    if (string.IsNullOrEmpty(assetPath) || !processedAssetPaths.Add(assetPath))
                    {
                        continue;
                    }

                    if (EditorUtility.DisplayCancelableProgressBar(
                            Title,
                            string.Format("Animation Clip 스캔 중...\n{0}", assetPath),
                            clipGuids.Length == 0 ? 1f : (float)i / clipGuids.Length))
                    {
                        _lastSummary = "스캔이 취소되었습니다.";
                        break;
                    }

                    AssetImporter importer = AssetImporter.GetAtPath(assetPath);
                    if (importer is ModelImporter)
                    {
                        if (_includeModelImporterClips)
                        {
                            ScanModelImporterAsset(assetPath, (ModelImporter)importer);
                        }
                        continue;
                    }

                    if (_includeAnimClipAssets)
                    {
                        ScanAnimationClipAsset(assetPath);
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            _matchedEventCount = _entries.Count(e => e.IsMatch);
            if (string.IsNullOrEmpty(_lastSummary))
            {
                _lastSummary = string.Format("스캔 완료: 에셋 {0}개, 매칭 이벤트 {1}개", _scannedAssetCount, _matchedEventCount);
            }
            Repaint();
        }

        private void ScanAnimationClipAsset(string assetPath)
        {
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
            if (clip == null)
            {
                return;
            }

            _scannedAssetCount++;
            AnimationEvent[] events = AnimationUtility.GetAnimationEvents(clip);
            if (events == null || events.Length == 0)
            {
                return;
            }

            for (int i = 0; i < events.Length; i++)
            {
                _entries.Add(new ScanEntry
                {
                    AssetPath = assetPath,
                    AssetType = ScanAssetType.AnimationClipAsset,
                    ClipName = clip.name,
                    EventIndex = i,
                    EventTime = events[i].time,
                    FunctionName = events[i].functionName,
                    TargetFunctionName = _newFunctionName,
                    ParameterPreview = BuildParameterPreview(events[i]),
                    IsMatch = string.Equals(events[i].functionName, _oldFunctionName, StringComparison.Ordinal),
                    CanApply = true,
                    IsSelected = _selectAllMatchesOnScan && string.Equals(events[i].functionName, _oldFunctionName, StringComparison.Ordinal)
                });
            }
        }

        private void ScanModelImporterAsset(string assetPath, ModelImporter importer)
        {
            ModelImporterClipAnimation[] clips = GetEditableModelImporterClips(importer);
            if (clips == null || clips.Length == 0)
            {
                return;
            }

            _scannedAssetCount++;
            for (int clipIndex = 0; clipIndex < clips.Length; clipIndex++)
            {
                ModelImporterClipAnimation clip = clips[clipIndex];
                AnimationEvent[] events = clip.events;
                if (events == null || events.Length == 0)
                {
                    continue;
                }

                for (int eventIndex = 0; eventIndex < events.Length; eventIndex++)
                {
                    AnimationEvent animationEvent = events[eventIndex];
                    bool isMatch = string.Equals(animationEvent.functionName, _oldFunctionName, StringComparison.Ordinal);
                    _entries.Add(new ScanEntry
                    {
                        AssetPath = assetPath,
                        AssetType = ScanAssetType.ModelImporterClip,
                        ClipName = clip.name,
                        ModelClipIndex = clipIndex,
                        EventIndex = eventIndex,
                        EventTime = animationEvent.time,
                        FunctionName = animationEvent.functionName,
                        TargetFunctionName = _newFunctionName,
                        ParameterPreview = BuildParameterPreview(animationEvent),
                        IsMatch = isMatch,
                        CanApply = true,
                        IsSelected = _selectAllMatchesOnScan && isMatch
                    });
                }
            }
        }

        private void ApplySelected()
        {
            List<ScanEntry> targets = _entries.Where(e => e.IsSelected && e.CanApply && e.IsMatch).ToList();
            if (targets.Count == 0)
            {
                EditorUtility.DisplayDialog(Title, "적용할 항목이 없습니다.", "OK");
                return;
            }

            if (!EditorUtility.DisplayDialog(
                    Title,
                    string.Format("선택된 {0}개 이벤트를 변경하시겠습니까?\n{1} -> {2}", targets.Count, _oldFunctionName, _newFunctionName),
                    "적용",
                    "취소"))
            {
                return;
            }

            int changedEvents = 0;
            int changedAssets = 0;
            List<string> errorMessages = new List<string>();

            var groupedByAsset = targets.GroupBy(e => e.AssetPath).ToList();
            try
            {
                for (int i = 0; i < groupedByAsset.Count; i++)
                {
                    IGrouping<string, ScanEntry> group = groupedByAsset[i];
                    string assetPath = group.Key;
                    if (EditorUtility.DisplayCancelableProgressBar(
                            Title,
                            string.Format("Animation Event 적용 중...\n{0}", assetPath),
                            groupedByAsset.Count == 0 ? 1f : (float)i / groupedByAsset.Count))
                    {
                        _lastSummary = "적용이 취소되었습니다.";
                        break;
                    }

                    try
                    {
                        ScanAssetType assetType = group.First().AssetType;
                        int assetChangedEvents = 0;
                        if (assetType == ScanAssetType.AnimationClipAsset)
                        {
                            assetChangedEvents = ApplyToAnimationClipAsset(assetPath, group.ToList());
                        }
                        else if (assetType == ScanAssetType.ModelImporterClip)
                        {
                            assetChangedEvents = ApplyToModelImporterAsset(assetPath, group.ToList());
                        }

                        if (assetChangedEvents > 0)
                        {
                            changedAssets++;
                            changedEvents += assetChangedEvents;
                        }
                    }
                    catch (Exception exception)
                    {
                        errorMessages.Add(string.Format("{0}\n{1}", assetPath, exception.Message));
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            _changedEventCountLastApply = changedEvents;
            _changedAssetCountLastApply = changedAssets;

            string resultSummary;
            if (errorMessages.Count > 0)
            {
                resultSummary = string.Format("적용 완료: 에셋 {0}개, 이벤트 {1}개 변경, 오류 {2}건", changedAssets, changedEvents, errorMessages.Count);
                Debug.LogError(string.Join("\n\n", errorMessages.ToArray()));
            }
            else if (!string.IsNullOrEmpty(_lastSummary) && _lastSummary == "적용이 취소되었습니다.")
            {
                resultSummary = _lastSummary;
            }
            else
            {
                resultSummary = string.Format("적용 완료: 에셋 {0}개, 이벤트 {1}개 변경", changedAssets, changedEvents);
            }

            Scan();
            _changedEventCountLastApply = changedEvents;
            _changedAssetCountLastApply = changedAssets;
            _lastSummary = resultSummary;
        }

        private int ApplyToAnimationClipAsset(string assetPath, List<ScanEntry> selectedEntries)
        {
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
            if (clip == null)
            {
                return 0;
            }

            AnimationEvent[] events = AnimationUtility.GetAnimationEvents(clip);
            if (events == null || events.Length == 0)
            {
                return 0;
            }

            HashSet<int> selectedEventIndices = new HashSet<int>(selectedEntries.Select(e => e.EventIndex));
            bool changed = false;
            int changedCount = 0;
            for (int i = 0; i < events.Length; i++)
            {
                if (!selectedEventIndices.Contains(i) || !string.Equals(events[i].functionName, _oldFunctionName, StringComparison.Ordinal))
                {
                    continue;
                }

                events[i].functionName = _newFunctionName;
                changed = true;
                changedCount++;
            }

            if (!changed)
            {
                return 0;
            }

            Undo.RecordObject(clip, "Change Animation Event Function Name");
            AnimationUtility.SetAnimationEvents(clip, events);
            EditorUtility.SetDirty(clip);
            return changedCount;
        }

        private int ApplyToModelImporterAsset(string assetPath, List<ScanEntry> selectedEntries)
        {
            ModelImporter importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
            if (importer == null)
            {
                return 0;
            }

            ModelImporterClipAnimation[] clips = GetEditableModelImporterClips(importer);
            if (clips == null || clips.Length == 0)
            {
                return 0;
            }

            int changedCount = 0;
            bool importerChanged = false;

            Dictionary<int, HashSet<int>> selectedEventIndicesByClip = selectedEntries
                .GroupBy(e => e.ModelClipIndex)
                .ToDictionary(g => g.Key, g => new HashSet<int>(g.Select(e => e.EventIndex)));

            for (int clipIndex = 0; clipIndex < clips.Length; clipIndex++)
            {
                HashSet<int> selectedEventIndices;
                if (!selectedEventIndicesByClip.TryGetValue(clipIndex, out selectedEventIndices))
                {
                    continue;
                }

                AnimationEvent[] events = clips[clipIndex].events;
                if (events == null || events.Length == 0)
                {
                    continue;
                }

                bool clipChanged = false;
                for (int eventIndex = 0; eventIndex < events.Length; eventIndex++)
                {
                    if (!selectedEventIndices.Contains(eventIndex) || !string.Equals(events[eventIndex].functionName, _oldFunctionName, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    events[eventIndex].functionName = _newFunctionName;
                    clipChanged = true;
                    importerChanged = true;
                    changedCount++;
                }

                if (clipChanged)
                {
                    clips[clipIndex].events = events;
                }
            }

            if (!importerChanged)
            {
                return 0;
            }

            importer.clipAnimations = clips;
            importer.SaveAndReimport();
            return changedCount;
        }

        private static ModelImporterClipAnimation[] GetEditableModelImporterClips(ModelImporter importer)
        {
            if (importer == null)
            {
                return Array.Empty<ModelImporterClipAnimation>();
            }

            ModelImporterClipAnimation[] clipAnimations = importer.clipAnimations;
            if (clipAnimations != null && clipAnimations.Length > 0)
            {
                return CloneClipAnimations(clipAnimations);
            }

            ModelImporterClipAnimation[] defaultClipAnimations = importer.defaultClipAnimations;
            if (defaultClipAnimations == null || defaultClipAnimations.Length == 0)
            {
                return Array.Empty<ModelImporterClipAnimation>();
            }

            return CloneClipAnimations(defaultClipAnimations);
        }

        private static ModelImporterClipAnimation[] CloneClipAnimations(ModelImporterClipAnimation[] source)
        {
            if (source == null || source.Length == 0)
            {
                return Array.Empty<ModelImporterClipAnimation>();
            }

            ModelImporterClipAnimation[] clone = new ModelImporterClipAnimation[source.Length];
            for (int i = 0; i < source.Length; i++)
            {
                clone[i] = source[i];
                AnimationEvent[] sourceEvents = source[i].events;
                if (sourceEvents == null || sourceEvents.Length == 0)
                {
                    clone[i].events = Array.Empty<AnimationEvent>();
                    continue;
                }

                AnimationEvent[] eventClone = new AnimationEvent[sourceEvents.Length];
                for (int eventIndex = 0; eventIndex < sourceEvents.Length; eventIndex++)
                {
                    eventClone[eventIndex] = CloneAnimationEvent(sourceEvents[eventIndex]);
                }
                clone[i].events = eventClone;
            }
            return clone;
        }

        private static AnimationEvent CloneAnimationEvent(AnimationEvent source)
        {
            AnimationEvent clone = new AnimationEvent();
            clone.time = source.time;
            clone.functionName = source.functionName;
            clone.stringParameter = source.stringParameter;
            clone.floatParameter = source.floatParameter;
            clone.intParameter = source.intParameter;
            clone.objectReferenceParameter = source.objectReferenceParameter;
            clone.messageOptions = source.messageOptions;
            return clone;
        }

        private void SetSelectionForVisibleEntries(bool selected)
        {
            foreach (ScanEntry entry in _entries)
            {
                if (!ShouldDrawEntry(entry) || !entry.CanApply)
                {
                    continue;
                }

                entry.IsSelected = selected;
            }
        }

        private static string BuildParameterPreview(AnimationEvent animationEvent)
        {
            if (animationEvent == null)
            {
                return string.Empty;
            }

            if (!string.IsNullOrEmpty(animationEvent.stringParameter))
            {
                string text = animationEvent.stringParameter;
                if (text.Length > 40)
                {
                    text = text.Substring(0, 40) + "...";
                }
                return "string: " + text;
            }

            if (animationEvent.intParameter != 0)
            {
                return "int: " + animationEvent.intParameter;
            }

            if (Math.Abs(animationEvent.floatParameter) > 0.0001f)
            {
                return "float: " + animationEvent.floatParameter.ToString("0.###");
            }

            if (animationEvent.objectReferenceParameter != null)
            {
                return "object: " + animationEvent.objectReferenceParameter.name;
            }

            return "-";
        }

        private static void PingAsset(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
            {
                return;
            }

            UnityEngine.Object asset = AssetDatabase.LoadMainAssetAtPath(assetPath);
            if (asset != null)
            {
                EditorGUIUtility.PingObject(asset);
                Selection.activeObject = asset;
            }
        }

        private enum ScanAssetType
        {
            AnimationClipAsset,
            ModelImporterClip
        }

        [Serializable]
        private sealed class ScanEntry
        {
            public string AssetPath;
            public ScanAssetType AssetType;
            public string ClipName;
            public int ModelClipIndex;
            public int EventIndex;
            public float EventTime;
            public string FunctionName;
            public string TargetFunctionName;
            public string ParameterPreview;
            public bool IsMatch;
            public bool CanApply;
            public bool IsSelected;

            public string AssetTypeLabel
            {
                get { return AssetType == ScanAssetType.ModelImporterClip ? "Model Clip" : ".anim"; }
            }

            public string EventTimeText
            {
                get { return EventTime.ToString("0.###"); }
            }

            public string StatusLabel
            {
                get
                {
                    if (!CanApply)
                    {
                        return "불가";
                    }

                    return IsMatch ? "변경 대상" : "유지";
                }
            }
        }
    }
}
#endif
