using System;
using System.Collections.Generic;
using System.Text;
using GGemCo2DCore;
using UnityEditor;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// 아이템 사용 테스트 EditorWindow.
    /// - item_use / item_use_action 기반 아이템 사용 효과를 즉시 실행(테스트)합니다.
    /// - 인벤토리 소모 없이 "ItemUid" 사용 효과만 실행합니다(런타임 ItemUseService.TryUseItem).
    /// - UseProjectile/UseCrowdControl UX를 참고하여 "대상 선택" 섹션을 최상단에 배치했습니다.
    ///
    /// 주의:
    /// - 실제 실행은 Play Mode에서만 동작합니다(SceneGame/테이블 로더 필요).
    /// - Edit Mode에서는 테이블 드롭다운/미리보기만 제공합니다.
    /// </summary>
    public sealed class UseItem : DefaultEditorWindow
    {
        private const string Title = "아이템 사용툴";

        [MenuItem(ConfigEditor.NameToolUseItem, false, (int)ConfigEditor.ToolOrdering.UseItem)]
        public static void ShowWindow() => GetWindow<UseItem>(Title);

        // ------------------------------
        // Target
        // ------------------------------
        [Header("대상")]
        private GameObject _targetObject;

        // ------------------------------
        // Table
        // ------------------------------
        [Header("정의(테이블)")]
        [Tooltip("item.txt Uid (item_use/item_use_action 정의가 있어야 실제 사용 가능)")]
        private bool _showAllItems;

        private TableItem _tableItem;
        private TableItemUse _tableItemUse;
        private TableItemUseAction _tableItemUseAction;

        private Dictionary<int, StruckTableItem> _dictionary;
        private readonly List<SearchableDropdownUtility.Option<StruckTableItem>> _dropDownOptions = new();
        private StruckTableItem _selectedData;

        private string _lastReloadMessage = string.Empty;
        private Vector2 _scroll;
        private Vector2 _scrollOption;

        protected override void OnEnable()
        {
            base.OnEnable();

            _selectedData = null;

            ReloadAllTables();
        }
        protected override void OnSelectedCharacterChanged(CharacterBase character)
        {
            Repaint();
        }

        private void OnGUI()
        {
            using (var scroll = new EditorGUILayout.ScrollViewScope(_scroll))
            {
                _scroll = scroll.scrollPosition;
                EditorGUILayout.Space(6);
                
                DrawPlayModeGate();
                EditorGUILayout.Space(6);
                
                DrawTargetSection();
                EditorGUILayout.Space(6);

                DrawTableSection();
                EditorGUILayout.Space(6);

                DrawPreviewSection();
                EditorGUILayout.Space(8);

                DrawExecuteSection();
                EditorGUILayout.Space(6);
                
                DrawReloadSection();
                EditorGUILayout.Space(20);
            }
        }

        private void DrawTargetSection()
        {
            DrawCharacterSelectionSection(Title);

            if (selectedCharacter == null)
                return;
        }

        private void DrawTableSection()
        {
            EditorGUILayout.LabelField("아이템 선택", EditorStyles.boldLabel);
            if (_tableItem == null || _tableItemUse == null || _tableItemUseAction == null)
            {
                EditorGUILayout.HelpBox("테이블을 불러오지 못했습니다. Addressables 설정/테이블 등록 상태를 확인하세요.", MessageType.Warning);
            }

            bool newShowAllItems = EditorGUILayout.ToggleLeft("item_use 정의가 없는 아이템도 표시", _showAllItems);
            if (newShowAllItems != _showAllItems)
            {
                _showAllItems = newShowAllItems;
                RebuildDropdown();
            }

            if (_dropDownOptions.Count <= 0)
                _selectedData = null;

            // Searchable dropdown (UseCrowdControl/UseProjectile 스타일)
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel("Item");

                string currentText = _selectedData != null ? _selectedData.Name : "선택...";
                int selectIndex = _selectedData?.Uid ?? 0;

                SearchableDropdownUtility.DrawButtonAndShow(
                    buttonText: currentText,
                    options: _dropDownOptions,
                    selectedIndex: selectIndex,
                    onSelected: (idx, opt) =>
                    {
                        _selectedData = opt.Data;
                        Repaint();
                    },
                    defaultSearchMode: SearchableDropdownUtility.SearchMode.Both);
            }
        }

        private void DrawPreviewSection()
        {
            EditorGUILayout.LabelField("미리보기", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope("box"))
            {
                if (_selectedData == null)
                {
                    EditorGUILayout.HelpBox("아이템을 선택하세요.", MessageType.Info);
                    return;
                }

                var sb = new StringBuilder();

                if (_selectedData != null)
                {
                    sb.AppendLine($"[Item] {_selectedData.Uid} - {_selectedData.Name}");
                    sb.AppendLine($"- Category: {_selectedData.Category}");
                    sb.AppendLine($"- CoolTime: {_selectedData.CoolTime}");
                }
                else
                {
                    sb.AppendLine("[Item] (테이블에서 찾지 못함)");
                }

                if (_tableItemUse != null && _tableItemUse.TryGetByItemUid(_selectedData.Uid, out var useGroup) && useGroup != null)
                {
                    sb.AppendLine();
                    sb.AppendLine($"[UseGroup] {useGroup.Uid} - {useGroup.Name}");
                    sb.AppendLine($"- ConsumeCount: {useGroup.ConsumeCount}");
                    sb.AppendLine($"- CooldownOverride: {useGroup.CooldownOverride}");
                    sb.AppendLine($"- FailPolicy: {useGroup.FailPolicy}");

                    var actions = _tableItemUseAction?.GetActions(useGroup.Uid);
                    sb.AppendLine();
                    sb.AppendLine("[Actions]");
                    if (actions == null || actions.Count == 0)
                    {
                        sb.AppendLine("- (none)");
                    }
                    else
                    {
                        for (int i = 0; i < actions.Count; i++)
                        {
                            var a = actions[i];
                            if (a == null) continue;
                            sb.AppendLine(
                                $"- ({a.Order}) {a.ActionType} / IntA:{a.ParamIntA} IntB:{a.ParamIntB} FloatA:{a.ParamFloatA} FloatB:{a.ParamFloatB} StrA:{a.ParamStringA} StrB:{a.ParamStringB}");
                        }
                    }
                }
                else
                {
                    sb.AppendLine();
                    sb.AppendLine("[UseGroup] (item_use 정의 없음)");
                }

                _scrollOption = EditorGUILayout.BeginScrollView(_scrollOption, GUILayout.MinHeight(140));
                EditorGUILayout.TextArea(sb.ToString());
                EditorGUILayout.EndScrollView();
            }
        }

        private void DrawExecuteSection()
        {
            EditorGUILayout.LabelField("실행", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope("box"))
            {
                using (new EditorGUI.DisabledScope(!Application.isPlaying))
                {
                    if (GUILayout.Button("아이템 사용 실행(소모 없음)"))
                    {
                        ExecuteUse();
                    }
                }

                EditorGUILayout.HelpBox(
                    "본 툴은 인벤토리를 소모하지 않습니다.\n" +
                    "실제 인벤토리 슬롯 사용 테스트는 게임 UI(인벤토리/퀵슬롯)에서 확인하세요.",
                    MessageType.Info);
            }
        }

        private void ExecuteUse()
        {
            if (_selectedData == null || _selectedData.Uid <= 0)
            {
                Debug.LogWarning("[UseItem] itemUid is invalid.");
                return;
            }

            var sceneGame = SceneGame.Instance;
            if (sceneGame == null)
            {
                Debug.LogWarning("[UseItem] SceneGame.Instance not found.");
                return;
            }

            var result = ItemUseService.TryUseItem(sceneGame, _selectedData.Uid, out var cooldownSeconds, _targetObject);
            Debug.Log($"[UseItem] itemUid:{_selectedData.Uid} result:{result?.Result} cooldown:{cooldownSeconds:0.###}s");
        }

        private void ReloadAllTables()
        {
            try
            {
                // Edit Mode 드롭다운을 위해 에디터 로더 사용(동기)
                _tableItem = TableLoaderManager.LoadItemTable();
                _tableItemUse = TableLoaderManager.LoadItemUseTable();
                _tableItemUseAction = TableLoaderManager.LoadItemUseActionTable();

                _dictionary = _tableItem.GetDatas();
                RebuildDropdown();

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
            _dropDownOptions.Clear();

            if (_tableItem == null)
            {
                _selectedData = null;
                return;
            }

            var datas = _tableItem.GetDatas();
            foreach (var kv in datas)
            {
                var row = kv.Value;
                if (row == null || row.Uid <= 0) continue;

                bool hasUse = _tableItemUse != null && _tableItemUse.TryGetByItemUid(row.Uid, out var useGroup) && useGroup != null;
                if (!_showAllItems && !hasUse) continue;

                var mark = hasUse ? "[Use]" : "[NoUse]";

                // Key(Uid) + Value(Name) 형태로 표시되며, 검색은 Key/Value 모두 지원
                _dropDownOptions.Add(new SearchableDropdownUtility.Option<StruckTableItem>(
                    key: row.Uid.ToString(),
                    value: $"{mark} {row.Name}",
                    data: row));
            }

            _selectedData = _dropDownOptions[0].Data;
        }
        
        private void DrawReloadSection()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("테이블 재로딩", EditorStyles.boldLabel);

                using (new EditorGUI.DisabledScope(!Application.isPlaying))
                {
                    if (GUILayout.Button("item / item_use / item_use_action 재로딩", GUILayout.Height(24)))
                    {
                        ReloadAllTables();
                        RefreshSceneCharacters();
                    }
                }

                if (!string.IsNullOrEmpty(_lastReloadMessage))
                    EditorGUILayout.HelpBox(_lastReloadMessage, MessageType.Info);
            }
        }
    }
}
