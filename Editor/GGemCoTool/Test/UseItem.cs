using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
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
        private int _itemUid;

        private bool _showAllItems;

        private TableItem _tableItem;
        private TableItemUse _tableItemUse;
        private TableItemUseAction _tableItemUseAction;

        private readonly List<string> _namesItem = new();
        private readonly List<int> _uidsItem = new();
        private readonly List<SearchableDropdownUtility.Option<int>> _itemOptions = new();
        private int _selectedIndexItem;

        private Vector2 _scroll;

        protected override void OnEnable()
        {
            base.OnEnable();

            _selectedIndexItem = 0;

            // Edit Mode 드롭다운을 위해 에디터 로더 사용(동기)
            _tableItem = TableLoaderManager.LoadItemTable();
            _tableItemUse = TableLoaderManager.LoadItemUseTable();
            _tableItemUseAction = TableLoaderManager.LoadItemUseActionTable();

            BuildItemDropdown();
        }

        private void OnGUI()
        {
            DrawTargetSection();
            EditorGUILayout.Space(6);

            DrawTableSection();
            EditorGUILayout.Space(6);

            DrawPreviewSection();
            EditorGUILayout.Space(8);

            DrawExecuteSection();
        }

        private void DrawTargetSection()
        {
            EditorGUILayout.LabelField("대상", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope("box"))
            {
                _targetObject = (GameObject)EditorGUILayout.ObjectField("Target", _targetObject, typeof(GameObject), true);

                EditorGUILayout.HelpBox(
                    "Target이 비어있으면 기본적으로 SceneGame.player(플레이어) 기준으로 처리합니다.\n" +
                    "스킬 지급/어펙트 적용 등은 Target/Player에 필요한 컴포넌트가 있어야 성공합니다.",
                    MessageType.Info);
            }
        }

        private void DrawTableSection()
        {
            EditorGUILayout.LabelField("아이템 선택", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope("box"))
            {
                if (_tableItem == null || _tableItemUse == null || _tableItemUseAction == null)
                {
                    EditorGUILayout.HelpBox("테이블을 불러오지 못했습니다. Addressables 설정/테이블 등록 상태를 확인하세요.", MessageType.Warning);
                }

                bool newShowAllItems = EditorGUILayout.ToggleLeft("item_use 정의가 없는 아이템도 표시", _showAllItems);
                if (newShowAllItems != _showAllItems)
                {
                    _showAllItems = newShowAllItems;
                    BuildItemDropdown();
                }

                if (_selectedIndexItem >= _itemOptions.Count)
                    _selectedIndexItem = 0;

                // Searchable dropdown (UseCrowdControl/UseProjectile 스타일)
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.PrefixLabel("Item");

                    string currentText = (_selectedIndexItem >= 0 && _selectedIndexItem < _itemOptions.Count)
                        ? _itemOptions[_selectedIndexItem].ToString()
                        : "Select...";

                    SearchableDropdownUtility.DrawButtonAndShow(
                        buttonText: currentText,
                        options: _itemOptions,
                        selectedIndex: _selectedIndexItem,
                        onSelected: (idx, opt) =>
                        {
                            _selectedIndexItem = idx;
                            _itemUid = opt.Data;
                            Repaint();
                        },
                        defaultSearchMode: SearchableDropdownUtility.SearchMode.Both);
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("드롭다운 새로고침"))
                    {
                        // Edit mode dropdown refresh
                        _tableItem = TableLoaderManager.LoadItemTable(true);
                        _tableItemUse = TableLoaderManager.LoadItemUseTable(true);
                        _tableItemUseAction = TableLoaderManager.LoadItemUseActionTable(true);
                        BuildItemDropdown();
                    }

                    if (GUILayout.Button("런타임 테이블 다시 로드(Play Mode)"))
                    {
                        _ = ReloadRuntimeTablesAsync();
                    }
                }
            }
        }

        private void DrawPreviewSection()
        {
            EditorGUILayout.LabelField("미리보기", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope("box"))
            {
                if (_itemUid <= 0)
                {
                    EditorGUILayout.HelpBox("아이템을 선택하세요.", MessageType.Info);
                    return;
                }

                var sb = new StringBuilder();

                var itemRow = _tableItem?.GetDataByUid(_itemUid);
                if (itemRow != null)
                {
                    sb.AppendLine($"[Item] {itemRow.Uid} - {itemRow.Name}");
                    sb.AppendLine($"- Category: {itemRow.Category}");
                    sb.AppendLine($"- CoolTime: {itemRow.CoolTime}");
                }
                else
                {
                    sb.AppendLine("[Item] (테이블에서 찾지 못함)");
                }

                if (_tableItemUse != null && _tableItemUse.TryGetByItemUid(_itemUid, out var useGroup) && useGroup != null)
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
                            sb.AppendLine($"- ({a.Order}) {a.ActionType} / IntA:{a.ParamIntA} IntB:{a.ParamIntB} FloatA:{a.ParamFloatA} FloatB:{a.ParamFloatB} StrA:{a.ParamStringA} StrB:{a.ParamStringB}");
                        }
                    }
                }
                else
                {
                    sb.AppendLine();
                    sb.AppendLine("[UseGroup] (item_use 정의 없음)");
                }

                _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.MinHeight(140));
                EditorGUILayout.TextArea(sb.ToString());
                EditorGUILayout.EndScrollView();
            }
        }

        private void DrawExecuteSection()
        {
            EditorGUILayout.LabelField("실행", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope("box"))
            {
                if (!Application.isPlaying)
                {
                    EditorGUILayout.HelpBox("Play Mode에서만 실행됩니다.", MessageType.Warning);
                    return;
                }

                if (GUILayout.Button("아이템 사용 실행(소모 없음)"))
                {
                    ExecuteUse();
                }

                EditorGUILayout.HelpBox(
                    "본 툴은 인벤토리를 소모하지 않습니다.\n" +
                    "실제 인벤토리 슬롯 사용 테스트는 게임 UI(인벤토리/퀵슬롯)에서 확인하세요.",
                    MessageType.Info);
            }
        }

        private void ExecuteUse()
        {
            if (_itemUid <= 0)
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

            var result = ItemUseService.TryUseItem(sceneGame, _itemUid, out var cooldownSeconds, _targetObject);
            Debug.Log($"[UseItem] itemUid:{_itemUid} result:{result?.Result} cooldown:{cooldownSeconds:0.###}s");
        }

        private void BuildItemDropdown()
        {
            _namesItem.Clear();
            _uidsItem.Clear();
            _itemOptions.Clear();

            _namesItem.Add("Select...");
            _uidsItem.Add(0);
            _itemOptions.Add(new SearchableDropdownUtility.Option<int>("0", "Select...", 0));

            if (_tableItem == null)
            {
                _selectedIndexItem = 0;
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
                _namesItem.Add($"{mark} {row.Uid} - {row.Name}");
                _uidsItem.Add(row.Uid);

                // Key(Uid) + Value(Name) 형태로 표시되며, 검색은 Key/Value 모두 지원
                _itemOptions.Add(new SearchableDropdownUtility.Option<int>(
                    key: row.Uid.ToString(),
                    value: $"{mark} {row.Name}",
                    data: row.Uid));
            }

            // 현재 itemUid가 드롭다운에 있으면 선택 맞춤
            if (_itemUid > 0)
            {
                for (int i = 0; i < _uidsItem.Count; i++)
                {
                    if (_uidsItem[i] == _itemUid)
                    {
                        _selectedIndexItem = i;
                        return;
                    }
                }
            }

            _selectedIndexItem = 0;
        }

        private async Task ReloadRuntimeTablesAsync()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[UseItem] Runtime reload is available only in Play Mode.");
                return;
            }

            var loader = Object.FindFirstObjectByType<GGemCo2DCore.TableLoaderManager>();
            if (loader == null)
            {
                Debug.LogWarning("[UseItem] TableLoaderManager (runtime) not found in scene.");
                return;
            }

            try
            {
                await loader.LoadDataFile(ConfigAddressableTable.TableItem);
                await loader.LoadDataFile(ConfigAddressableTable.TableItemUse);
                await loader.LoadDataFile(ConfigAddressableTable.TableItemUseAction);

                // 드롭다운은 에디터용 테이블을 다시 로드해 갱신
                _tableItem = TableLoaderManager.LoadItemTable(true);
                _tableItemUse = TableLoaderManager.LoadItemUseTable(true);
                _tableItemUseAction = TableLoaderManager.LoadItemUseActionTable(true);
                BuildItemDropdown();

                Debug.Log("[UseItem] Runtime tables reloaded.");
            }
            catch (System.SystemException e)
            {
                Debug.LogError($"[UseItem] ReloadRuntimeTablesAsync failed: {e}");
            }
        }
    }
}
